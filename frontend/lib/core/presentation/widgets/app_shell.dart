import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../features/auth/data/auth_session_controller.dart';
import '../../../features/auth/data/auth_models.dart';

class AppShell extends ConsumerStatefulWidget {
  const AppShell({
    super.key,
    required this.child,
  });

  final Widget child;

  @override
  ConsumerState<AppShell> createState() => _AppShellState();
}

class _AppShellState extends ConsumerState<AppShell> {
  bool _logoutBusy = false;

  Future<void> _logout() async {
    setState(() => _logoutBusy = true);
    try {
      await ref.read(authSessionControllerProvider.notifier).signOut();
    } finally {
      if (mounted) setState(() => _logoutBusy = false);
    }
  }

  void _showSettingsMessage() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Settings coming soon!')),
    );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authSessionControllerProvider);
    final session = authState.session;

    if (session == null) {
      return widget.child;
    }

    final profile = session.profile;
    final isCompact = MediaQuery.sizeOf(context).width < 1100;

    // Determine title logically based on current route
    final currentPath = GoRouterState.of(context).matchedLocation;
    String appBarTitle = 'Flux';
    if (currentPath == '/calendar') appBarTitle = 'Calendar';
    if (currentPath == '/') appBarTitle = 'Time Tracker';

    if (isCompact) {
      return Scaffold(
        backgroundColor: const Color(0xFFF5F7FB),
        appBar: AppBar(
          backgroundColor: Colors.white,
          surfaceTintColor: Colors.transparent,
          title: Text(appBarTitle),
        ),
        drawer: Drawer(
          child: SafeArea(
            child: _Sidebar(
              profile: profile,
              logoutBusy: _logoutBusy,
              onSettings: _showSettingsMessage,
              onLogout: _logout,
            ),
          ),
        ),
        body: widget.child,
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
                logoutBusy: _logoutBusy,
                onSettings: _showSettingsMessage,
                onLogout: _logout,
              ),
            ),
            Expanded(
              child: widget.child,
            ),
          ],
        ),
      ),
    );
  }
}

class _Sidebar extends StatelessWidget {
  const _Sidebar({
    required this.profile,
    required this.logoutBusy,
    required this.onSettings,
    required this.onLogout,
  });

  final AuthProfile profile;
  final bool logoutBusy;
  final VoidCallback onSettings;
  final VoidCallback onLogout;

  @override
  Widget build(BuildContext context) {
    final currentPath = GoRouterState.of(context).matchedLocation;

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
          // Scrollable area for Navigation buttons
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                _NavItem(
                  icon: Icons.access_time_rounded,
                  label: 'Time Tracker',
                  selected: currentPath == '/' || currentPath.isEmpty,
                  onTap: () => context.go('/'),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.calendar_month_rounded,
                  label: 'Calendar',
                  selected: currentPath == '/calendar',
                  onTap: () => context.go('/calendar'),
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.receipt_long_rounded,
                  label: 'Expenses',
                  selected: false,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Expenses Coming Soon!')),
                    );
                  },
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.bar_chart_rounded,
                  label: 'Report',
                  selected: false,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Report Coming Soon!')),
                    );
                  },
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.folder_copy_rounded,
                  label: 'Projects',
                  selected: false,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Projects Coming Soon!')),
                    );
                  },
                ),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.groups_rounded,
                  label: 'Members',
                  selected: false,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Members Coming Soon!')),
                    );
                  },
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          // Bottom area for User Profile and Logout
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
      onTap: () {
        if (onTap != null) onTap!();
        final isCompact = MediaQuery.sizeOf(context).width < 1100;
        // Close the drawer if we are navigating
        if (isCompact && Scaffold.of(context).isDrawerOpen) {
          Scaffold.of(context).closeDrawer();
        }
      },
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
