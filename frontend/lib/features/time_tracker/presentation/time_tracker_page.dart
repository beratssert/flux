import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api_error_message.dart';
import '../../auth/data/auth_api_client.dart';
import '../../auth/data/auth_models.dart';
import '../../auth/data/auth_session_controller.dart';
import '../data/time_tracker_api_client.dart';
import '../data/time_tracker_models.dart';

class TimeTrackerPage extends ConsumerStatefulWidget {
  const TimeTrackerPage({super.key});

  @override
  ConsumerState<TimeTrackerPage> createState() => _TimeTrackerPageState();
}

class _TimeTrackerPageState extends ConsumerState<TimeTrackerPage> {
  final TextEditingController _descriptionController = TextEditingController();

  Timer? _ticker;
  bool _loading = true;
  bool _submitting = false;
  String? _screenError;
  RunningTimerRecord? _activeTimer;
  List<TimeEntryRecord> _entries = const <TimeEntryRecord>[];
  List<int> _knownProjectIds = const <int>[];
  Map<int, String> _projectNames = const <int, String>{};
  AuthProfile? _liveProfile;
  int? _selectedProjectId;
  bool _isBillable = false;
  _AppSection _activeSection = _AppSection.timeTracker;

  @override
  void initState() {
    super.initState();
    _startTicker();
    _loadDashboard();
  }

  @override
  void dispose() {
    _ticker?.cancel();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<AuthProfile?> _loadCurrentProfile(AuthSession? session) async {
    if (session == null || session.accessToken.isEmpty) {
      return session?.profile;
    }

    try {
      final response = await ref.read(authApiClientProvider).getMyProfile(
            accessToken: session.accessToken,
          );
      final profile = AuthProfile.fromJson(_asLocalJsonMap(response.data));

      return session.profile.copyWith(
        id: profile.id.isNotEmpty ? profile.id : session.profile.id,
        email: profile.email.isNotEmpty ? profile.email : session.profile.email,
        firstName: profile.firstName,
        lastName: profile.lastName,
        role: profile.role ?? session.profile.role,
        isActive: profile.isActive,
        lastLoginAtUtc: profile.lastLoginAtUtc,
      );
    } catch (error) {
      await _handleProtectedError(error);
      return session.profile;
    }
  }

  Future<void> _loadDashboard({bool showLoading = true}) async {
    if (showLoading && mounted) {
      setState(() {
        _loading = true;
        _screenError = null;
      });
    }

    try {
      final api = ref.read(timeTrackerApiClientProvider);
      final session = ref.read(authSessionControllerProvider).session;
      final profile = await _loadCurrentProfile(session);
      final storage = ref.read(authStorageProvider);
      final knownProjectIds = await storage.readKnownProjectIds();
      final knownProjectNames = await storage.readKnownProjectNames();
      final activeTimer = await api.getActiveTimer();
      final entriesPage = await api.getTimeEntries();

      final mergedProjectIds = <int>{
        ...knownProjectIds,
        ...entriesPage.items.map((entry) => entry.projectId),
        if (activeTimer != null) activeTimer.projectId,
      }.toList()
        ..sort();

      final mergedProjectNames = _mergeProjectNames(
        existingNames: knownProjectNames,
        projectIds: mergedProjectIds,
      );

      await storage.writeKnownProjectIds(mergedProjectIds);
      await storage.writeKnownProjectNames(mergedProjectNames);

      if (!mounted) {
        return;
      }

      setState(() {
        _activeTimer = activeTimer;
        _entries = _sortedEntries(entriesPage.items);
        _knownProjectIds = mergedProjectIds;
        _projectNames = mergedProjectNames;
        _liveProfile = profile ?? session?.profile ?? _liveProfile;
        _selectedProjectId = _resolveSelectedProjectId(
          currentValue: _selectedProjectId,
          activeProjectId: activeTimer?.projectId,
          knownProjectIds: mergedProjectIds,
        );
        _isBillable = activeTimer?.isBillable ?? _isBillable;
        if (activeTimer != null) {
          _descriptionController.text = activeTimer.description;
        }
        _loading = false;
        _screenError = null;
      });
    } catch (error) {
      await _handleProtectedError(error);

      if (!mounted) {
        return;
      }

      setState(() {
        _loading = false;
        _screenError = describeApiError(
          error,
          fallback: 'Time tracker data could not be loaded.',
        );
      });
    }
  }

  Future<void> _startTimer() async {
    final projectId = _selectedProjectId;
    if (projectId == null) {
      _showMessage('Select a project before starting the timer.');
      return;
    }

    final description = _descriptionController.text.trim();
    if (description.isEmpty) {
      _showMessage('Enter a short description first.');
      return;
    }

    setState(() {
      _submitting = true;
    });

    try {
      await ref.read(timeTrackerApiClientProvider).startTimer(
            projectId: projectId,
            description: description,
            isBillable: _isBillable,
          );
      await _loadDashboard(showLoading: false);
      _showMessage('Timer started.');
    } catch (error) {
      await _handleProtectedError(error);
      _showMessage(
        describeApiError(
          error,
          fallback: 'Timer could not be started.',
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _submitting = false;
        });
      }
    }
  }

