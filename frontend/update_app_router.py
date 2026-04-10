with open('lib/core/router/app_router.dart', 'r') as f:
    text = f.read()

text = text.replace("import '../presentation/main_layout.dart';", "import '../presentation/main_layout.dart';\nimport '../presentation/launch_screen.dart';")

text = text.replace("""redirect: (context, state) {
      if (authState.status == AuthStatus.checking) return null;

      final isAuth = authState.isAuthenticated;
      final isGoingToLogin = state.uri.toString() == '/login';

      if (!isAuth && !isGoingToLogin) {
        return '/login';
      }

      if (isAuth && isGoingToLogin) {
        return '/';
      }

      return null;
    },""", """redirect: (context, state) {
      final isAuth = authState.isAuthenticated;
      final isChecking = authState.status == AuthStatus.checking;
      final isGoingToLogin = state.uri.toString() == '/login';
      final isGoingToChecking = state.uri.toString() == '/checking';

      if (isChecking) return '/checking';

      if (!isAuth && !isGoingToLogin) return '/login';

      if (isAuth && (isGoingToLogin || isGoingToChecking)) return '/';

      return null;
    },""")

text = text.replace("""routes: [
      GoRoute(
        path: '/login',""", """routes: [
      GoRoute(
        path: '/checking',
        builder: (context, state) => const LaunchScreen(),
      ),
      GoRoute(
        path: '/login',""")

with open('lib/core/router/app_router.dart', 'w') as f:
    f.write(text)
