import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../features/auth/data/auth_session_controller.dart';
import '../../features/auth/data/auth_models.dart';

class MainLayout extends ConsumerWidget {
  final Widget child;
  const MainLayout({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authSessionControllerProvider);
    final profile = authState.session?.profile;
    final isDesktop = MediaQuery.sizeOf(context).width >= 1100;
    final sidebar = _Sidebar(
      profile: profile!,
      logoutBusy: false,
      onSettings: () {},
      onLogout: () => ref.read(authSessionControllerProvider.notifier).signOut(),
    );
    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FB),
      appBar: isDesktop
          ? null
          : AppBar(
              backgroundColor: Colors.white,
              surfaceTintColor: Colors.transparent,
              title: const Text('Flux'),
            ),
      drawer: isDesktop ? null : Drawer(child: SafeArea(child: sidebar)),
      body: SafeArea(
        child: Row(
          children: [
            if (isDesktop) SizedBox(width: 286, child: sidebar),
            Expanded(child: child),
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
                  selected: GoRouterState.of(context).uri.toString() == '/',
                  onTap: () => context.go('/'),
                ),
                const SizedBox(height: 8),
                _NavItem(icon: Icons.bar_chart_rounded, label: 'Report'),
                const SizedBox(height: 8),
                _NavItem(icon: Icons.receipt_long_rounded, label: 'Expenses'),
                const SizedBox(height: 8),
                _NavItem(
                  icon: Icons.calendar_month_rounded, 
                  label: 'Calendar',
                  selected: GoRouterState.of(context).uri.toString() == '/calendar',
                  onTap: () => context.go('/calendar'),
                ),
                const SizedBox(height: 8),
                _NavItem(icon: Icons.folder_copy_rounded, label: 'Projects'),
                const SizedBox(height: 8),
                _NavItem(icon: Icons.groups_rounded, label: 'Members'),
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
              color: selected ? const Color(0xFF1E7BF2) : const Color(0xFF728099),
            ),
            const SizedBox(width: 14),
            Text(
              label,
              style: TextStyle(
                color:
                    selected ? const Color(0xFF1E7BF2) : const Color(0xFF53627C),
                fontSize: 15,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}String _initialsFor(String name) {
  final parts = name.trim().split(' ');
  if (parts.isEmpty) return 'U';
  if (parts.length == 1) {
    if (parts[0].isEmpty) return 'U';
    return parts[0].substring(0, 1).toUpperCase();
  }
  final first = parts[0].substring(0, 1);
  final last = parts[parts.length - 1].substring(0, 1);
  return '$first$last'.toUpperCase();
}

String _titleCaseRole(String role) {
  if (role.isEmpty) return 'User';
  return role[0].toUpperCase() + role.substring(1).toLowerCase();
}