  Future<void> _stopTimer() async {
    final overlap = _firstTimerOverlap();
    if (overlap != null) {
      await _showTimerOverlapDialog(overlap);
      return;
    }

    setState(() {
      _submitting = true;
    });

    try {
      await ref.read(timeTrackerApiClientProvider).stopTimer();
      _descriptionController.clear();
      await _loadDashboard(showLoading: false);
      _showMessage('Timer stopped and saved.');
    } catch (error) {
      await _handleProtectedError(error);
      _showMessage(
        describeApiError(
          error,
          fallback: 'Timer could not be stopped.',
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _submitting = false;
        });
      }
    }
  }

  Future<void> _showTimerOverlapDialog(TimeEntryRecord overlap) async {
    final shouldEdit = await showDialog<bool>(
          context: context,
          builder: (dialogContext) {
            return AlertDialog(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(24),
              ),
              title: const Text('Overlapping time entry'),
              content: Text(
                'Your active timer overlaps with "${overlap.description.trim().isEmpty ? 'Untitled session' : overlap.description.trim()}" (${_formatEntryTime(overlap)}). Edit that entry first, then the timer can be stopped.',
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.of(dialogContext).pop(false),
                  child: const Text('Later'),
                ),
                FilledButton(
                  onPressed: () => Navigator.of(dialogContext).pop(true),
                  child: const Text('Edit entry'),
                ),
              ],
            );
          },
        ) ??
        false;

    if (!shouldEdit) {
      return;
    }

    final saved = await _showManualEntryDialog(entry: overlap);
    if (!saved || !mounted || _activeTimer == null) {
      return;
    }

    final nextOverlap = _firstTimerOverlap();
    if (nextOverlap != null) {
      _showMessage('Another overlapping time entry still blocks this timer.');
      return;
    }

    await _stopTimer();
  }

  Future<void> _deleteEntry(TimeEntryRecord entry) async {
    final title = entry.description.trim().isEmpty
        ? 'Untitled session'
        : entry.description.trim();
    final confirmed = await showDialog<bool>(
          context: context,
          builder: (dialogContext) {
            return AlertDialog(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(24),
              ),
              title: const Text('Delete time entry'),
              content: Text(
                'Delete "$title" permanently? This action cannot be undone.',
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.of(dialogContext).pop(false),
                  child: const Text('Cancel'),
                ),
                FilledButton(
                  onPressed: () => Navigator.of(dialogContext).pop(true),
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFFEF4444),
                  ),
                  child: const Text('Delete'),
                ),
              ],
            );
          },
        ) ??
        false;

    if (!confirmed) {
      return;
    }

    setState(() {
      _submitting = true;
    });

    try {
      await ref
          .read(timeTrackerApiClientProvider)
          .deleteTimeEntry(id: entry.id);
      await _loadDashboard(showLoading: false);
      _showMessage('Time entry deleted.');
    } catch (error) {
      await _handleProtectedError(error);
      _showMessage(
        describeApiError(
          error,
          fallback: 'Time entry could not be deleted.',
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _submitting = false;
        });
      }
    }
  }

  Future<bool> _showManualEntryDialog({TimeEntryRecord? entry}) async {
    final isEditing = entry != null;
    final dialogTitle = isEditing ? 'Edit time entry' : 'Add manual entry';
    final descriptionController = TextEditingController(
      text: isEditing ? entry.description : _descriptionController.text.trim(),
    );
    final durationController = TextEditingController(
      text:
          isEditing && (entry.startTimeUtc == null || entry.endTimeUtc == null)
              ? entry.durationMinutes.toString()
              : '',
    );

    var selectedProjectId = entry?.projectId ?? _selectedProjectId;
    var selectedDate = _editorDateForEntry(entry);
    var useTimeRange = entry == null ||
        (entry.startTimeUtc != null && entry.endTimeUtc != null);
    var startTime = entry?.startTimeUtc == null
        ? null
        : TimeOfDay.fromDateTime(entry!.startTimeUtc!.toLocal());
    var endTime = entry?.endTimeUtc == null
        ? null
        : TimeOfDay.fromDateTime(entry!.endTimeUtc!.toLocal());
    var isBillable = entry?.isBillable ?? _isBillable;
    String? formError;

    final draft = await showDialog<_ManualEntryDraft>(
      context: context,
      builder: (dialogContext) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            Future<void> pickDate() async {
              final picked = await showDatePicker(
                context: dialogContext,
                initialDate: selectedDate,
                firstDate: DateTime(2020),
                lastDate: DateTime(2100),
              );
              if (picked == null) {
                return;
              }
              setDialogState(() {
                selectedDate = picked;
              });
            }

            Future<void> pickTime({required bool isStart}) async {
              final initialTime = isStart
                  ? (startTime ?? const TimeOfDay(hour: 9, minute: 0))
                  : (endTime ?? const TimeOfDay(hour: 10, minute: 0));
              final picked = await showTimePicker(
                context: dialogContext,
                initialTime: initialTime,
              );
              if (picked == null) {
                return;
              }
              setDialogState(() {
                if (isStart) {
                  startTime = picked;
                } else {
                  endTime = picked;
                }
              });
            }

            return AlertDialog(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(24),
              ),
              title: Text(dialogTitle),
              content: SizedBox(
                width: 560,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      DropdownButtonFormField<int>(
                        key: ValueKey<int?>(selectedProjectId),
                        initialValue: selectedProjectId,
                        isExpanded: true,
                        decoration: const InputDecoration(
                          labelText: 'Project name',
                        ),
                        items: _projectOptions
                            .map(
                              (project) => DropdownMenuItem<int>(
                                value: project.id,
                                child: Text(
                                  project.name,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            )
                            .toList(growable: false),
                        onChanged: (value) {
                          setDialogState(() {
                            selectedProjectId = value;
                          });
                        },
                      ),
                      const SizedBox(height: 16),
                      TextField(
                        controller: descriptionController,
                        maxLength: 1000,
                        maxLines: 3,
                        decoration: const InputDecoration(
                          labelText: 'Description',
                          hintText: 'Describe the work you completed',
                        ),
                      ),
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 10,
                        runSpacing: 10,
                        children: [
                          OutlinedButton.icon(
                            onPressed: pickDate,
                            icon: const Icon(Icons.calendar_month_rounded),
                            label: Text(_formatPickerDate(selectedDate)),
                          ),
                          ChoiceChip(
                            label: const Text('Time range'),
                            selected: useTimeRange,
                            onSelected: (selected) {
                              setDialogState(() {
                                useTimeRange = true;
                                formError = null;
                              });
                            },
                          ),
                          ChoiceChip(
                            label: const Text('Duration only'),
                            selected: !useTimeRange,
                            onSelected: (selected) {
                              setDialogState(() {
                                useTimeRange = false;
                                formError = null;
                              });
                            },
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      if (useTimeRange)
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: () => pickTime(isStart: true),
                                icon: const Icon(Icons.schedule_rounded),
                                label: Text(
                                  startTime == null
                                      ? 'Start time'
                                      : startTime!.format(context),
                                ),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: () => pickTime(isStart: false),
                                icon: const Icon(Icons.schedule_rounded),
                                label: Text(
                                  endTime == null
                                      ? 'End time'
                                      : endTime!.format(context),
                                ),
                              ),
                            ),
                          ],
                        )
                      else
                        TextField(
                          controller: durationController,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Duration (minutes)',
                            hintText: 'Example: 90',
                          ),
                        ),
                      const SizedBox(height: 12),
                      SwitchListTile.adaptive(
                        contentPadding: EdgeInsets.zero,
                        value: isBillable,
                        onChanged: (value) {
                          setDialogState(() {
                            isBillable = value;
                          });
                        },
                        title: const Text('Billable'),
                      ),
                      if (formError != null) ...[
                        const SizedBox(height: 8),
                        Text(
                          formError!,
                          style: const TextStyle(
                            color: Color(0xFFD9465F),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.of(dialogContext).pop(),
                  child: const Text('Cancel'),
                ),
                FilledButton(
                  onPressed: () {
                    final projectId = selectedProjectId;
                    if (projectId == null) {
                      setDialogState(() {
                        formError = 'Select a project first.';
                      });
                      return;
                    }

                    final description = descriptionController.text.trim();
                    if (description.length > 1000) {
                      setDialogState(() {
                        formError =
                            'Description must be 1000 characters or fewer.';
                      });
                      return;
                    }

                    DateTime? startUtc;
                    DateTime? endUtc;
                    int? durationMinutes;

                    if (useTimeRange) {
                      if (startTime == null || endTime == null) {
                        setDialogState(() {
                          formError = 'Pick both a start and end time.';
                        });
                        return;
                      }

                      final localStart =
                          _combineLocalDateAndTime(selectedDate, startTime!);
                      final localEnd =
                          _combineLocalDateAndTime(selectedDate, endTime!);
                      if (!localEnd.isAfter(localStart)) {
                        setDialogState(() {
                          formError = 'End time must be after start time.';
                        });
                        return;
                      }

                      startUtc = localStart.toUtc();
                      endUtc = localEnd.toUtc();

                      if (_overlapsActiveTimer(
                        startUtc: startUtc,
                        endUtc: endUtc,
                        editingEntryId: entry?.id,
                      )) {
                        setDialogState(() {
                          formError =
                              'This range overlaps with your running timer.';
                        });
                        return;
                      }
                    } else {
                      durationMinutes =
                          int.tryParse(durationController.text.trim());
                      if (durationMinutes == null || durationMinutes <= 0) {
                        setDialogState(() {
                          formError = 'Enter a positive duration in minutes.';
                        });
                        return;
                      }
                    }

                    Navigator.of(dialogContext).pop(
                      _ManualEntryDraft(
                        id: entry?.id,
                        projectId: projectId,
                        entryDateUtc: _utcDateOnly(selectedDate),
                        startTimeUtc: startUtc,
                        endTimeUtc: endUtc,
                        durationMinutes: durationMinutes,
                        description: description,
                        isBillable: isBillable,
                      ),
                    );
                  },
                  child: Text(isEditing ? 'Save changes' : 'Create entry'),
                ),
              ],
            );
          },
        );
      },
    );

    descriptionController.dispose();
    durationController.dispose();

    if (draft == null) {
      return false;
    }

    setState(() {
      _submitting = true;
    });

    try {
      final api = ref.read(timeTrackerApiClientProvider);
      if (draft.id == null) {
        await api.createTimeEntry(
          projectId: draft.projectId,
          entryDate: draft.entryDateUtc,
          description: draft.description,
          isBillable: draft.isBillable,
          startTimeUtc: draft.startTimeUtc,
          endTimeUtc: draft.endTimeUtc,
          durationMinutes: draft.durationMinutes,
        );
      } else {
        await api.updateTimeEntry(
          id: draft.id!,
          projectId: draft.projectId,
          entryDate: draft.entryDateUtc,
          description: draft.description,
          isBillable: draft.isBillable,
          startTimeUtc: draft.startTimeUtc,
          endTimeUtc: draft.endTimeUtc,
          durationMinutes: draft.durationMinutes,
        );
      }

      await _loadDashboard(showLoading: false);
      _showMessage(isEditing ? 'Time entry updated.' : 'Manual entry added.');
      return true;
    } catch (error) {
      await _handleProtectedError(error);
      _showMessage(
        describeApiError(
          error,
          fallback: isEditing
              ? 'Time entry could not be updated.'
              : 'Manual entry could not be created.',
        ),
      );
      return false;
    } finally {
      if (mounted) {
        setState(() {
          _submitting = false;
        });
      }
    }
  }

  Future<void> _logout() async {
    await ref.read(authSessionControllerProvider.notifier).signOut();
  }

  Future<void> _handleProtectedError(Object error) async {
    if (error is DioException && error.response?.statusCode == 401) {
      await ref.read(authSessionControllerProvider.notifier).signOut();
    }
  }

  void _showMessage(String message) {
    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }

  void _showSettingsMessage() {
    _showMessage('Settings panel is not available yet.');
  }

  void _startTicker() {
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted && _activeTimer != null) {
        setState(() {});
      }
    });
  }

  Duration get _activeDuration {
    if (_activeTimer == null) {
      return Duration.zero;
    }

    final duration =
        DateTime.now().toUtc().difference(_activeTimer!.startedAtUtc);
    return duration.isNegative ? Duration.zero : duration;
  }

  List<_ProjectOption> get _projectOptions {
    final options = _knownProjectIds
        .map(
          (id) => _ProjectOption(
            id: id,
            name: _projectNameFor(id),
          ),
        )
        .toList(growable: false);

    options.sort(
      (left, right) => left.name.toLowerCase().compareTo(
            right.name.toLowerCase(),
          ),
    );

    return options;
  }

  String? get _selectedProjectName {
    final projectId = _selectedProjectId;
    if (projectId == null) {
      return null;
    }
    return _projectNameFor(projectId);
  }

  Map<int, String> _mergeProjectNames({
    required Map<int, String> existingNames,
    required List<int> projectIds,
  }) {
    return <int, String>{
      for (final id in projectIds)
        id: _normalizeProjectName(existingNames[id], id),
    };
  }

  String _normalizeProjectName(String? rawName, int projectId) {
    final normalized = rawName?.trim();
    final temporaryName = _temporaryProjectNames[projectId];
    if (temporaryName != null &&
        (normalized == null ||
            normalized.isEmpty ||
            normalized == _fallbackProjectName(projectId))) {
      return temporaryName;
    }
    if (normalized != null && normalized.isNotEmpty) {
      return normalized;
    }
    return _fallbackProjectName(projectId);
  }

  String _projectNameFor(int projectId) {
    return _normalizeProjectName(_projectNames[projectId], projectId);
  }

  List<TimeEntryRecord> _sortedEntries(List<TimeEntryRecord> items) {
    final sorted = items.toList();
    sorted.sort((left, right) {
      final comparison = _entryAnchor(right).compareTo(_entryAnchor(left));
      if (comparison != 0) {
        return comparison;
      }
      return right.id.compareTo(left.id);
    });
    return sorted;
  }

  int? _resolveSelectedProjectId({
    required int? currentValue,
    required int? activeProjectId,
    required List<int> knownProjectIds,
  }) {
    if (activeProjectId != null) {
      return activeProjectId;
    }
    if (currentValue != null && knownProjectIds.contains(currentValue)) {
      return currentValue;
    }
    return knownProjectIds.isEmpty ? null : knownProjectIds.first;
  }

  TimeEntryRecord? _firstTimerOverlap() {
    final activeTimer = _activeTimer;
    if (activeTimer == null) {
      return null;
    }

    final timerStartUtc = activeTimer.startedAtUtc;
    final timerEndUtc = DateTime.now().toUtc();
    if (!timerEndUtc.isAfter(timerStartUtc)) {
      return null;
    }

    for (final entry in _entries) {
      final entryStartUtc = entry.startTimeUtc;
      final entryEndUtc = entry.endTimeUtc;
      if (entryStartUtc == null || entryEndUtc == null) {
        continue;
      }
      if (_rangesOverlap(
          timerStartUtc, timerEndUtc, entryStartUtc, entryEndUtc)) {
        return entry;
      }
    }

    return null;
  }

  bool _overlapsActiveTimer({
    required DateTime? startUtc,
    required DateTime? endUtc,
    int? editingEntryId,
  }) {
    final activeTimer = _activeTimer;
    if (activeTimer == null || startUtc == null || endUtc == null) {
      return false;
    }

    if (!endUtc.isAfter(startUtc)) {
      return false;
    }

    final timerStartUtc = activeTimer.startedAtUtc;
    final timerEndUtc = DateTime.now().toUtc();
    if (!timerEndUtc.isAfter(timerStartUtc)) {
      return false;
    }

    final currentOverlap = _firstTimerOverlap();
    if (currentOverlap != null && currentOverlap.id == editingEntryId) {
      return _rangesOverlap(timerStartUtc, timerEndUtc, startUtc, endUtc);
    }

    return _rangesOverlap(timerStartUtc, timerEndUtc, startUtc, endUtc);
  }

  void _handleSectionChange(_AppSection section) {
    setState(() {
      _activeSection = section;
    });
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authSessionControllerProvider);
    final session = authState.session;
    if (session == null) {
      return const SizedBox.shrink();
    }

    final profile = _liveProfile ?? session.profile;
    final isCompact = MediaQuery.sizeOf(context).width < 1100;

    if (isCompact) {
      return Scaffold(
        backgroundColor: const Color(0xFFF5F7FB),
        appBar: AppBar(
          backgroundColor: Colors.white,
          surfaceTintColor: Colors.transparent,
          title: Text(_sectionTitle(_activeSection)),
        ),
        drawer: Drawer(
          child: SafeArea(
            child: _Sidebar(
              profile: profile,
              logoutBusy: _submitting,
              activeSection: _activeSection,
              onSectionChanged: (section) {
                Navigator.of(context).pop();
                _handleSectionChange(section);
              },
              onSettings: _showSettingsMessage,
              onLogout: _logout,
            ),
          ),
        ),
        body: _buildSectionContent(profile, isCompact),
      );
    }

    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FB),
      body: SafeArea(
        child: Row(
          children: [
            SizedBox(
              width: 286,
              child: _Sidebar(
                profile: profile,
                logoutBusy: _submitting,
                activeSection: _activeSection,
                onSectionChanged: _handleSectionChange,
                onSettings: _showSettingsMessage,
                onLogout: _logout,
              ),
            ),
            Expanded(
              child: _buildSectionContent(profile, isCompact),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSectionContent(AuthProfile profile, bool isCompact) {
    switch (_activeSection) {
      case _AppSection.timeTracker:
        return _buildMainArea(profile, isCompact);
      case _AppSection.report:
        return _PlaceholderSection(
          icon: Icons.bar_chart_rounded,
          title: 'Reports',
          description:
              'View detailed reports on time tracked across projects and team members.',
        );
      case _AppSection.expenses:
        return _PlaceholderSection(
          icon: Icons.receipt_long_rounded,
          title: 'Expenses',
          description:
              'Track and manage project expenses and reimbursements.',
        );
      case _AppSection.calendar:
        return _PlaceholderSection(
          icon: Icons.calendar_month_rounded,
          title: 'Calendar',
          description:
              'View your time entries and scheduled work in a calendar layout.',
        );
      case _AppSection.projects:
        return _PlaceholderSection(
          icon: Icons.folder_copy_rounded,
          title: 'Projects',
          description:
              'Manage your projects, set budgets, and track project progress.',
        );
      case _AppSection.members:
        return _PlaceholderSection(
          icon: Icons.groups_rounded,
          title: 'Members',
          description:
              'Manage team members, roles, and workspace access.',
        );
    }
  }

  static String _sectionTitle(_AppSection section) {
    switch (section) {
      case _AppSection.timeTracker:
        return 'Time Tracker';
      case _AppSection.report:
        return 'Reports';
      case _AppSection.expenses:
        return 'Expenses';
      case _AppSection.calendar:
        return 'Calendar';
      case _AppSection.projects:
        return 'Projects';
      case _AppSection.members:
        return 'Members';
    }
  }

  Widget _buildMainArea(AuthProfile profile, bool isCompact) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_screenError != null && _entries.isEmpty && _activeTimer == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: _ErrorCard(
              message: _screenError!,
              onRetry: _loadDashboard,
            ),
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => _loadDashboard(showLoading: false),
      child: ListView(
        padding: EdgeInsets.fromLTRB(
            isCompact ? 16 : 30, 20, isCompact ? 16 : 30, 24),
        children: [
          _TopTrackerBar(
            profile: profile,
            descriptionController: _descriptionController,
            projectOptions: _projectOptions,
            selectedProjectId: _selectedProjectId,
            selectedProjectName: _selectedProjectName,
            activeProjectName: _activeTimer == null
                ? null
                : _projectNameFor(_activeTimer!.projectId),
            durationText: _formatDuration(_activeDuration),
            hasActiveTimer: _activeTimer != null,
            isBillable: _isBillable,
            isCompact: isCompact,
            submitting: _submitting,
            onProjectChanged: (value) {
              setState(() {
                _selectedProjectId = value;
              });
            },
            onBillableChanged: (value) {
              setState(() {
                _isBillable = value;
              });
            },
            onManualEntry: () async {
              await _showManualEntryDialog();
            },
            onPrimaryAction: _activeTimer != null ? _stopTimer : _startTimer,
          ),
          if (_screenError != null) ...[
            const SizedBox(height: 16),
            _InlineErrorBanner(
              message: _screenError!,
              onRetry: () => _loadDashboard(showLoading: false),
            ),
          ],
          const SizedBox(height: 18),
          _SummaryStrip(
            entries: _entries,
            activeTimer: _activeTimer,
            activeDuration: _activeDuration,
            distinctProjectCount: _knownProjectIds.length,
          ),
          const SizedBox(height: 28),
          ..._buildEntrySections(),
        ],
      ),
    );
  }

  List<Widget> _buildEntrySections() {
    if (_entries.isEmpty) {
      return <Widget>[
        _EmptyEntriesCard(
          onAddEntry: () async {
            await _showManualEntryDialog();
          },
        ),
      ];
    }

    final sections = <DateTime, List<TimeEntryRecord>>{};
    for (final entry in _entries) {
      final anchor = _entryAnchor(entry).toLocal();
      final key = DateTime(anchor.year, anchor.month, anchor.day);
      sections.putIfAbsent(key, () => <TimeEntryRecord>[]).add(entry);
    }

    final orderedDays = sections.keys.toList()
      ..sort((left, right) => right.compareTo(left));

    final widgets = <Widget>[];
    for (final day in orderedDays) {
      final entriesForDay = sections[day]!;
      final totalMinutes = entriesForDay.fold<int>(
        0,
        (sum, entry) => sum + entry.durationMinutes,
      );

      widgets.add(
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  _formatDateLabel(day),
                  style: const TextStyle(
                    color: Color(0xFF132039),
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              Text(
                'Total: ${_formatDuration(Duration(minutes: totalMinutes))}',
                style: const TextStyle(
                  color: Color(0xFF61708C),
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      );

      widgets.addAll(
        entriesForDay.map(
          (entry) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: _EntryCard(
              entry: entry,
              projectName: _projectNameFor(entry.projectId),
              onEdit: () async {
                await _showManualEntryDialog(entry: entry);
              },
              onDelete: () async {
                await _deleteEntry(entry);
              },
            ),
          ),
        ),
      );

      widgets.add(const SizedBox(height: 16));
    }

    return widgets;
  }
}

class _PlaceholderSection extends StatelessWidget {
  const _PlaceholderSection({
    required this.icon,
    required this.title,
    required this.description,
  });

  final IconData icon;
  final String title;
  final String description;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 440),
          child: Container(
            padding: const EdgeInsets.all(36),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(24),
              border: Border.all(color: const Color(0xFFE4EAF4)),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 72,
                  height: 72,
                  decoration: BoxDecoration(
                    color: const Color(0xFFEAF4FF),
                    borderRadius: BorderRadius.circular(24),
                  ),
                  child: Icon(
                    icon,
                    size: 36,
                    color: const Color(0xFF1E7BF2),
                  ),
                ),
                const SizedBox(height: 20),
                Text(
                  title,
                  style: const TextStyle(
                    color: Color(0xFF132039),
                    fontSize: 22,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  description,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: Color(0xFF61708C),
                    fontSize: 15,
                    fontWeight: FontWeight.w500,
                    height: 1.5,
                  ),
                ),
                const SizedBox(height: 24),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 18,
                    vertical: 10,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF5F7FB),
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: const Text(
                    'Coming soon',
                    style: TextStyle(
                      color: Color(0xFF5B6B86),
                      fontSize: 13,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

enum _AppSection {
  timeTracker,
  report,
  expenses,
  calendar,
  projects,
  members,
}

class _Sidebar extends StatelessWidget {
  const _Sidebar({
    required this.profile,
    required this.logoutBusy,
    required this.activeSection,
    required this.onSectionChanged,
    required this.onSettings,
    required this.onLogout,
  });

  final AuthProfile profile;
  final bool logoutBusy;
  final _AppSection activeSection;
  final ValueChanged<_AppSection> onSectionChanged;
  final VoidCallback onSettings;
  final VoidCallback onLogout;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(18, 14, 18, 14),
            child: Row(
              children: [
                Container(
                  height: 38,
                  width: 38,
                  decoration: BoxDecoration(
                    color: const Color(0xFF1E7BF2),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: const Icon(
                    Icons.access_time_rounded,
                    color: Colors.white,
                    size: 22,
                  ),
                ),
                const SizedBox(width: 10),
                const Text(
                  'Flux',
                  style: TextStyle(
                    color: Color(0xFF132039),
                    fontSize: 20,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                _NavItem(
                  icon: Icons.access_time_rounded,
                  label: 'Time Tracker',
                  selected: activeSection == _AppSection.timeTracker,
                  onTap: () => onSectionChanged(_AppSection.timeTracker),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.bar_chart_rounded,
                  label: 'Reports',
                  selected: activeSection == _AppSection.report,
                  onTap: () => onSectionChanged(_AppSection.report),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.receipt_long_rounded,
                  label: 'Expenses',
                  selected: activeSection == _AppSection.expenses,
                  onTap: () => onSectionChanged(_AppSection.expenses),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.calendar_month_rounded,
                  label: 'Calendar',
                  selected: activeSection == _AppSection.calendar,
                  onTap: () => onSectionChanged(_AppSection.calendar),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.folder_copy_rounded,
                  label: 'Projects',
                  selected: activeSection == _AppSection.projects,
                  onTap: () => onSectionChanged(_AppSection.projects),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.groups_rounded,
                  label: 'Members',
                  selected: activeSection == _AppSection.members,
                  onTap: () => onSectionChanged(_AppSection.members),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 18),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundColor: const Color(0xFF1E7BF2),
                  child: Text(
                    _initialsFor(profile.displayName),
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        profile.displayName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Color(0xFF132039),
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      if (profile.role != null &&
                          profile.role!.trim().isNotEmpty) ...[
                        const SizedBox(height: 4),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 3,
                          ),
                          decoration: BoxDecoration(
                            color: const Color(0xFFF1F5F9),
                            borderRadius: BorderRadius.circular(999),
                            border: Border.all(color: const Color(0xFFE2E8F0)),
                          ),
                          child: Text(
                            _titleCaseRole(profile.role!),
                            style: const TextStyle(
                              color: Color(0xFF5B6B86),
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                      ],
                      const SizedBox(height: 4),
                      Text(
                        profile.email,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Color(0xFF61708C),
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                ),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    IconButton(
                      tooltip: 'Settings',
                      onPressed: logoutBusy ? null : onSettings,
                      icon: const Icon(Icons.settings_rounded),
                    ),
                    IconButton(
                      tooltip: 'Logout',
                      onPressed: logoutBusy ? null : onLogout,
                      icon: const Icon(Icons.logout_rounded),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.icon,
    required this.label,
    required this.onTap,
    this.selected = false,
  });

  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 15),
        decoration: BoxDecoration(
          color: selected ? const Color(0xFFDCEEFF) : Colors.transparent,
          borderRadius: BorderRadius.circular(16),
        ),
        child: Row(
          children: [
            Icon(
              icon,
              color:
                  selected ? const Color(0xFF1E7BF2) : const Color(0xFF728099),
            ),
            const SizedBox(width: 14),
            Text(
              label,
              style: TextStyle(
                color: selected
                    ? const Color(0xFF1E7BF2)
                    : const Color(0xFF53627C),
                fontSize: 15,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TopTrackerBar extends StatelessWidget {
  const _TopTrackerBar({
    required this.profile,
    required this.descriptionController,
    required this.projectOptions,
    required this.selectedProjectId,
    required this.selectedProjectName,
    required this.activeProjectName,
    required this.durationText,
    required this.hasActiveTimer,
    required this.isBillable,
    required this.isCompact,
    required this.submitting,
    required this.onProjectChanged,
    required this.onBillableChanged,
    required this.onManualEntry,
    required this.onPrimaryAction,
  });

  final AuthProfile profile;
  final TextEditingController descriptionController;
  final List<_ProjectOption> projectOptions;
  final int? selectedProjectId;
  final String? selectedProjectName;
  final String? activeProjectName;
  final String durationText;
  final bool hasActiveTimer;
  final bool isBillable;
  final bool isCompact;
  final bool submitting;
  final ValueChanged<int?> onProjectChanged;
  final ValueChanged<bool> onBillableChanged;
  final Future<void> Function() onManualEntry;
  final Future<void> Function() onPrimaryAction;

  @override
  Widget build(BuildContext context) {
    final title = hasActiveTimer
        ? 'You are currently tracking'
        : 'What are you working on?';
    final projectHint = hasActiveTimer
        ? activeProjectName ?? selectedProjectName ?? 'Select project'
        : selectedProjectName ?? 'Select project';

    final inputField = isCompact
        ? SizedBox(
            width: double.infinity,
            child: _TrackerInput(
              controller: descriptionController,
              enabled: !hasActiveTimer && !submitting,
              hintText: title,
            ),
          )
        : Expanded(
            flex: 5,
            child: _TrackerInput(
              controller: descriptionController,
              enabled: !hasActiveTimer && !submitting,
              hintText: title,
            ),
          );

    final projectDropdown = SizedBox(
      width: isCompact ? double.infinity : 240,
      child: DropdownButtonFormField<int>(
        initialValue: selectedProjectId,
        isExpanded: true,
        decoration: InputDecoration(
          hintText: 'Select project',
          filled: true,
          fillColor: const Color(0xFFF5F7FB),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(18),
            borderSide: BorderSide.none,
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(18),
            borderSide: BorderSide.none,
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(18),
            borderSide: const BorderSide(color: Color(0xFF1E7BF2), width: 1.2),
          ),
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 18, vertical: 18),
        ),
        hint: Text(projectHint),
        items: projectOptions
            .map(
              (project) => DropdownMenuItem<int>(
                value: project.id,
                child: Text(
                  project.name,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            )
            .toList(growable: false),
        onChanged: hasActiveTimer || submitting ? null : onProjectChanged,
      ),
    );

    final timerReadout = _TimerReadout(
      durationText: durationText,
      isRunning: hasActiveTimer,
    );

    final primaryButton = SizedBox(
      height: 60,
      child: FilledButton.icon(
        onPressed: submitting ? null : onPrimaryAction,
        style: FilledButton.styleFrom(
          backgroundColor: hasActiveTimer
              ? const Color(0xFFEF4444)
              : const Color(0xFF1E7BF2),
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 18),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
          ),
        ),
        icon: Icon(
          hasActiveTimer ? Icons.stop_rounded : Icons.play_arrow_rounded,
        ),
        label: Text(
          hasActiveTimer ? 'Stop' : 'Start',
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
      ),
    );

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: const Color(0xFFE4EAF4)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (isCompact) ...[
            inputField,
            const SizedBox(height: 12),
            projectDropdown,
            const SizedBox(height: 12),
            timerReadout,
            const SizedBox(height: 12),
            SizedBox(width: double.infinity, child: primaryButton),
          ] else ...[
            Row(
              children: [
                inputField,
                const SizedBox(width: 16),
                projectDropdown,
                const SizedBox(width: 16),
                timerReadout,
                const SizedBox(width: 16),
                primaryButton,
              ],
            ),
          ],
          const SizedBox(height: 14),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                decoration: BoxDecoration(
                  color: const Color(0xFFF5F7FB),
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  hasActiveTimer
                      ? 'Running on ${activeProjectName ?? 'your selected project'}'
                      : 'Welcome back, ${profile.firstName ?? profile.displayName}',
                  style: const TextStyle(
                    color: Color(0xFF53627C),
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              FilterChip(
                label: Text(isBillable ? 'Billable' : 'Non-billable'),
                selected: isBillable,
                onSelected:
                    hasActiveTimer || submitting ? null : onBillableChanged,
                side: const BorderSide(color: Color(0xFFE4EAF4)),
                backgroundColor: Colors.white,
                selectedColor: const Color(0xFFEAF4FF),
              ),
              TextButton.icon(
                onPressed: submitting ? null : onManualEntry,
                icon: const Icon(Icons.edit_calendar_rounded),
                label: const Text('Add manual entry'),
              ),
            ],
          ),
          if (projectOptions.isEmpty) ...[
            const SizedBox(height: 10),
            const Text(
              'No project is available for this account yet.',
              style: TextStyle(
                color: Color(0xFF6B7891),
                fontSize: 13,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _TrackerInput extends StatelessWidget {
  const _TrackerInput({
    required this.controller,
    required this.enabled,
    required this.hintText,
  });

  final TextEditingController controller;
  final bool enabled;
  final String hintText;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      enabled: enabled,
      decoration: InputDecoration(
        hintText: hintText,
        filled: true,
        fillColor: const Color(0xFFF5F7FB),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(18),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(18),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(18),
          borderSide: const BorderSide(color: Color(0xFF1E7BF2), width: 1.2),
        ),
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 18, vertical: 18),
      ),
    );
  }
}

class _TimerReadout extends StatelessWidget {
  const _TimerReadout({
    required this.durationText,
    required this.isRunning,
  });

  final String durationText;
  final bool isRunning;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isRunning ? 'Live timer' : 'Timer',
            style: const TextStyle(
              color: Color(0xFF6B7891),
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            durationText,
            style: TextStyle(
              color:
                  isRunning ? const Color(0xFF132039) : const Color(0xFF24324A),
              fontSize: 23,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.0,
              fontFamily: 'monospace',
            ),
          ),
        ],
      ),
    );
  }
}

class _SummaryStrip extends StatelessWidget {
  const _SummaryStrip({
    required this.entries,
    required this.activeTimer,
    required this.activeDuration,
    required this.distinctProjectCount,
  });

  final List<TimeEntryRecord> entries;
  final RunningTimerRecord? activeTimer;
  final Duration activeDuration;
  final int distinctProjectCount;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now().toLocal();
    final todayStart = DateTime(now.year, now.month, now.day);
    final weekStart =
        todayStart.subtract(Duration(days: todayStart.weekday - 1));
    final nextWeekStart = weekStart.add(const Duration(days: 7));

    var todayMinutes = 0;
    var weekMinutes = 0;

    for (final entry in entries) {
      final businessDate = _entryBusinessDate(entry);
      if (_isSameDay(businessDate, todayStart)) {
        todayMinutes += entry.durationMinutes;
      }
      if (!businessDate.isBefore(weekStart) &&
          businessDate.isBefore(nextWeekStart)) {
        weekMinutes += entry.durationMinutes;
      }
    }

    if (activeTimer != null) {
      final startedLocal = activeTimer!.startedAtUtc.toLocal();
      if (_isSameDay(startedLocal, now)) {
        todayMinutes += activeDuration.inMinutes;
      }
      if (!startedLocal.isBefore(weekStart) &&
          startedLocal.isBefore(nextWeekStart)) {
        weekMinutes += activeDuration.inMinutes;
      }
    }

    final items = <_SummaryItemData>[
      _SummaryItemData(
        label: 'Today',
        value: _formatDuration(Duration(minutes: todayMinutes)),
        accent: const Color(0xFF1E7BF2),
      ),
      _SummaryItemData(
        label: 'This week',
        value: _formatDuration(Duration(minutes: weekMinutes)),
        accent: const Color(0xFF10B981),
      ),
      _SummaryItemData(
        label: 'Projects',
        value: distinctProjectCount.toString(),
        accent: const Color(0xFFF59E0B),
      ),
    ];

    return Wrap(
      spacing: 14,
      runSpacing: 14,
      children: items
          .map(
            (item) => _SummaryTile(data: item),
          )
          .toList(growable: false),
    );
  }
}

class _SummaryItemData {
  const _SummaryItemData({
    required this.label,
    required this.value,
    required this.accent,
  });

  final String label;
  final String value;
  final Color accent;
}

class _SummaryTile extends StatelessWidget {
  const _SummaryTile({
    required this.data,
  });

  final _SummaryItemData data;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 220,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: const Color(0xFFE4EAF4)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            height: 10,
            width: 46,
            decoration: BoxDecoration(
              color: data.accent.withValues(alpha: 0.16),
              borderRadius: BorderRadius.circular(999),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            data.label,
            style: const TextStyle(
              color: Color(0xFF61708C),
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            data.value,
            style: const TextStyle(
              color: Color(0xFF132039),
              fontSize: 20,
              fontWeight: FontWeight.w800,
            ),
          ),
        ],
      ),
    );
  }
}

class _EntryCard extends StatelessWidget {
  const _EntryCard({
    required this.entry,
    required this.projectName,
    this.onEdit,
    this.onDelete,
  });

  final TimeEntryRecord entry;
  final String projectName;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;

  @override
  Widget build(BuildContext context) {
    final title = entry.description.trim().isEmpty
        ? 'Untitled session'
        : entry.description.trim();
    final sourceText = entry.sourceType;
    final durationText =
        _formatDuration(Duration(minutes: entry.durationMinutes));
    final timeText = _formatEntryTime(entry);

    return LayoutBuilder(
      builder: (context, constraints) {
        final compact = constraints.maxWidth < 900;

        final details = Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(
                color: Color(0xFF132039),
                fontSize: 17,
                fontWeight: FontWeight.w700,
                height: 1.25,
              ),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    _Dot(
                      color: entry.isBillable
                          ? const Color(0xFF10B981)
                          : const Color(0xFFF59E0B),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      projectName,
                      style: const TextStyle(
                        color: Color(0xFF4B5A73),
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
                _EntryTag(
                  label: sourceText,
                  backgroundColor: const Color(0xFFF8FAFC),
                  foregroundColor: const Color(0xFF66758F),
                ),
                _BillingTag(isBillable: entry.isBillable),
              ],
            ),
          ],
        );

        final actions = Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (onEdit != null)
              _EntryActionButton(
                icon: Icons.edit_outlined,
                tooltip: 'Edit entry',
                onPressed: onEdit!,
              ),
            if (onEdit != null && onDelete != null) const SizedBox(width: 8),
            if (onDelete != null)
              _EntryActionButton(
                icon: Icons.delete_outline_rounded,
                tooltip: 'Delete entry',
                foregroundColor: const Color(0xFFD9465F),
                backgroundColor: const Color(0xFFFFF1F3),
                borderColor: const Color(0xFFF5D1D8),
                onPressed: onDelete!,
              ),
          ],
        );

        final durationPill = Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: const Color(0xFFF8FAFC),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: const Color(0xFFE2E8F0)),
          ),
          child: Text(
            durationText,
            style: const TextStyle(
              color: Color(0xFF132039),
              fontSize: 18,
              fontWeight: FontWeight.w800,
              fontFamily: 'monospace',
              letterSpacing: 0.5,
            ),
          ),
        );

        final rightRail = compact
            ? Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SizedBox(height: 14),
                  Container(height: 1, color: const Color(0xFFE8EDF5)),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          timeText,
                          style: const TextStyle(
                            color: Color(0xFF61708C),
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      durationPill,
                      if (onEdit != null || onDelete != null) ...[
                        const SizedBox(width: 10),
                        actions,
                      ],
                    ],
                  ),
                ],
              )
            : Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 1,
                    height: 56,
                    color: const Color(0xFFE8EDF5),
                  ),
                  const SizedBox(width: 18),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        timeText,
                        style: const TextStyle(
                          color: Color(0xFF61708C),
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 10),
                      Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          durationPill,
                          if (onEdit != null || onDelete != null) ...[
                            const SizedBox(width: 10),
                            actions,
                          ],
                        ],
                      ),
                    ],
                  ),
                ],
              );

        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: const Color(0xFFE4EAF4)),
          ),
          child: compact
              ? Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    details,
                    rightRail,
                  ],
                )
              : Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Expanded(child: details),
                    const SizedBox(width: 18),
                    rightRail,
                  ],
                ),
        );
      },
    );
  }
}

