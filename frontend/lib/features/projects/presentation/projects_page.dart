import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api_error_message.dart';
import '../../auth/data/auth_models.dart';
import '../data/projects_api_client.dart';
import '../data/projects_models.dart';

class ProjectsWorkspacePage extends ConsumerStatefulWidget {
  const ProjectsWorkspacePage({
    required this.session,
    this.embeddedInShell = false,
    super.key,
  });

  final AuthSession session;
  final bool embeddedInShell;

  @override
  ConsumerState<ProjectsWorkspacePage> createState() =>
      _ProjectsWorkspacePageState();
}

class _ProjectsWorkspacePageState extends ConsumerState<ProjectsWorkspacePage> {
  final TextEditingController _searchController = TextEditingController();

  bool _loading = true;
  String? _screenError;
  ProjectsPage? _projectsPage;
  String _statusFilter = 'All';
  int? _selectedProjectId;
  Map<int, MyProjectAssignmentRecord> _myAssignments =
      const <int, MyProjectAssignmentRecord>{};

  bool get _isManager => _role == 'Manager';
  bool get _isEmployee => _role == 'Employee';
  bool get _supportsProjects => _isManager || _isEmployee;
  String get _currentUserId => widget.session.profile.id;
  String get _currentUserDisplayName => widget.session.profile.displayName;

  String get _role {
    final profileRole = widget.session.profile.role?.trim();
    if (profileRole != null && profileRole.isNotEmpty) {
      return profileRole;
    }
    if (widget.session.roles.isNotEmpty) {
      return widget.session.roles.first;
    }
    return 'Employee';
  }

