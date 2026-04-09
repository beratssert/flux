import 'package:flutter/material.dart';

import '../features/auth/presentation/confirm_email_page.dart';
import '../features/auth/presentation/forgot_password_page.dart';
import '../features/auth/presentation/login_page.dart';
import '../features/auth/presentation/register_page.dart';
import '../features/auth/presentation/reset_password_page.dart';
import '../features/time_tracker/presentation/time_tracker_page.dart';

abstract class AppRoutes {
  static const login = '/login';
  static const register = '/register';
  static const forgotPassword = '/forgot-password';
  static const confirmEmail = '/confirm-email';
  static const resetPassword = '/reset-password';
  static const timeTracker = '/time-tracker';
}

class AppRouter {
  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      case AppRoutes.login:
        return MaterialPageRoute<void>(builder: (_) => const LoginPage());
      case AppRoutes.register:
        return MaterialPageRoute<void>(builder: (_) => const RegisterPage());
      case AppRoutes.forgotPassword:
        return MaterialPageRoute<void>(
          builder: (_) => const ForgotPasswordPage(),
        );
      case AppRoutes.confirmEmail:
        return MaterialPageRoute<void>(
          builder: (_) => const ConfirmEmailPage(),
        );
      case AppRoutes.resetPassword:
        return MaterialPageRoute<void>(
          builder: (_) => const ResetPasswordPage(),
        );
      case AppRoutes.timeTracker:
        return MaterialPageRoute<void>(
          builder: (_) => const TimeTrackerPage(),
        );
      default:
        return MaterialPageRoute<void>(
          builder: (_) => const Scaffold(
            body: Center(child: Text('Page not found')),
          ),
        );
    }
  }
}
