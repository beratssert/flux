import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/app_config.dart';

final authApiClientProvider = Provider<AuthApiClient>(
  (ref) => AuthApiClient(),
);

class AuthApiClient {
  AuthApiClient({Dio? dio})
      : _dio = dio ??
            Dio(
              BaseOptions(
                baseUrl: AppConfig.apiBaseUrl,
                connectTimeout: const Duration(seconds: 10),
                receiveTimeout: const Duration(seconds: 10),
              ),
            );

  final Dio _dio;

  Future<Response<dynamic>> login({
    required String email,
    required String password,
  }) {
    return _dio.post(
      '/api/v1/auth/login',
      data: <String, dynamic>{
        'email': email,
        'password': password,
      },
    );
  }

  Future<Response<dynamic>> register({
    required String firstName,
    required String lastName,
    required String email,
    required String userName,
    required String password,
  }) {
    return _dio.post(
      '/api/v1/auth/register',
      data: <String, dynamic>{
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'userName': userName,
        'password': password,
        'confirmPassword': password,
      },
    );
  }

  Future<Response<dynamic>> getMyProfile({
    required String accessToken,
  }) {
    return _dio.get(
      '/api/v1/users/me',
      options: Options(
        headers: <String, dynamic>{
          'Authorization': 'Bearer $accessToken',
        },
      ),
    );
  }

  Future<Response<dynamic>> forgotPassword({
    required String email,
  }) async {
    throw UnsupportedError(
      'Password reset is not supported by the current backend.',
    );
  }

  Future<Response<dynamic>> resetPassword({
    required String email,
    required String token,
    required String password,
    required String confirmPassword,
  }) async {
    throw UnsupportedError(
      'Password reset is not supported by the current backend.',
    );
  }

  Future<Response<dynamic>> confirmEmail({
    required String userId,
    required String code,
  }) async {
    throw UnsupportedError(
      'Email confirmation is not exposed by the current backend.',
    );
  }
}
