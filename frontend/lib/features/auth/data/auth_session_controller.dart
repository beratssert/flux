import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_api_client.dart';
import 'auth_models.dart';
import 'auth_storage.dart';

final authStorageProvider = Provider<AuthStorage>(
  (ref) => SecureAuthStorage(),
);

final authSessionControllerProvider =
    StateNotifierProvider<AuthSessionController, AuthState>(
  (ref) => AuthSessionController(ref),
);

enum AuthStatus {
  checking,
  authenticated,
  unauthenticated,
}

class AuthState {
  const AuthState._({
    required this.status,
    this.session,
  });

  const AuthState.checking() : this._(status: AuthStatus.checking);

  const AuthState.authenticated(AuthSession session)
      : this._(
          status: AuthStatus.authenticated,
          session: session,
        );

  const AuthState.unauthenticated()
      : this._(status: AuthStatus.unauthenticated);

  final AuthStatus status;
  final AuthSession? session;

  bool get isAuthenticated =>
      status == AuthStatus.authenticated && session != null;
}

class AuthSessionController extends StateNotifier<AuthState> {
  AuthSessionController(this._ref) : super(const AuthState.checking()) {
    restoreSession();
  }

  final Ref _ref;

  Future<void> restoreSession() async {
    final rawSession = await _ref.read(authStorageProvider).readSession();
    if (rawSession == null || rawSession.isEmpty) {
      state = const AuthState.unauthenticated();
      return;
    }

    try {
      final session = AuthSession.fromJson(
        _asJsonMap(jsonDecode(rawSession)),
      );

      if (session.isExpired || session.accessToken.isEmpty) {
        await signOut();
        return;
      }

      final hydrated = await _hydrateSession(
        session,
        tolerateProfileFailure: true,
      );
      await _persist(hydrated);
      state = AuthState.authenticated(hydrated);
    } catch (_) {
      await _ref.read(authStorageProvider).clearSession();
      state = const AuthState.unauthenticated();
    }
  }

  Future<void> signIn({
    required String email,
    required String password,
  }) async {
    final response = await _ref.read(authApiClientProvider).login(
          email: email,
          password: password,
        );

    final session = AuthSession.fromAuthResponse(
      _asJsonMap(response.data),
    );

    if (session.accessToken.isEmpty) {
      throw StateError('Access token was not returned by the backend.');
    }

    final hydrated = await _hydrateSession(
      session,
      tolerateProfileFailure: true,
    );

    await _persist(hydrated);
    state = AuthState.authenticated(hydrated);
  }

  Future<void> signOut() async {
    await _ref.read(authStorageProvider).clearSession();
    state = const AuthState.unauthenticated();
  }

  Future<AuthSession> _hydrateSession(
    AuthSession session, {
    required bool tolerateProfileFailure,
  }) async {
    try {
      final response = await _ref.read(authApiClientProvider).getMyProfile(
            accessToken: session.accessToken,
          );
      final profile = AuthProfile.fromJson(
        _asJsonMap(response.data),
      );

      final mergedProfile = session.profile.copyWith(
        id: profile.id.isNotEmpty ? profile.id : session.profile.id,
        email: profile.email.isNotEmpty ? profile.email : session.profile.email,
        firstName: profile.firstName,
        lastName: profile.lastName,
        role: profile.role ?? session.profile.role,
        isActive: profile.isActive,
        lastLoginAtUtc: profile.lastLoginAtUtc,
      );

      return session.copyWith(profile: mergedProfile);
    } catch (_) {
      if (!tolerateProfileFailure) {
        rethrow;
      }
      return session;
    }
  }

  Future<void> _persist(AuthSession session) {
    return _ref.read(authStorageProvider).writeSession(
          jsonEncode(session.toJson()),
        );
  }
}

Map<String, dynamic> _asJsonMap(dynamic value) {
  if (value is Map<String, dynamic>) {
    return value;
  }
  if (value is Map) {
    return value.map(
      (key, dynamic item) => MapEntry(key.toString(), item),
    );
  }
  throw StateError('Unexpected API payload.');
}
