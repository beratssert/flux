import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/data/auth_session_controller.dart';
import '../../features/auth/presentation/login_page.dart';
import '../../features/time_tracker/presentation/time_tracker_page.dart';
import '../../features/calendar/presentation/pages/calendar_page.dart';
import '../../features/expenses/presentation/expenses_page.dart';
import '../../features/projects/presentation/projects_page.dart';
import '../presentation/main_layout.dart';
import '../presentation/launch_screen.dart';
import '../../features/expenses/presentation/settings_page.dart';
import '../../features/expenses/presentation/expense_categories_page.dart';
import '../../features/expenses/presentation/currency_settings_page.dart';

final rootNavigatorKey = GlobalKey<NavigatorState>();
final shellNavigatorKey = GlobalKey<NavigatorState>();

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authSessionControllerProvider);

  return GoRouter(
    navigatorKey: rootNavigatorKey,
    initialLocation: '/',
    redirect: (context, state) {
      final isAuth = authState.isAuthenticated;
      final isChecking = authState.status == AuthStatus.checking;
      final isGoingToLogin = state.uri.toString() == '/login';
      final isGoingToChecking = state.uri.toString() == '/checking';

      if (isChecking) return '/checking';

      if (!isAuth && !isGoingToLogin) return '/login';

      if (isAuth && (isGoingToLogin || isGoingToChecking)) return '/';

      return null;
    },
    routes: [
      GoRoute(
        path: '/checking',
        builder: (context, state) => const LaunchScreen(),
      ),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginPage(),
      ),
      ShellRoute(
        navigatorKey: shellNavigatorKey,
        builder: (context, state, child) => MainLayout(child: child),
        routes: [
          GoRoute(
            path: '/',
            builder: (context, state) => const TimeTrackerPage(),
          ),
          GoRoute(
            path: '/calendar',
            builder: (context, state) => const CalendarPage(),
          ),
          GoRoute(
            path: '/expenses',
            builder: (context, state) => const ExpensesPage(),
          ),
          GoRoute(
            path: '/projects',
            builder: (context, state) => ProjectsWorkspacePage(
              session: authState.session!,
            ),
          ),
          GoRoute(
            path: '/settings',
            builder: (context, state) => const SettingsPage(),
            routes: [
              GoRoute(
                path: 'expense-categories',
                builder: (context, state) => const ExpenseCategoriesPage(),
              ),
              GoRoute(
                path: 'currencies',
                builder: (context, state) => const CurrencySettingsPage(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
});
