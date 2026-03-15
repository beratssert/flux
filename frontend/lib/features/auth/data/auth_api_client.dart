import 'package:dio/dio.dart';

import '../../../core/app_config.dart';

class AuthApiClient {
  final Dio _dio;

  AuthApiClient({Dio? dio})
      : _dio = dio ??
            Dio(
              BaseOptions(
                baseUrl: AppConfig.apiBaseUrl,
                connectTimeout: const Duration(seconds: 10),
                receiveTimeout: const Duration(seconds: 10),
              ),
            );

  Future<Response<dynamic>> login({
    required String email,
    required String password,
  }) {
    return _dio.post(
      '/api/Account/authenticate',
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
      '/api/Account/register',
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

  Future<Response<dynamic>> forgotPassword({
    required String email,
  }) {
    return _dio.post(
      '/api/Account/forgot-password',
      data: <String, dynamic>{
        'email': email,
      },
      options: Options(
        headers: <String, dynamic>{
          // Backend, origin header'ını reset linki için kullanıyor.
          'origin': AppConfig.apiBaseUrl,
        },
      ),
    );
  }

  Future<Response<dynamic>> resetPassword({
    required String email,
    required String token,
    required String password,
    required String confirmPassword,
  }) {
    return _dio.post(
      '/api/Account/reset-password',
      data: <String, dynamic>{
        'email': email,
        'token': token,
        'password': password,
        'confirmPassword': confirmPassword,
      },
    );
  }

  Future<Response<dynamic>> confirmEmail({
    required String userId,
    required String code,
  }) {
    return _dio.get(
      '/api/Account/confirm-email',
      queryParameters: <String, dynamic>{
        'userId': userId,
        'code': code,
      },
    );
  }
}

