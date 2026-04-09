import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/data/auth_session_controller.dart';
import '../../features/auth/presentation/login_page.dart';
import '../../features/auth/presentation/register_page.dart';
import '../../features/auth/presentation/forgot_password_page.dart';
import '../../features/time_tracker/presentation/time_tracker_page.dart';
import '../../features/calendar/presentation/pages/calendar_page.dart';
import '../presentation/widgets/app_shell.dart';
// Note: You can load your other pages here like reset password, etc.

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authSessionControllerProvider);

  return GoRouter(
    initialLocation: '/',
    redirect: (context, state) {
      final isAuth = authState.isAuthenticated;
      final isChecking = authState.status == AuthStatus.checking;

      // Unauthenticated routes that don't require login
      final isGoingToLogin = state.matchedLocation == '/login';
      final isGoingToRegister = state.matchedLocation == '/register';
      final isGoingToForgot = state.matchedLocation == '/forgot-password';

      final isAuthRoute =
          isGoingToLogin || isGoingToRegister || isGoingToForgot;

      // 1. If currently checking the session status, show splash
      if (isChecking) {
        return '/splash';
      }

      // 2. If not authenticated and trying to access a protected route, go to login
      if (!isAuth && !isAuthRoute) {
        return '/login';
      }

      // 3. If authenticated and trying to access an auth route, go to main dashboard (TimeTracker)
      if (isAuth && isAuthRoute) {
        return '/';
      }

      // --- ROLE-BASED ROUTE GUARDS ---
      // Get the user's role (e.g., 'admin', 'manager', 'employee')
      // final userRole = authState.session?.profile.role?.toLowerCase();

      // Example constraint for an admin-only area
      // final isGoingToAdmin = state.matchedLocation.startsWith('/admin');
      // if (isGoingToAdmin && userRole != 'admin') {
      //   return '/'; // Return to home if not authorized
      // }

      // Otherwise, no routing override, let them go where they asked
      return null;
    },
    routes: [
      GoRoute(
        path: '/splash',
        builder: (context, state) => const _LaunchScreen(),
      ),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginPage(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterPage(),
      ),
      GoRoute(
        path: '/forgot-password',
        builder: (context, state) => const ForgotPasswordPage(),
      ),
      ShellRoute(
        builder: (context, state, child) => AppShell(child: child),
        routes: [
          GoRoute(
            path: '/',
            builder: (context, state) => const TimeTrackerPage(),
          ),
          GoRoute(
            path: '/calendar',
            builder: (context, state) => const CalendarPage(),
          ),
        ],
      ),
      // --- EXAMPLE ADMIN ROUTE ---
      // GoRoute(
      //   path: '/admin',
      //   builder: (context, state) => const AdminDashboard(),
      // ),
    ],
  );
});

// We extracted _LaunchScreen from main.dart to here, or you can place it in a separate file.
class _LaunchScreen extends StatelessWidget {
  const _LaunchScreen();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Color(0xFF0D5EF8),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircularProgressIndicator(
              valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
            ),
            SizedBox(height: 18),
            Text(
              'Flux is preparing your workspace...',
              style: TextStyle(
                color: Colors.white,
                fontSize: 16,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