class _Dot extends StatelessWidget {
  const _Dot({
    required this.color,
  });

  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 10,
      width: 10,
      decoration: BoxDecoration(
        color: color,
        shape: BoxShape.circle,
      ),
    );
  }
}

class _EntryActionButton extends StatelessWidget {
  const _EntryActionButton({
    required this.icon,
    required this.tooltip,
    required this.onPressed,
    this.foregroundColor = const Color(0xFF5B6B86),
    this.backgroundColor = Colors.white,
    this.borderColor = const Color(0xFFE2E8F0),
  });

  final IconData icon;
  final String tooltip;
  final VoidCallback onPressed;
  final Color foregroundColor;
  final Color backgroundColor;
  final Color borderColor;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(10),
        child: Ink(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: backgroundColor,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: borderColor),
          ),
          child: Icon(icon, size: 18, color: foregroundColor),
        ),
      ),
    );
  }
}

class _EntryTag extends StatelessWidget {
  const _EntryTag({
    required this.label,
    required this.backgroundColor,
    required this.foregroundColor,
  });

  final String label;
  final Color backgroundColor;
  final Color foregroundColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: foregroundColor,
          fontSize: 11,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _BillingTag extends StatelessWidget {
  const _BillingTag({
    required this.isBillable,
  });

  final bool isBillable;

  @override
  Widget build(BuildContext context) {
    final backgroundColor =
        isBillable ? const Color(0xFFEFFAF5) : const Color(0xFFF8FAFC);
    final foregroundColor =
        isBillable ? const Color(0xFF0F8B61) : const Color(0xFF64748B);
    final iconBackgroundColor =
        isBillable ? Colors.white : const Color(0xFF94A3B8);
    final iconForegroundColor =
        isBillable ? const Color(0xFF10B981) : Colors.white;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(
          color: isBillable ? const Color(0xFFCDEEDD) : const Color(0xFFE2E8F0),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 16,
            height: 16,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: iconBackgroundColor,
              shape: BoxShape.circle,
            ),
            child: Text(
              '\$',
              style: TextStyle(
                color: iconForegroundColor,
                fontSize: 11,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
          const SizedBox(width: 6),
          Text(
            isBillable ? 'Billable' : 'Unbillable',
            style: TextStyle(
              color: foregroundColor,
              fontSize: 11,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _InlineErrorBanner extends StatelessWidget {
  const _InlineErrorBanner({
    required this.message,
    required this.onRetry,
  });

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF4F4),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFF3C9CF)),
      ),
      child: Row(
        children: [
          const Icon(Icons.error_outline_rounded, color: Color(0xFFD9465F)),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(
                color: Color(0xFF6A2B36),
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          TextButton(
            onPressed: onRetry,
            child: const Text('Retry'),
          ),
        ],
      ),
    );
  }
}

class _ErrorCard extends StatelessWidget {
  const _ErrorCard({
    required this.message,
    required this.onRetry,
  });

  final String message;
  final Future<void> Function({bool showLoading}) onRetry;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: const Color(0xFFE4EAF4)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_rounded,
            size: 42,
            color: Color(0xFFD9465F),
          ),
          const SizedBox(height: 14),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Color(0xFF4B5871),
              fontSize: 15,
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: () => onRetry(showLoading: true),
            icon: const Icon(Icons.refresh_rounded),
            label: const Text('Try again'),
          ),
        ],
      ),
    );
  }
}

