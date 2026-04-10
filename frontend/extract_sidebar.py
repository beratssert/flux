import re
with open('lib/features/time_tracker/presentation/time_tracker_page.dart', 'r') as f:
    content = f.read()

sidebar_class = re.search(r'(class _Sidebar extends StatelessWidget \{.*?\n\})', content, re.DOTALL)
nav_item_class = re.search(r'(class _NavItem extends StatelessWidget \{.*?\n\})', content, re.DOTALL)

with open('lib/core/presentation/main_layout.dart', 'w') as f:
    f.write("import 'package:flutter/material.dart';\n")
    f.write("import 'package:flutter_riverpod/flutter_riverpod.dart';\n")
    f.write("import 'package:go_router/go_router.dart';\n")
    f.write("import '../../features/auth/data/auth_session_controller.dart';\n\n")
    f.write("class MainLayout extends ConsumerWidget {\n")
    f.write("  final Widget child;\n")
    f.write("  const MainLayout({super.key, required this.child});\n\n")
    f.write("  @override\n")
    f.write("  Widget build(BuildContext context, WidgetRef ref) {\n")
    f.write("    final authState = ref.watch(authSessionControllerProvider);\n")
    f.write("    final profile = authState.session?.profile;\n")
    f.write("    final isDesktop = MediaQuery.sizeOf(context).width >= 1100;\n")
    f.write("    final sidebar = _Sidebar(\n")
    f.write("      profile: profile!,\n")
    f.write("      logoutBusy: false,\n")
    f.write("      onSettings: () {},\n")
    f.write("      onLogout: () => ref.read(authSessionControllerProvider.notifier).logout(),\n")
    f.write("    );\n")
    f.write("    return Scaffold(\n")
    f.write("      drawer: isDesktop ? null : Drawer(child: sidebar),\n")
    f.write("      body: Row(\n")
    f.write("        children: [\n")
    f.write("          if (isDesktop) SizedBox(width: 286, child: sidebar),\n")
    f.write("          Expanded(child: child),\n")
    f.write("        ],\n")
    f.write("      ),\n")
    f.write("    );\n")
    f.write("  }\n")
    f.write("}\n\n")

    if sidebar_class:
        f.write(sidebar_class.group(1))
    if nav_item_class:
        f.write("\n\n" + nav_item_class.group(1))

