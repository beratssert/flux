import re

with open('lib/features/time_tracker/presentation/time_tracker_page.dart', 'r') as f:
    content = f.read()

# 1. Remove _Sidebar and _NavItem classes
content = re.sub(r'class _Sidebar extends StatelessWidget \{.*?\n\}\n*', '', content, flags=re.DOTALL)
content = re.sub(r'class _NavItem extends StatelessWidget \{.*?\n\}\n*', '', content, flags=re.DOTALL)

# Remove trailing helper functions
content = re.sub(r'String _initialsFor\(String name\) \{.*?\n\}\n*', '', content, flags=re.DOTALL)
content = re.sub(r'String _titleCaseRole\(String role\) \{.*?\n\}\n*', '', content, flags=re.DOTALL)

# Replace build method to return _buildMainArea unconditionally
build_replacement = """  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authSessionControllerProvider);
    final profile = authState.session?.profile;

    if (profile == null) {
      return const Center(child: CircularProgressIndicator());
    }

    final isCompact = MediaQuery.sizeOf(context).width < 1100;
    return _buildMainArea(profile, isCompact);
  }"""
content = re.sub(r'  @override\n  Widget build\(BuildContext context\) \{.*?\n  \}\n\n  Widget _buildMainArea', build_replacement + '\n\n  Widget _buildMainArea', content, flags=re.DOTALL)

with open('lib/features/time_tracker/presentation/time_tracker_page.dart', 'w') as f:
    f.write(content)