  @override
  void initState() {
    super.initState();
    _loadProjects();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadProjects({
    int? preferredProjectId,
    bool showLoading = true,
  }) async {
    if (!_supportsProjects) {
      if (mounted) {
        setState(() {
          _loading = false;
          _screenError = null;
          _projectsPage = const ProjectsPage(
            items: <ProjectRecord>[],
            page: 1,
            pageSize: 20,
            totalCount: 0,
            totalPages: 0,
            hasNext: false,
            hasPrevious: false,
          );
        });
      }
      return;
    }

    if (showLoading && mounted) {
      setState(() {
        _loading = true;
        _screenError = null;
      });
    }

    try {
      final api = ref.read(projectsApiClientProvider);
      final projectsFuture = api.getProjects(
        page: 1,
        pageSize: 50,
        status: _statusFilter == 'All' ? null : _statusFilter,
        query: _searchController.text,
      );
      final assignmentsFuture = _isEmployee
          ? api.getMyAssignments()
          : Future<List<MyProjectAssignmentRecord>>.value(
              const <MyProjectAssignmentRecord>[],
            );

      final results = await Future.wait<dynamic>([
        projectsFuture,
        assignmentsFuture,
      ]);

      final projectsPage = results[0] as ProjectsPage;
      final myAssignments = results[1] as List<MyProjectAssignmentRecord>;
      final nextSelectedId = _resolveSelectedProjectId(
        items: projectsPage.items,
        currentValue: preferredProjectId ?? _selectedProjectId,
      );

      if (!mounted) {
        return;
      }

      setState(() {
        _projectsPage = projectsPage;
        _myAssignments = {
          for (final item in myAssignments) item.projectId: item,
        };
        _selectedProjectId = nextSelectedId;
        _loading = false;
        _screenError = null;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }

      setState(() {
        _loading = false;
        _screenError = describeApiError(
          error,
          fallback: 'Projects could not be loaded.',
        );
      });
    }
  }

  int? _resolveSelectedProjectId({
    required List<ProjectRecord> items,
    required int? currentValue,
  }) {
    if (items.isEmpty) {
      return null;
    }
    if (currentValue != null && items.any((item) => item.id == currentValue)) {
      return currentValue;
    }
    return items.first.id;
  }

  Future<void> _openProjectCreator() async {
    final created = await showDialog<ProjectRecord>(
      context: context,
      builder: (context) => _ProjectEditorDialog(
        title: 'Create project',
        submitLabel: 'Create project',
        onSubmit: (draft) {
          return ref.read(projectsApiClientProvider).createProject(
                name: draft.name,
                code: draft.code,
                description: draft.description,
                startDate: draft.startDate,
                endDate: draft.endDate,
              );
        },
      ),
    );

    if (created == null) {
      return;
    }

    await _loadProjects(preferredProjectId: created.id, showLoading: false);
    if (!mounted) {
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Project created.')),
    );
  }

  Future<void> _openMobileDetail(ProjectRecord project) async {
    final changed = await Navigator.of(context).push<bool>(
          MaterialPageRoute(
            builder: (context) => _ProjectDetailRoute(
              projectId: project.id,
              canManage: _isManager,
              currentUserId: _currentUserId,
              currentUserDisplayName: _currentUserDisplayName,
            ),
          ),
        ) ??
        false;

    if (changed) {
      await _loadProjects(preferredProjectId: project.id, showLoading: false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final projects = _projectsPage?.items ?? const <ProjectRecord>[];

    return LayoutBuilder(
      builder: (context, constraints) {
        final isDesktop = constraints.maxWidth >= 1100;

        final unsupportedContent = Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: const Card(
              child: Padding(
                padding: EdgeInsets.all(28),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.folder_off_outlined, size: 54),
                    SizedBox(height: 14),
                    Text(
                      'Projects module is available for manager and employee roles in this release.',
                      textAlign: TextAlign.center,
                      style: TextStyle(fontSize: 16),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );

        if (!_supportsProjects) {
          if (widget.embeddedInShell) {
            return unsupportedContent;
          }

          return Scaffold(
            backgroundColor: const Color(0xFFF4F7FB),
            body: SafeArea(child: unsupportedContent),
          );
        }

        final workspaceContent = Padding(
          padding: EdgeInsets.fromLTRB(
            isDesktop ? 28 : 18,
            18,
            isDesktop ? 28 : 18,
            18,
          ),
          child: Column(
            children: [
              _ProjectsHeader(
                isManager: _isManager,
                isEmployee: _isEmployee,
                searchController: _searchController,
                statusFilter: _statusFilter,
                onStatusChanged: (value) {
                  setState(() {
                    _statusFilter = value;
                  });
                  _loadProjects();
                },
                onSearchSubmitted: (_) => _loadProjects(),
                onRefresh: () => _loadProjects(),
                onCreate: _isManager ? _openProjectCreator : null,
              ),
              const SizedBox(height: 18),
              Expanded(
                child: isDesktop
                    ? Row(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          SizedBox(
                            width: 380,
                            child: _ProjectsListPanel(
                              loading: _loading,
                              error: _screenError,
                              projects: projects,
                              selectedProjectId: _selectedProjectId,
                              myAssignments: _myAssignments,
                              onRetry: _loadProjects,
                              onSelect: (project) {
                                setState(() {
                                  _selectedProjectId = project.id;
                                });
                              },
                            ),
                          ),
                          const SizedBox(width: 18),
                          Expanded(
                            child: _selectedProjectId == null
                                ? const _EmptySelectionCard()
                                : _ProjectDetailPanel(
                                    projectId: _selectedProjectId!,
                                    canManage: _isManager,
                                    currentUserId: _currentUserId,
                                    currentUserDisplayName:
                                        _currentUserDisplayName,
                                    embedded: true,
                                    onRefreshRequested: () {
                                      _loadProjects(
                                        preferredProjectId: _selectedProjectId,
                                        showLoading: false,
                                      );
                                    },
                                  ),
                          ),
                        ],
                      )
                    : _ProjectsListPanel(
                        loading: _loading,
                        error: _screenError,
                        projects: projects,
                        selectedProjectId: _selectedProjectId,
                        myAssignments: _myAssignments,
                        onRetry: _loadProjects,
                        onSelect: _openMobileDetail,
                      ),
              ),
            ],
          ),
        );

        if (widget.embeddedInShell) {
          return Stack(
            children: [
              workspaceContent,
              if (!isDesktop && _isManager)
                Positioned(
                  right: 18,
                  bottom: 18,
                  child: FloatingActionButton.extended(
                    onPressed: _openProjectCreator,
                    icon: const Icon(Icons.add),
                    label: const Text('New project'),
                  ),
                ),
            ],
          );
        }

        return Scaffold(
          backgroundColor: const Color(0xFFF4F7FB),
          floatingActionButton: !isDesktop && _isManager
              ? FloatingActionButton.extended(
                  onPressed: _openProjectCreator,
                  icon: const Icon(Icons.add),
                  label: const Text('New project'),
                )
              : null,
          body: SafeArea(child: workspaceContent),
        );
      },
    );
  }
}

class _ProjectsHeader extends StatelessWidget {
  const _ProjectsHeader({
    required this.isManager,
    required this.isEmployee,
    required this.searchController,
    required this.statusFilter,
    required this.onStatusChanged,
    required this.onSearchSubmitted,
    required this.onRefresh,
    required this.onCreate,
  });

  final bool isManager;
  final bool isEmployee;
  final TextEditingController searchController;
  final String statusFilter;
  final ValueChanged<String> onStatusChanged;
  final ValueChanged<String> onSearchSubmitted;
  final VoidCallback onRefresh;
  final VoidCallback? onCreate;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Projects',
                        style: theme.textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        isManager
                            ? 'Shape delivery, manage lifecycle, and control staffing from one workspace.'
                            : 'Browse the projects you are assigned to and track the context behind your work.',
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: const Color(0xFF5E728A),
                          height: 1.45,
                        ),
                      ),
                    ],
                  ),
                ),
                if (onCreate != null)
                  FilledButton.icon(
                    onPressed: onCreate,
                    icon: const Icon(Icons.add),
                    label: const Text('New project'),
                  ),
              ],
            ),
            const SizedBox(height: 18),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                SizedBox(
                  width: 320,
                  child: TextField(
                    controller: searchController,
                    textInputAction: TextInputAction.search,
                    onSubmitted: onSearchSubmitted,
                    decoration: const InputDecoration(
                      hintText: 'Search by project name or code',
                      prefixIcon: Icon(Icons.search),
                    ),
                  ),
                ),
                SizedBox(
                  width: 180,
                  child: DropdownButtonFormField<String>(
                    initialValue: statusFilter,
                    onChanged: (value) {
                      if (value != null) {
                        onStatusChanged(value);
                      }
                    },
                    decoration: const InputDecoration(labelText: 'Status'),
                    items: const [
                      DropdownMenuItem(value: 'All', child: Text('All')),
                      DropdownMenuItem(value: 'Active', child: Text('Active')),
                      DropdownMenuItem(
                        value: 'Archived',
                        child: Text('Archived'),
                      ),
                      DropdownMenuItem(value: 'Closed', child: Text('Closed')),
                    ],
                  ),
                ),
                OutlinedButton.icon(
                  onPressed: onRefresh,
                  icon: const Icon(Icons.refresh),
                  label: const Text('Refresh'),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 10,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF4F7FB),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Text(
                    isEmployee
                        ? 'View mode: Assigned projects'
                        : 'View mode: Managed projects',
                    style: const TextStyle(
                      color: Color(0xFF5E728A),
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ProjectsListPanel extends StatelessWidget {
  const _ProjectsListPanel({
    required this.loading,
    required this.error,
    required this.projects,
    required this.selectedProjectId,
    required this.myAssignments,
    required this.onRetry,
    required this.onSelect,
  });

  final bool loading;
  final String? error;
  final List<ProjectRecord> projects;
  final int? selectedProjectId;
  final Map<int, MyProjectAssignmentRecord> myAssignments;
  final Future<void> Function() onRetry;
  final ValueChanged<ProjectRecord> onSelect;

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Card(
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (error != null) {
      return Card(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(28),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.cloud_off_rounded, size: 50),
                const SizedBox(height: 12),
                Text(
                  error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(height: 1.45),
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: onRetry,
                  child: const Text('Try again'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    if (projects.isEmpty) {
      return const Card(
        child: Center(
          child: Padding(
            padding: EdgeInsets.all(28),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.folder_open_outlined, size: 54),
                SizedBox(height: 14),
                Text(
                  'No projects matched the current filters.',
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ),
      );
    }

    return Card(
      child: ListView.separated(
        padding: const EdgeInsets.all(14),
        itemCount: projects.length,
        separatorBuilder: (_, __) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final project = projects[index];
          final selected = project.id == selectedProjectId;
          final myAssignment = myAssignments[project.id];

          return InkWell(
            borderRadius: BorderRadius.circular(22),
            onTap: () => onSelect(project),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 180),
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(22),
                color: selected
                    ? const Color(0xFFE8F0FF)
                    : const Color(0xFFF8FAFD),
                border: Border.all(
                  color: selected
                      ? const Color(0xFF0D5EF8)
                      : const Color(0xFFDCE5F1),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          project.name,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      _StatusChip(status: project.status),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      if (project.code != null &&
                          project.code!.trim().isNotEmpty)
                        _MiniInfoPill(label: project.code!.trim()),
                      _MiniInfoPill(label: _projectRangeLabel(project)),
                    ],
                  ),
                  if (myAssignment != null) ...[
                    const SizedBox(height: 10),
                    Text(
                      'Assigned on ${_formatDateTime(myAssignment.assignedAtUtc, dateOnly: true)}',
                      style: const TextStyle(
                        color: Color(0xFF5E728A),
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _ProjectDetailRoute extends StatelessWidget {
  const _ProjectDetailRoute({
    required this.projectId,
    required this.canManage,
    required this.currentUserId,
    required this.currentUserDisplayName,
  });

  final int projectId;
  final bool canManage;
  final String currentUserId;
  final String currentUserDisplayName;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF4F7FB),
      appBar: AppBar(
        title: const Text('Project detail'),
        backgroundColor: const Color(0xFFF4F7FB),
      ),
      body: Padding(
        padding: const EdgeInsets.fromLTRB(18, 0, 18, 18),
        child: _ProjectDetailPanel(
          projectId: projectId,
          canManage: canManage,
          currentUserId: currentUserId,
          currentUserDisplayName: currentUserDisplayName,
          embedded: false,
          onRefreshRequested: () {
            Navigator.of(context).pop(true);
          },
        ),
      ),
    );
  }
}

class _ProjectDetailPanel extends ConsumerStatefulWidget {
  const _ProjectDetailPanel({
    required this.projectId,
    required this.canManage,
    required this.currentUserId,
    required this.currentUserDisplayName,
    required this.embedded,
    required this.onRefreshRequested,
  });

  final int projectId;
  final bool canManage;
  final String currentUserId;
  final String currentUserDisplayName;
  final bool embedded;
  final VoidCallback onRefreshRequested;

  @override
  ConsumerState<_ProjectDetailPanel> createState() =>
      _ProjectDetailPanelState();
}

class _ProjectDetailPanelState extends ConsumerState<_ProjectDetailPanel> {
  bool _loading = true;
  bool _assignmentBusy = false;
  String? _error;
  ProjectRecord? _project;
  List<ProjectAssignmentRecord> _assignments =
      const <ProjectAssignmentRecord>[];
  Map<String, UserOption> _assignmentUsers = const <String, UserOption>{};

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant _ProjectDetailPanel oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.projectId != widget.projectId ||
        oldWidget.canManage != widget.canManage) {
      _load();
    }
  }

  Future<void> _load({bool showLoading = true}) async {
    if (showLoading && mounted) {
      setState(() {
        _loading = true;
        _error = null;
      });
    }

    try {
      final api = ref.read(projectsApiClientProvider);
      final project = await api.getProjectById(widget.projectId);
      List<ProjectAssignmentRecord> assignments =
          const <ProjectAssignmentRecord>[];
      Map<String, UserOption> assignmentUsers = const <String, UserOption>{};

      if (widget.canManage) {
        assignments = await api.getProjectAssignments(widget.projectId);
        final assignedUsers = await api.getEmployees(
          projectId: widget.projectId,
          page: 1,
          pageSize: 100,
        );
        assignmentUsers = {
          for (final user in assignedUsers.items) user.id: user,
        };
      }

      if (!mounted) {
        return;
      }

      setState(() {
        _project = project;
        _assignments = assignments;
        _assignmentUsers = assignmentUsers;
        _loading = false;
        _error = null;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _loading = false;
        _error = describeApiError(
          error,
          fallback: 'Project detail could not be loaded.',
        );
      });
    }
  }

  String _managerDisplayValue(ProjectRecord project) {
    final currentUserId = widget.currentUserId.trim();
    final currentUserDisplayName = widget.currentUserDisplayName.trim();
    if (currentUserId.isNotEmpty &&
        currentUserDisplayName.isNotEmpty &&
        project.managerUserId == currentUserId) {
      return currentUserDisplayName;
    }

    return project.managerUserId;
  }

  Future<void> _editProject() async {
    final project = _project;
    if (project == null) {
      return;
    }

    final updated = await showDialog<ProjectRecord>(
      context: context,
      builder: (context) => _ProjectEditorDialog(
        title: 'Edit project',
        submitLabel: 'Save changes',
        initialProject: project,
        onSubmit: (draft) {
          return ref.read(projectsApiClientProvider).updateProject(
                id: project.id,
                name: draft.name,
                code: draft.code,
                description: draft.description,
                startDate: draft.startDate,
                endDate: draft.endDate,
              );
        },
      ),
    );

    if (updated == null) {
      return;
    }

    await _load(showLoading: false);
    widget.onRefreshRequested();
    if (!mounted) {
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Project updated.')),
    );
  }

  Future<void> _updateStatus(String status) async {
    setState(() {
      _assignmentBusy = true;
    });

    try {
      await ref.read(projectsApiClientProvider).updateProjectStatus(
            id: widget.projectId,
            status: status,
          );
      await _load(showLoading: false);
      widget.onRefreshRequested();
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Project marked as $status.')),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            describeApiError(
              error,
              fallback: 'Project status could not be updated.',
            ),
          ),
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _assignmentBusy = false;
        });
      }
    }
  }

  Future<void> _addAssignment() async {
    final user = await showDialog<UserOption>(
      context: context,
      builder: (context) => _AssignmentPickerDialog(
        projectId: widget.projectId,
        assignedUserIds: _assignments.map((item) => item.userId).toSet(),
      ),
    );

    if (user == null) {
      return;
    }

    setState(() {
      _assignmentBusy = true;
    });

    try {
      await ref.read(projectsApiClientProvider).addProjectAssignment(
            projectId: widget.projectId,
            userId: user.id,
          );
      await _load(showLoading: false);
      widget.onRefreshRequested();
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${user.displayName} assigned to project.')),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            describeApiError(
              error,
              fallback: 'Employee could not be assigned.',
            ),
          ),
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _assignmentBusy = false;
        });
      }
    }
  }

  Future<void> _removeAssignment(ProjectAssignmentRecord assignment) async {
    final user = _assignmentUsers[assignment.userId];
    final confirmed = await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(24),
            ),
            title: const Text('Remove assignment'),
            content: Text(
              'Remove ${user?.displayName ?? assignment.userId} from this project?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Cancel'),
              ),
              FilledButton(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Remove'),
              ),
            ],
          ),
        ) ??
        false;

    if (!confirmed) {
      return;
    }

    setState(() {
      _assignmentBusy = true;
    });

    try {
      await ref.read(projectsApiClientProvider).removeProjectAssignment(
            projectId: widget.projectId,
            userId: assignment.userId,
          );
      await _load(showLoading: false);
      widget.onRefreshRequested();
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Assignment removed.')),
      );
    } catch (error) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            describeApiError(
              error,
              fallback: 'Assignment could not be removed.',
            ),
          ),
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _assignmentBusy = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Card(
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (_error != null) {
      return Card(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(28),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline, size: 48),
                const SizedBox(height: 12),
                Text(
                  _error!,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: _load,
                  child: const Text('Reload detail'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    final project = _project;
    if (project == null) {
      return const _EmptySelectionCard();
    }

    return Card(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Wrap(
                        spacing: 10,
                        runSpacing: 10,
                        children: [
                          _StatusChip(status: project.status),
                          if (project.code != null &&
                              project.code!.trim().isNotEmpty)
                            _MiniInfoPill(label: project.code!.trim()),
                          _MiniInfoPill(label: _projectRangeLabel(project)),
                        ],
                      ),
                      const SizedBox(height: 16),
                      Text(
                        project.name,
                        style:
                            Theme.of(context).textTheme.headlineSmall?.copyWith(
                                  fontWeight: FontWeight.w800,
                                ),
                      ),
                      const SizedBox(height: 10),
                      Text(
                        project.description?.trim().isNotEmpty == true
                            ? project.description!.trim()
                            : 'No project description has been added yet.',
                        style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                              color: const Color(0xFF5E728A),
                              height: 1.55,
                            ),
                      ),
                    ],
                  ),
                ),
                if (widget.canManage)
                  Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children: [
                      OutlinedButton.icon(
                        onPressed: _assignmentBusy ? null : _editProject,
                        icon: const Icon(Icons.edit_outlined),
                        label: const Text('Edit'),
                      ),
                      PopupMenuButton<String>(
                        enabled: !_assignmentBusy,
                        onSelected: _updateStatus,
                        itemBuilder: (context) => const [
                          PopupMenuItem(value: 'Active', child: Text('Active')),
                          PopupMenuItem(
                            value: 'Archived',
                            child: Text('Archived'),
                          ),
                          PopupMenuItem(value: 'Closed', child: Text('Closed')),
                        ],
                        child: OutlinedButton.icon(
                          onPressed: null,
                          icon: const Icon(Icons.flag_outlined),
                          label: const Text('Status'),
                        ),
                      ),
                    ],
                  ),
              ],
            ),
            const SizedBox(height: 20),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                _DetailStatCard(
                  label: 'Project ID',
                  value: project.id.toString(),
                  icon: Icons.tag,
                ),
                _DetailStatCard(
                  label: 'Project Manager',
                  value: _managerDisplayValue(project),
                  icon: Icons.manage_accounts_outlined,
                ),
                _DetailStatCard(
                  label: 'Schedule',
                  value: _projectRangeLabel(project),
                  icon: Icons.date_range_outlined,
                ),
              ],
            ),
            if (widget.canManage) ...[
              const SizedBox(height: 24),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      'Assignments',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                  ),
                  FilledButton.icon(
                    onPressed: _assignmentBusy ? null : _addAssignment,
                    icon: const Icon(Icons.person_add_alt_1),
                    label: const Text('Assign employee'),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              if (_assignments.isEmpty)
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(18),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF8FAFD),
                    borderRadius: BorderRadius.circular(22),
                    border: Border.all(color: const Color(0xFFDCE5F1)),
                  ),
                  child: const Text(
                    'No employees are assigned to this project yet.',
                  ),
                )
              else
                Column(
                  children: [
                    for (final assignment in _assignments) ...[
                      _AssignmentCard(
                        assignment: assignment,
                        user: _assignmentUsers[assignment.userId],
                        busy: _assignmentBusy,
                        onRemove: () => _removeAssignment(assignment),
                      ),
                      const SizedBox(height: 10),
                    ],
                  ],
                ),
            ],
          ],
        ),
      ),
    );
  }
}

