import re

with open('lib/core/presentation/main_layout.dart', 'r') as f:
    text = f.read()

scaffold_repl = """Scaffold(
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
    );"""

text = re.sub(r'Scaffold\(\s*drawer: isDesktop \? null : Drawer\(child: sidebar\),\s*body: Row\(\s*children: \[\s*if \(isDesktop\) SizedBox\(width: 286, child: sidebar\),\s*Expanded\(child: child\),\s*\],\s*\),\s*\);', scaffold_repl, text, flags=re.DOTALL)

with open('lib/core/presentation/main_layout.dart', 'w') as f:
    f.write(text)
