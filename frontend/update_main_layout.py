import re

with open('lib/core/presentation/main_layout.dart', 'r') as f:
    content = f.read()

# Replace _NavItem to handle taps
nav_item_replacement = """class _NavItem extends StatelessWidget {
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
}"""
content = re.sub(r'class _NavItem extends StatelessWidget \{.*?\n\}', nav_item_replacement, content, flags=re.DOTALL)

# Update _Sidebar listview children
sidebar_listview_replacement = """ListView(
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
            )"""
content = re.sub(r'ListView\(\s*padding: const EdgeInsets\.symmetric\(horizontal: 16\),\s*children: const \[\s*_NavItem\(\s*icon: Icons\.access_time_rounded,\s*label: \'Time Tracker\',\s*selected: true,\s*\),\s*SizedBox\(height: 8\),\s*_NavItem\(icon: Icons\.bar_chart_rounded, label: \'Report\'\),\s*SizedBox\(height: 8\),\s*_NavItem\(icon: Icons\.receipt_long_rounded, label: \'Expenses\'\),\s*SizedBox\(height: 8\),\s*_NavItem\(icon: Icons\.calendar_month_rounded, label: \'Calendar\'\),\s*SizedBox\(height: 8\),\s*_NavItem\(icon: Icons\.folder_copy_rounded, label: \'Projects\'\),\s*SizedBox\(height: 8\),\s*_NavItem\(icon: Icons\.groups_rounded, label: \'Members\'\),\s*\],\s*\)', sidebar_listview_replacement, content, flags=re.DOTALL)

with open('lib/core/presentation/main_layout.dart', 'w') as f:
    f.write(content)