class _ProjectEditorDialog extends StatefulWidget {
  const _ProjectEditorDialog({
    required this.title,
    required this.submitLabel,
    required this.onSubmit,
    this.initialProject,
  });

  final String title;
  final String submitLabel;
  final ProjectRecord? initialProject;
  final Future<ProjectRecord> Function(_ProjectDraft draft) onSubmit;

  @override
  State<_ProjectEditorDialog> createState() => _ProjectEditorDialogState();
}

class _ProjectEditorDialogState extends State<_ProjectEditorDialog> {
  late final TextEditingController _nameController;
  late final TextEditingController _codeController;
  late final TextEditingController _descriptionController;
  DateTime? _startDate;
  DateTime? _endDate;
  bool _submitting = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    final project = widget.initialProject;
    _nameController = TextEditingController(text: project?.name ?? '');
    _codeController = TextEditingController(text: project?.code ?? '');
    _descriptionController =
        TextEditingController(text: project?.description ?? '');
    _startDate = project?.startDate;
    _endDate = project?.endDate;
  }

  @override
  void dispose() {
    _nameController.dispose();
    _codeController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _pickDate({required bool isStart}) async {
    final currentValue = isStart ? _startDate : _endDate;
    final picked = await showDatePicker(
      context: context,
      initialDate: currentValue ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
    );

    if (picked == null) {
      return;
    }

    setState(() {
      if (isStart) {
        _startDate = picked;
      } else {
        _endDate = picked;
      }
    });
  }

  Future<void> _submit() async {
    final name = _nameController.text.trim();
    if (name.isEmpty) {
      setState(() {
        _error = 'Project name is required.';
      });
      return;
    }

    if (_startDate != null &&
        _endDate != null &&
        _endDate!.isBefore(_startDate!)) {
      setState(() {
        _error = 'End date cannot be earlier than start date.';
      });
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      final project = await widget.onSubmit(
        _ProjectDraft(
          name: name,
          code: _blankToNull(_codeController.text),
          description: _blankToNull(_descriptionController.text),
          startDate: _startDate,
          endDate: _endDate,
        ),
      );
      if (!mounted) {
        return;
      }
      Navigator.of(context).pop(project);
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _submitting = false;
        _error = describeApiError(
          error,
          fallback: 'Project could not be saved.',
        );
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
      title: Text(widget.title),
      content: SizedBox(
        width: 520,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              TextField(
                controller: _nameController,
                decoration: const InputDecoration(labelText: 'Project name'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _codeController,
                decoration: const InputDecoration(labelText: 'Code'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _descriptionController,
                minLines: 3,
                maxLines: 5,
                decoration: const InputDecoration(labelText: 'Description'),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed:
                          _submitting ? null : () => _pickDate(isStart: true),
                      icon: const Icon(Icons.event_outlined),
                      label: Text(
                        _startDate == null
                            ? 'Pick start date'
                            : 'Start ${_formatDateTime(_startDate!, dateOnly: true)}',
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed:
                          _submitting ? null : () => _pickDate(isStart: false),
                      icon: const Icon(Icons.event_available_outlined),
                      label: Text(
                        _endDate == null
                            ? 'Pick end date'
                            : 'End ${_formatDateTime(_endDate!, dateOnly: true)}',
                      ),
                    ),
                  ),
                ],
              ),
              if (_error != null) ...[
                const SizedBox(height: 14),
                Text(
                  _error!,
                  style: const TextStyle(
                    color: Color(0xFFB42318),
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
          onPressed: _submitting ? null : () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        FilledButton(
          onPressed: _submitting ? null : _submit,
          child: _submitting
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text(widget.submitLabel),
        ),
      ],
    );
  }
}

class _AssignmentPickerDialog extends ConsumerStatefulWidget {
  const _AssignmentPickerDialog({
    required this.projectId,
    required this.assignedUserIds,
  });

  final int projectId;
  final Set<String> assignedUserIds;

  @override
  ConsumerState<_AssignmentPickerDialog> createState() =>
      _AssignmentPickerDialogState();
}

class _AssignmentPickerDialogState
    extends ConsumerState<_AssignmentPickerDialog> {
  final TextEditingController _searchController = TextEditingController();

  bool _loading = true;
  String? _error;
  List<UserOption> _users = const <UserOption>[];
  UserOption? _selected;

  @override
  void initState() {
    super.initState();
    _loadEmployees();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadEmployees() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final page = await ref.read(projectsApiClientProvider).getEmployees(
            query: _searchController.text,
            page: 1,
            pageSize: 50,
          );

      if (!mounted) {
        return;
      }

      setState(() {
        _users = page.items;
        _selected = _users.firstWhere(
          (user) => !widget.assignedUserIds.contains(user.id),
          orElse: () => const UserOption(
            id: '',
            firstName: null,
            lastName: null,
            email: '',
            role: null,
            isActive: true,
          ),
        );
        if (_selected?.id.isEmpty ?? true) {
          _selected = null;
        }
        _loading = false;
      });
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() {
        _loading = false;
        _error = describeApiError(
          error,
          fallback: 'Employees could not be loaded.',
        );
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
      title: const Text('Assign employee'),
      content: SizedBox(
        width: 540,
        height: 420,
        child: Column(
          children: [
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _searchController,
                    textInputAction: TextInputAction.search,
                    onSubmitted: (_) => _loadEmployees(),
                    decoration: const InputDecoration(
                      hintText: 'Search employee by name or email',
                      prefixIcon: Icon(Icons.search),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                OutlinedButton(
                  onPressed: _loadEmployees,
                  child: const Text('Search'),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                      ? Center(
                          child: Text(_error!, textAlign: TextAlign.center))
                      : _users.isEmpty
                          ? const Center(
                              child: Text('No employee matched your search.'),
                            )
                          : ListView.separated(
                              itemCount: _users.length,
                              separatorBuilder: (_, __) =>
                                  const Divider(height: 1),
                              itemBuilder: (context, index) {
                                final user = _users[index];
                                final disabled =
                                    widget.assignedUserIds.contains(user.id);
                                final isSelected = _selected?.id == user.id;
                                return Container(
                                  decoration: BoxDecoration(
                                    borderRadius: BorderRadius.circular(18),
                                    border: Border.all(
                                      color: disabled
                                          ? const Color(0xFFDCE5F1)
                                          : isSelected
                                              ? const Color(0xFF0D5EF8)
                                              : Colors.transparent,
                                    ),
                                  ),
                                  child: ListTile(
                                    enabled: !disabled,
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(18),
                                    ),
                                    onTap: disabled
                                        ? null
                                        : () {
                                            setState(() {
                                              _selected = user;
                                            });
                                          },
                                    title: Text(user.displayName),
                                    subtitle: Text(
                                      disabled
                                          ? '${user.email} • already assigned'
                                          : user.email,
                                    ),
                                    trailing: Icon(
                                      isSelected
                                          ? Icons.radio_button_checked
                                          : Icons.radio_button_off,
                                      color: disabled
                                          ? const Color(0xFF98A6B8)
                                          : isSelected
                                              ? const Color(0xFF0D5EF8)
                                              : const Color(0xFF98A6B8),
                                    ),
                                  ),
                                );
                              },
                            ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        FilledButton(
          onPressed: _selected == null
              ? null
              : () => Navigator.of(context).pop(_selected),
          child: const Text('Assign'),
        ),
      ],
    );
  }
}

class _AssignmentCard extends StatelessWidget {
  const _AssignmentCard({
    required this.assignment,
    required this.user,
    required this.busy,
    required this.onRemove,
  });

  final ProjectAssignmentRecord assignment;
  final UserOption? user;
  final bool busy;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFD),
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: const Color(0xFFDCE5F1)),
      ),
      child: Row(
        children: [
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: const Color(0xFFE5EEFF),
              borderRadius: BorderRadius.circular(14),
            ),
            child: const Icon(Icons.person_outline),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  user?.displayName ?? assignment.userId,
                  style: const TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 15,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  user?.email ?? assignment.userId,
                  style: const TextStyle(color: Color(0xFF5E728A)),
                ),
                const SizedBox(height: 6),
                Text(
                  'Assigned ${_formatDateTime(assignment.assignedAtUtc)}',
                  style: const TextStyle(
                    color: Color(0xFF5E728A),
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Remove assignment',
            onPressed: busy ? null : onRemove,
            icon: const Icon(Icons.person_remove_outlined),
          ),
        ],
      ),
    );
  }
}

class _DetailStatCard extends StatelessWidget {
  const _DetailStatCard({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 220,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFFF9FBFF), Color(0xFFF1F6FE)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
          borderRadius: BorderRadius.circular(22),
          border: Border.all(color: const Color(0xFFDCE5F1)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: const Color(0xFF0D5EF8)),
            const SizedBox(height: 12),
            Text(
              label,
              style: const TextStyle(
                color: Color(0xFF5E728A),
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              value,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final normalized = status.trim().toLowerCase();
    late final Color foreground;
    late final Color background;

    switch (normalized) {
      case 'archived':
        foreground = const Color(0xFF8A4B14);
        background = const Color(0xFFFFF2D8);
        break;
      case 'closed':
        foreground = const Color(0xFF7A271A);
        background = const Color(0xFFFEE4E2);
        break;
      default:
        foreground = const Color(0xFF0C6B58);
        background = const Color(0xFFDDF7EF);
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        status,
        style: TextStyle(
          color: foreground,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _MiniInfoPill extends StatelessWidget {
  const _MiniInfoPill({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: const Color(0xFFDCE5F1)),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Color(0xFF5E728A),
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _EmptySelectionCard extends StatelessWidget {
  const _EmptySelectionCard();

  @override
  Widget build(BuildContext context) {
    return const Card(
      child: Center(
        child: Padding(
          padding: EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.touch_app_outlined, size: 54),
              SizedBox(height: 14),
              Text(
                'Select a project to inspect its operational detail.',
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ProjectDraft {
  const _ProjectDraft({
    required this.name,
    required this.code,
    required this.description,
    required this.startDate,
    required this.endDate,
  });

  final String name;
  final String? code;
  final String? description;
  final DateTime? startDate;
  final DateTime? endDate;
}

String? _blankToNull(String value) {
  final trimmed = value.trim();
  return trimmed.isEmpty ? null : trimmed;
}

String _projectRangeLabel(ProjectRecord project) {
  if (project.startDate == null && project.endDate == null) {
    return 'No schedule';
  }
  if (project.startDate != null && project.endDate != null) {
    return '${_formatDateTime(project.startDate!, dateOnly: true)} - ${_formatDateTime(project.endDate!, dateOnly: true)}';
  }
  if (project.startDate != null) {
    return 'Starts ${_formatDateTime(project.startDate!, dateOnly: true)}';
  }
  return 'Ends ${_formatDateTime(project.endDate!, dateOnly: true)}';
}

String _formatDateTime(DateTime value, {bool dateOnly = false}) {
  final local = value.toLocal();
  final month = local.month.toString().padLeft(2, '0');
  final day = local.day.toString().padLeft(2, '0');
  if (dateOnly) {
    return '${local.year}-$month-$day';
  }
  final hour = local.hour.toString().padLeft(2, '0');
  final minute = local.minute.toString().padLeft(2, '0');
  return '${local.year}-$month-$day $hour:$minute';
}
