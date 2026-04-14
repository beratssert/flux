import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/app_config.dart';
import '../../auth/data/auth_session_controller.dart';
import 'projects_models.dart';

final projectsApiClientProvider = Provider<ProjectsApiClient>(
  (ref) {
    final session = ref.watch(authSessionControllerProvider).session;
    return ProjectsApiClient(accessToken: session?.accessToken);
  },
);

class ProjectsApiClient {
  ProjectsApiClient({
    required this.accessToken,
    Dio? dio,
  }) : _dio = dio ??
            Dio(
              BaseOptions(
                baseUrl: AppConfig.apiBaseUrl,
                connectTimeout: const Duration(seconds: 10),
                receiveTimeout: const Duration(seconds: 10),
              ),
            );

  final String? accessToken;
  final Dio _dio;

  Future<ProjectsPage> getProjects({
    int page = 1,
    int pageSize = 20,
    String? status,
    String? managerUserId,
    String? query,
  }) async {
    final response = await _dio.get<dynamic>(
      '/api/v1/projects',
      queryParameters: <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        if (status != null && status.trim().isNotEmpty) 'status': status,
        if (managerUserId != null && managerUserId.trim().isNotEmpty)
          'managerUserId': managerUserId,
        if (query != null && query.trim().isNotEmpty) 'q': query.trim(),
      },
      options: _authorizedOptions(),
    );

    return ProjectsPage.fromJson(_asJsonMap(response.data));
  }

  Future<ProjectRecord> getProjectById(int id) async {
    final response = await _dio.get<dynamic>(
      '/api/v1/projects/$id',
      options: _authorizedOptions(),
    );

    return ProjectRecord.fromJson(_asJsonMap(response.data));
  }

  Future<ProjectRecord> createProject({
    required String name,
    String? code,
    String? description,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final response = await _dio.post<dynamic>(
      '/api/v1/projects',
      data: <String, dynamic>{
        'name': name.trim(),
        if (code != null) 'code': code.trim(),
        if (description != null) 'description': description.trim(),
        if (startDate != null) 'startDate': _toDateOnly(startDate),
        if (endDate != null) 'endDate': _toDateOnly(endDate),
      },
      options: _authorizedOptions(),
    );

    return ProjectRecord.fromJson(_asJsonMap(response.data));
  }

  Future<ProjectRecord> updateProject({
    required int id,
    String? name,
    String? code,
    String? description,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final response = await _dio.patch<dynamic>(
      '/api/v1/projects/$id',
      data: <String, dynamic>{
        if (name != null) 'name': name.trim(),
        if (code != null) 'code': code.trim(),
        if (description != null) 'description': description.trim(),
        if (startDate != null) 'startDate': _toDateOnly(startDate),
        if (endDate != null) 'endDate': _toDateOnly(endDate),
      },
      options: _authorizedOptions(),
    );

    return ProjectRecord.fromJson(_asJsonMap(response.data));
  }

  Future<ProjectRecord> updateProjectStatus({
    required int id,
    required String status,
  }) async {
    final response = await _dio.patch<dynamic>(
      '/api/v1/projects/$id/status',
      data: <String, dynamic>{
        'status': status,
      },
      options: _authorizedOptions(),
    );

    return ProjectRecord.fromJson(_asJsonMap(response.data));
  }

  Future<List<ProjectAssignmentRecord>> getProjectAssignments(
      int projectId) async {
    final response = await _dio.get<dynamic>(
      '/api/v1/projects/$projectId/assignments',
      options: _authorizedOptions(),
    );

    final raw = response.data;
    if (raw is! List) {
      return const <ProjectAssignmentRecord>[];
    }

    return raw
        .whereType<Map>()
        .map(
          (item) => ProjectAssignmentRecord.fromJson(
            item.map((key, dynamic value) => MapEntry(key.toString(), value)),
          ),
        )
        .toList(growable: false);
  }

  Future<void> addProjectAssignment({
    required int projectId,
    required String userId,
  }) {
    return _dio.post<dynamic>(
      '/api/v1/projects/$projectId/assignments',
      data: <String, dynamic>{
        'userId': userId,
      },
      options: _authorizedOptions(),
    );
  }

  Future<void> removeProjectAssignment({
    required int projectId,
    required String userId,
  }) {
    return _dio.delete<dynamic>(
      '/api/v1/projects/$projectId/assignments/$userId',
      options: _authorizedOptions(),
    );
  }

  Future<List<MyProjectAssignmentRecord>> getMyAssignments() async {
    final response = await _dio.get<dynamic>(
      '/api/v1/users/me/assignments',
      options: _authorizedOptions(),
    );

    final raw = response.data;
    if (raw is! List) {
      return const <MyProjectAssignmentRecord>[];
    }

    return raw
        .whereType<Map>()
        .map(
          (item) => MyProjectAssignmentRecord.fromJson(
            item.map((key, dynamic value) => MapEntry(key.toString(), value)),
          ),
        )
        .toList(growable: false);
  }

  Future<UsersPage> getEmployees({
    String? query,
    int page = 1,
    int pageSize = 20,
    int? projectId,
  }) async {
    final response = await _dio.get<dynamic>(
      '/api/v1/users',
      queryParameters: <String, dynamic>{
        'role': 'Employee',
        'isActive': true,
        'page': page,
        'pageSize': pageSize,
        if (projectId != null) 'projectId': projectId,
        if (query != null && query.trim().isNotEmpty) 'q': query.trim(),
      },
      options: _authorizedOptions(),
    );

    return UsersPage.fromJson(_asJsonMap(response.data));
  }

  Options _authorizedOptions() {
    final token = accessToken;
    if (token == null || token.isEmpty) {
      throw StateError('You must be signed in to access projects.');
    }

    return Options(
      headers: <String, dynamic>{
        'Authorization': 'Bearer $token',
      },
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

String _toDateOnly(DateTime value) {
  final normalized = DateTime(value.year, value.month, value.day);
  final month = normalized.month.toString().padLeft(2, '0');
  final day = normalized.day.toString().padLeft(2, '0');
  return '${normalized.year}-$month-$day';
}
