import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/api_error_message.dart';
import '../../../auth/data/auth_models.dart';
import '../../../auth/data/auth_session_controller.dart';
import '../../../time_tracker/data/time_tracker_api_client.dart';
import '../../../time_tracker/data/time_tracker_models.dart';

class CalendarPage extends ConsumerStatefulWidget {
  const CalendarPage({super.key});

  @override
  ConsumerState<CalendarPage> createState() => _CalendarPageState();
}

class _CalendarPageState extends ConsumerState<CalendarPage> {
  bool _loading = true;
  String? _screenError;
  List<TimeEntryRecord> _entries = const <TimeEntryRecord>[];
  Map<int, String> _projectNames = const <int, String>{};
  AuthProfile? _liveProfile;
  DateTime _focusedMonth = DateTime.now();

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData({bool showLoading = true}) async {
    if (showLoading && mounted) {
      setState(() {
        _loading = true;
        _screenError = null;
      });
    }

    try {
      final api = ref.read(timeTrackerApiClientProvider);
      final session = ref.read(authSessionControllerProvider).session;
      final storage = ref.read(authStorageProvider);

      final firstOfMonth =
          DateTime(_focusedMonth.year, _focusedMonth.month, 1);
      final lastOfMonth = DateTime(
        _focusedMonth.year,
        _focusedMonth.month + 1,
        0,
        23,
        59,
        59,
      );

      final entriesPage = await api.getTimeEntries(
        from: firstOfMonth,
        to: lastOfMonth,
        pageSize: 500,
      );

      final knownProjectNames = await storage.readKnownProjectNames();

      if (!mounted) {
        return;
      }

      setState(() {
        _entries = entriesPage.items;
        _projectNames = knownProjectNames;
        _liveProfile = session?.profile;
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
          fallback: 'Calendar data could not be loaded.',
        );
      });
    }
  }

  Future<void> _handleProtectedError(Object error) async {
    if (error is DioException && error.response?.statusCode == 401) {
      await ref.read(authSessionControllerProvider.notifier).signOut();
    }
  }

  Future<void> _logout() async {
    await ref.read(authSessionControllerProvider.notifier).signOut();
  }

  void _goToPreviousMonth() {
    setState(() {
      _focusedMonth = DateTime(_focusedMonth.year, _focusedMonth.month - 1);
    });
    _loadData();
  }

  void _goToNextMonth() {
    setState(() {
      _focusedMonth = DateTime(_focusedMonth.year, _focusedMonth.month + 1);
    });
    _loadData();
  }

  Map<DateTime, List<TimeEntryRecord>> get _entriesByDay {
    final map = <DateTime, List<TimeEntryRecord>>{};
    for (final entry in _entries) {
      final local = entry.entryDate.toLocal();
      final key = DateTime(local.year, local.month, local.day);
      map.putIfAbsent(key, () => <TimeEntryRecord>[]).add(entry);
    }
    return map;
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
          title: const Text('Flux'),
        ),
        drawer: Drawer(
          child: SafeArea(
            child: _CalendarSidebar(
              profile: profile,
              onTimeTracker: () => Navigator.of(context).pop(),
              onLogout: _logout,
            ),
          ),
        ),
        body: _buildBody(isCompact),
      );
    }

    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FB),
      body: SafeArea(
        child: Row(
          children: [
            SizedBox(
              width: 286,
              child: _CalendarSidebar(
                profile: profile,
                onTimeTracker: () => Navigator.of(context).pop(),
                onLogout: _logout,
              ),
            ),
            Expanded(child: _buildBody(isCompact)),
          ],
        ),
      ),
    );
  }

  Widget _buildBody(bool isCompact) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    return RefreshIndicator(
      onRefresh: () => _loadData(showLoading: false),
      child: ListView(
        padding: EdgeInsets.fromLTRB(
          isCompact ? 16 : 30,
          20,
          isCompact ? 16 : 30,
          24,
        ),
        children: [
          _CalendarHeader(
            focusedMonth: _focusedMonth,
            onPreviousMonth: _goToPreviousMonth,
            onNextMonth: _goToNextMonth,
          ),
          const SizedBox(height: 18),
          if (_screenError != null) ...[
            _InlineErrorBanner(
              message: _screenError!,
              onRetry: () => _loadData(showLoading: false),
            ),
            const SizedBox(height: 16),
          ],
          _CalendarGrid(
            focusedMonth: _focusedMonth,
            entriesByDay: _entriesByDay,
            projectNames: _projectNames,
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Sidebar
// ---------------------------------------------------------------------------

class _CalendarSidebar extends StatelessWidget {
  const _CalendarSidebar({
    required this.profile,
    required this.onTimeTracker,
    required this.onLogout,
  });

  final AuthProfile profile;
  final VoidCallback onTimeTracker;
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
                _SidebarNavItem(
                  icon: Icons.access_time_rounded,
                  label: 'Time Tracker',
                  onTap: onTimeTracker,
                ),
                const SizedBox(height: 8),
                const _SidebarNavItem(
                  icon: Icons.bar_chart_rounded,
                  label: 'Report',
                ),
                const SizedBox(height: 8),
                const _SidebarNavItem(
                  icon: Icons.receipt_long_rounded,
                  label: 'Expenses',
                ),
                const SizedBox(height: 8),
                const _SidebarNavItem(
                  icon: Icons.calendar_month_rounded,
                  label: 'Calendar',
                  selected: true,
                ),
                const SizedBox(height: 8),
                const _SidebarNavItem(
                  icon: Icons.folder_copy_rounded,
                  label: 'Projects',
                ),
                const SizedBox(height: 8),
                const _SidebarNavItem(
                  icon: Icons.groups_rounded,
                  label: 'Members',
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
                IconButton(
                  tooltip: 'Logout',
                  onPressed: onLogout,
                  icon: const Icon(Icons.logout_rounded),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SidebarNavItem extends StatelessWidget {
  const _SidebarNavItem({
    required this.icon,
    required this.label,
    this.selected = false,
    this.onTap,
  });

  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback? onTap;

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
              color: selected
                  ? const Color(0xFF1E7BF2)
                  : const Color(0xFF728099),
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

// ---------------------------------------------------------------------------
// Calendar Header
// ---------------------------------------------------------------------------

class _CalendarHeader extends StatelessWidget {
  const _CalendarHeader({
    required this.focusedMonth,
    required this.onPreviousMonth,
    required this.onNextMonth,
  });

  final DateTime focusedMonth;
  final VoidCallback onPreviousMonth;
  final VoidCallback onNextMonth;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            _monthLabel(focusedMonth),
            style: const TextStyle(
              color: Color(0xFF132039),
              fontSize: 22,
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
        IconButton(
          onPressed: onPreviousMonth,
          icon: const Icon(Icons.chevron_left_rounded),
          tooltip: 'Previous month',
          style: IconButton.styleFrom(
            backgroundColor: Colors.white,
            foregroundColor: const Color(0xFF53627C),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
              side: const BorderSide(color: Color(0xFFE4EAF4)),
            ),
          ),
        ),
        const SizedBox(width: 8),
        IconButton(
          onPressed: onNextMonth,
          icon: const Icon(Icons.chevron_right_rounded),
          tooltip: 'Next month',
          style: IconButton.styleFrom(
            backgroundColor: Colors.white,
            foregroundColor: const Color(0xFF53627C),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
              side: const BorderSide(color: Color(0xFFE4EAF4)),
            ),
          ),
        ),
      ],
    );
  }
}

// ---------------------------------------------------------------------------
// Calendar Grid
// ---------------------------------------------------------------------------

class _CalendarGrid extends StatelessWidget {
  const _CalendarGrid({
    required this.focusedMonth,
    required this.entriesByDay,
    required this.projectNames,
  });

  final DateTime focusedMonth;
  final Map<DateTime, List<TimeEntryRecord>> entriesByDay;
  final Map<int, String> projectNames;

  @override
  Widget build(BuildContext context) {
    const weekDays = <String>['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    final firstDay =
        DateTime(focusedMonth.year, focusedMonth.month, 1);
    final daysInMonth =
        DateTime(focusedMonth.year, focusedMonth.month + 1, 0).day;
    // weekday: Mon=1 ... Sun=7 → offset to start on Monday
    final startOffset = (firstDay.weekday - 1) % 7;
    final totalCells = startOffset + daysInMonth;
    final rows = (totalCells / 7).ceil();

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: const Color(0xFFE4EAF4)),
      ),
      child: Column(
        children: [
          // Day-of-week headers
          Padding(
            padding: const EdgeInsets.fromLTRB(4, 16, 4, 8),
            child: Row(
              children: weekDays
                  .map(
                    (day) => Expanded(
                      child: Center(
                        child: Text(
                          day,
                          style: const TextStyle(
                            color: Color(0xFF61708C),
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                    ),
                  )
                  .toList(growable: false),
            ),
          ),
          const Divider(height: 1),
          // Calendar day cells
          for (var row = 0; row < rows; row++) ...[
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: List.generate(7, (col) {
                  final cellIndex = row * 7 + col;
                  final dayNumber = cellIndex - startOffset + 1;

                  if (dayNumber < 1 || dayNumber > daysInMonth) {
                    return const Expanded(child: SizedBox());
                  }

                  final date = DateTime(
                    focusedMonth.year,
                    focusedMonth.month,
                    dayNumber,
                  );
                  final dayEntries = entriesByDay[date] ?? const [];
                  final totalMinutes = dayEntries.fold<int>(
                    0,
                    (sum, e) => sum + e.durationMinutes,
                  );

                  final today = DateTime.now();
                  final isToday = date.year == today.year &&
                      date.month == today.month &&
                      date.day == today.day;

                  return Expanded(
                    child: _DayCell(
                      dayNumber: dayNumber,
                      totalMinutes: totalMinutes,
                      entries: dayEntries,
                      projectNames: projectNames,
                      isToday: isToday,
                    ),
                  );
                }),
              ),
            ),
            if (row < rows - 1)
              const Divider(height: 1),
          ],
          const SizedBox(height: 4),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Day Cell
// ---------------------------------------------------------------------------

class _DayCell extends StatelessWidget {
  const _DayCell({
    required this.dayNumber,
    required this.totalMinutes,
    required this.entries,
    required this.projectNames,
    required this.isToday,
  });

  final int dayNumber;
  final int totalMinutes;
  final List<TimeEntryRecord> entries;
  final Map<int, String> projectNames;
  final bool isToday;

  @override
  Widget build(BuildContext context) {
    final hasEntries = entries.isNotEmpty;

    return Container(
      constraints: const BoxConstraints(minHeight: 72),
      padding: const EdgeInsets.all(8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Day number badge
          Container(
            width: 28,
            height: 28,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: isToday ? const Color(0xFF1E7BF2) : Colors.transparent,
              shape: BoxShape.circle,
            ),
            child: Text(
              dayNumber.toString(),
              style: TextStyle(
                color: isToday
                    ? Colors.white
                    : const Color(0xFF132039),
                fontSize: 13,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          if (hasEntries) ...[
            const SizedBox(height: 4),
            Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 6, vertical: 3),
              decoration: BoxDecoration(
                color: const Color(0xFFEAF4FF),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                _formatDuration(Duration(minutes: totalMinutes)),
                style: const TextStyle(
                  color: Color(0xFF1E7BF2),
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  fontFamily: 'monospace',
                ),
              ),
            ),
            const SizedBox(height: 4),
            ...entries.take(2).map(
                  (entry) => Padding(
                    padding: const EdgeInsets.only(bottom: 2),
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 5,
                        vertical: 2,
                      ),
                      decoration: BoxDecoration(
                        color: entry.isBillable
                            ? const Color(0xFFEFFAF5)
                            : const Color(0xFFF8FAFC),
                        borderRadius: BorderRadius.circular(6),
                        border: Border.all(
                          color: entry.isBillable
                              ? const Color(0xFFCDEEDD)
                              : const Color(0xFFE2E8F0),
                        ),
                      ),
                      child: Text(
                        _projectName(entry.projectId),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: entry.isBillable
                              ? const Color(0xFF0F8B61)
                              : const Color(0xFF53627C),
                          fontSize: 10,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                ),
            if (entries.length > 2)
              Text(
                '+${entries.length - 2} more',
                style: const TextStyle(
                  color: Color(0xFF1E7BF2),
                  fontSize: 10,
                  fontWeight: FontWeight.w600,
                ),
              ),
          ],
        ],
      ),
    );
  }

  String _projectName(int projectId) {
    final name = projectNames[projectId]?.trim();
    if (name != null && name.isNotEmpty) {
      return name;
    }
    return 'Project $projectId';
  }
}

// ---------------------------------------------------------------------------
// Inline error banner
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

String _monthLabel(DateTime date) {
  const months = <String>[
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];
  return '${months[date.month - 1]} ${date.year}';
}

String _formatDuration(Duration duration) {
  final totalSeconds = duration.inSeconds;
  final hours = (totalSeconds ~/ 3600).toString().padLeft(2, '0');
  final minutes = ((totalSeconds % 3600) ~/ 60).toString().padLeft(2, '0');
  return '$hours:$minutes';
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
