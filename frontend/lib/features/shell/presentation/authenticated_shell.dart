import 'package:flutter/material.dart';

import '../../auth/data/auth_models.dart';
import '../../projects/presentation/projects_page.dart';
import '../../time_tracker/presentation/time_tracker_page.dart';

class AuthenticatedShell extends StatefulWidget {
  const AuthenticatedShell({
    required this.session,
    this.initialIndex = 0,
    super.key,
  });

  final AuthSession session;
  final int initialIndex;

  @override
  State<AuthenticatedShell> createState() => _AuthenticatedShellState();
}

class _AuthenticatedShellState extends State<AuthenticatedShell> {
  late int _currentIndex;

  @override
  void initState() {
    super.initState();
    _currentIndex = widget.initialIndex.clamp(0, 1);
  }

  @override
  Widget build(BuildContext context) {
    final destinations = <_ShellDestination>[
      const _ShellDestination(
        label: 'Time Tracker',
        icon: Icons.timer_outlined,
        selectedIcon: Icons.timer,
      ),
      const _ShellDestination(
        label: 'Projects',
        icon: Icons.folder_copy_outlined,
        selectedIcon: Icons.folder_copy,
      ),
    ];

    final profile = widget.session.profile;
    final displayName = profile.displayName;
    final role = _resolveRole(widget.session);

    return LayoutBuilder(
      builder: (context, constraints) {
        final useRail = constraints.maxWidth >= 980;
        final body = _currentIndex == 0
            ? const TimeTrackerPage()
            : ProjectsWorkspacePage(session: widget.session);

        if (!useRail) {
          return Scaffold(
            body: body,
            bottomNavigationBar: NavigationBar(
              selectedIndex: _currentIndex,
              onDestinationSelected: (value) {
                setState(() {
                  _currentIndex = value;
                });
              },
              destinations: [
                for (final destination in destinations)
                  NavigationDestination(
                    icon: Icon(destination.icon),
                    selectedIcon: Icon(destination.selectedIcon),
                    label: destination.label,
                  ),
              ],
            ),
          );
        }

        return Scaffold(
          body: Row(
            children: [
              SafeArea(
                child: Container(
                  width: 248,
                  padding: const EdgeInsets.fromLTRB(18, 24, 18, 18),
                  decoration: const BoxDecoration(
                    color: Colors.white,
                    border: Border(
                      right: BorderSide(color: Color(0xFFDCE5F1)),
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: const Color(0xFFF3F7FC),
                          borderRadius: BorderRadius.circular(24),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              width: 48,
                              height: 48,
                              decoration: BoxDecoration(
                                color: const Color(0xFF0D5EF8),
                                borderRadius: BorderRadius.circular(16),
                              ),
                              child: const Icon(
                                Icons.bolt_rounded,
                                color: Colors.white,
                              ),
                            ),
                            const SizedBox(height: 14),
                            Text(
                              displayName,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                fontSize: 17,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              role,
                              style: const TextStyle(
                                color: Color(0xFF5E728A),
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: Align(
                                alignment: Alignment.topLeft,
                                child: NavigationRail(
                                  extended: true,
                                  minExtendedWidth: 210,
                                  backgroundColor: Colors.transparent,
                                  selectedIndex: _currentIndex,
                                  useIndicator: true,
                                  indicatorColor: const Color(0xFFE5EEFF),
                                  onDestinationSelected: (value) {
                                    setState(() {
                                      _currentIndex = value;
                                    });
                                  },
                                  destinations: [
                                    for (final destination in destinations)
                                      NavigationRailDestination(
                                        icon: Icon(destination.icon),
                                        selectedIcon:
                                            Icon(destination.selectedIcon),
                                        label: Text(destination.label),
                                      ),
                                  ],
                                ),
                              ),
                            ),
                            Text(
                              'Projects dashboard is available for managers and employees in this release.',
                              style: Theme.of(context)
                                  .textTheme
                                  .bodySmall
                                  ?.copyWith(
                                    color: const Color(0xFF6D7F93),
                                    height: 1.45,
                                  ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              Expanded(child: body),
            ],
          ),
        );
      },
    );
  }
}

class _ShellDestination {
  const _ShellDestination({
    required this.label,
    required this.icon,
    required this.selectedIcon,
  });

  final String label;
  final IconData icon;
  final IconData selectedIcon;
}

String _resolveRole(AuthSession session) {
  final profileRole = session.profile.role?.trim();
  if (profileRole != null && profileRole.isNotEmpty) {
    return profileRole;
  }
  if (session.roles.isNotEmpty) {
    return session.roles.first;
  }
  return 'Employee';
}
