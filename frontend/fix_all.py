import re

# 1. main.dart cleanup
with open('lib/main.dart', 'r') as f:
    text = f.read()
# We just replace the entire content cleanly without AppRoot or old stuff
main_dart = """import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/router/app_router.dart';

void main() {
  runApp(const ProviderScope(child: MyApp()));
}

class MyApp extends ConsumerWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);

    return MaterialApp.router(
      title: 'Flux',
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF0D5EF8),
          brightness: Brightness.light,
        ),
        scaffoldBackgroundColor: const Color(0xFFF4F7FB),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16),
            borderSide: const BorderSide(color: Color(0xFFD6DEEA)),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16),
            borderSide: const BorderSide(color: Color(0xFFD6DEEA)),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16),
            borderSide: const BorderSide(
              color: Color(0xFF0D5EF8),
              width: 1.4,
            ),
          ),
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 16,
          ),
        ),
        cardTheme: CardThemeData(
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(24),
          ),
        ),
      ),
    );
  }
}
"""
with open('lib/main.dart', 'w') as f:
    f.write(main_dart)

# 2. main_layout.dart fix
with open('lib/core/presentation/main_layout.dart', 'r') as f:
    text = f.read()

# Add auth_models import
if 'auth_models.dart' not in text:
    text = text.replace("import '../../features/auth/data/auth_session_controller.dart';", "import '../../features/auth/data/auth_session_controller.dart';\nimport '../../features/auth/data/auth_models.dart';")

# Replace logout() with signOut()
text = text.replace("ref.read(authSessionControllerProvider.notifier).logout()", "ref.read(authSessionControllerProvider.notifier).signOut()")

with open('lib/core/presentation/main_layout.dart', 'w') as f:
    f.write(text)