class _EmptyEntriesCard extends StatelessWidget {
  const _EmptyEntriesCard({
    required this.onAddEntry,
  });

  final Future<void> Function() onAddEntry;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(30),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: const Color(0xFFE4EAF4)),
      ),
      child: Column(
        children: [
          const Icon(
            Icons.timer_outlined,
            size: 46,
            color: Color(0xFF1E7BF2),
          ),
          const SizedBox(height: 14),
          const Text(
            'No saved entries yet',
            style: TextStyle(
              color: Color(0xFF132039),
              fontSize: 20,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 8),
          const Text(
            'Add a manual entry and your saved work logs will appear here in a clean daily timeline.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Color(0xFF61708C),
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            alignment: WrapAlignment.center,
            children: [
              FilledButton.icon(
                onPressed: onAddEntry,
                icon: const Icon(Icons.edit_calendar_rounded),
                label: const Text('Add manual entry'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ManualEntryDraft {
  const _ManualEntryDraft({
    this.id,
    required this.projectId,
    required this.entryDateUtc,
    required this.description,
    required this.isBillable,
    this.startTimeUtc,
    this.endTimeUtc,
    this.durationMinutes,
  });

  final int? id;
  final int projectId;
  final DateTime entryDateUtc;
  final DateTime? startTimeUtc;
  final DateTime? endTimeUtc;
  final int? durationMinutes;
  final String description;
  final bool isBillable;
}

class _ProjectOption {
  const _ProjectOption({
    required this.id,
    required this.name,
  });

  final int id;
  final String name;
}

const Map<int, String> _temporaryProjectNames = <int, String>{
  1: 'Flux Internal',
  2: 'Clockify Clone MVP',
};

Map<String, dynamic> _asLocalJsonMap(dynamic value) {
  if (value is Map<String, dynamic>) {
    return value;
  }
  if (value is Map) {
    return value.map(
      (key, dynamic item) => MapEntry(key.toString(), item),
    );
  }
  throw StateError('Unexpected API payload.');
}

DateTime _editorDateForEntry(TimeEntryRecord? entry) {
  final source = entry?.startTimeUtc?.toLocal() ??
      entry?.entryDate.toLocal() ??
      DateTime.now();
  return DateTime(source.year, source.month, source.day);
}

DateTime _combineLocalDateAndTime(DateTime date, TimeOfDay time) {
  return DateTime(date.year, date.month, date.day, time.hour, time.minute);
}

DateTime _utcDateOnly(DateTime date) {
  return DateTime.utc(date.year, date.month, date.day);
}

String _formatPickerDate(DateTime date) {
  final normalized = DateTime(date.year, date.month, date.day);
  return _formatDateLabel(normalized);
}

String _titleCaseRole(String rawRole) {
  final normalized = rawRole.trim();
  if (normalized.isEmpty) {
    return rawRole;
  }

  return normalized
      .split(RegExp(r'[\s_-]+'))
      .where((part) => part.isNotEmpty)
      .map(
        (part) => part[0].toUpperCase() + part.substring(1).toLowerCase(),
      )
      .join(' ');
}

String _initialsFor(String value) {
  final parts = value
      .split(RegExp(r'\s+'))
      .where((part) => part.trim().isNotEmpty)
      .toList(growable: false);

  if (parts.isEmpty) {
    return 'FL';
  }
  if (parts.length == 1) {
    final text = parts.first;
    return text.substring(0, text.length >= 2 ? 2 : 1).toUpperCase();
  }

  return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
}

String _fallbackProjectName(int projectId) {
  return 'Workspace Project $projectId';
}

DateTime _entryAnchor(TimeEntryRecord entry) {
  return entry.endTimeUtc ?? entry.startTimeUtc ?? entry.entryDate;
}

DateTime _entryBusinessDate(TimeEntryRecord entry) {
  final localDate = entry.entryDate.toLocal();
  return DateTime(localDate.year, localDate.month, localDate.day);
}

bool _isSameDay(DateTime left, DateTime right) {
  return left.year == right.year &&
      left.month == right.month &&
      left.day == right.day;
}

bool _rangesOverlap(
  DateTime leftStart,
  DateTime leftEnd,
  DateTime rightStart,
  DateTime rightEnd,
) {
  return leftStart.isBefore(rightEnd) && rightStart.isBefore(leftEnd);
}

String _formatDuration(Duration duration) {
  final totalSeconds = duration.inSeconds;
  final hours = (totalSeconds ~/ 3600).toString().padLeft(2, '0');
  final minutes = ((totalSeconds % 3600) ~/ 60).toString().padLeft(2, '0');
  final seconds = (totalSeconds % 60).toString().padLeft(2, '0');
  return '$hours:$minutes:$seconds';
}

String _formatDateLabel(DateTime date) {
  const months = <String>[
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];

  return '${months[date.month - 1]} ${date.day}, ${date.year}';
}

String _formatClockTime(DateTime value) {
  final local = value.toLocal();
  final hour = local.hour % 12 == 0 ? 12 : local.hour % 12;
  final minute = local.minute.toString().padLeft(2, '0');
  final period = local.hour >= 12 ? 'PM' : 'AM';
  return '$hour:$minute $period';
}

String _formatEntryTime(TimeEntryRecord entry) {
  if (entry.startTimeUtc != null && entry.endTimeUtc != null) {
    return '${_formatClockTime(entry.startTimeUtc!)} - ${_formatClockTime(entry.endTimeUtc!)}';
  }

  return 'Manual entry';
}
