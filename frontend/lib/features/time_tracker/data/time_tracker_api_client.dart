import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/app_config.dart';
import '../../auth/data/auth_session_controller.dart';
import 'time_tracker_models.dart';

final timeTrackerApiClientProvider = Provider<TimeTrackerApiClient>(
  (ref) {
    final session = ref.watch(authSessionControllerProvider).session;
    return TimeTrackerApiClient(accessToken: session?.accessToken);
  },
);

class TimeTrackerApiClient {
  TimeTrackerApiClient({
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

  Future<RunningTimerRecord?> getActiveTimer() async {
    final response = await _dio.get<dynamic>(
      '/api/v1/Timers/active',
      options: _authorizedOptions(),
    );

    if (response.statusCode == 204 || _isEmptyPayload(response.data)) {
      return null;
    }

    return RunningTimerRecord.fromJson(_asJsonMap(response.data));
  }

  Future<TimeEntriesPage> getTimeEntries({
    int pageNumber = 1,
    int pageSize = 100,
    DateTime? from,
    DateTime? to,
  }) async {
    final response = await _dio.get<dynamic>(
      '/api/v1/TimeEntries',
      queryParameters: <String, dynamic>{
        'pageNumber': pageNumber,
        'pageSize': pageSize,
        if (from != null) 'from': from.toUtc().toIso8601String(),
        if (to != null) 'to': to.toUtc().toIso8601String(),
      },
      options: _authorizedOptions(),
    );

    return TimeEntriesPage.fromJson(_asJsonMap(response.data));
  }

  Future<void> startTimer({
    required int projectId,
    required String description,
    required bool isBillable,
  }) {
    return _dio.post<dynamic>(
      '/api/v1/Timers/start',
      data: <String, dynamic>{
        'projectId': projectId,
        'description': description,
        'isBillable': isBillable,
      },
      options: _authorizedOptions(),
    );
  }

  Future<void> stopTimer() {
    return _dio.post<dynamic>(
      '/api/v1/Timers/stop',
      options: _authorizedOptions(),
    );
  }

  Future<void> createTimeEntry({
    required int projectId,
    required DateTime entryDate,
    required String description,
    required bool isBillable,
    DateTime? startTimeUtc,
    DateTime? endTimeUtc,
    int? durationMinutes,
  }) {
    return _dio.post<dynamic>(
      '/api/v1/TimeEntries',
      data: _timeEntryPayload(
        projectId: projectId,
        entryDate: entryDate,
        description: description,
        isBillable: isBillable,
        startTimeUtc: startTimeUtc,
        endTimeUtc: endTimeUtc,
        durationMinutes: durationMinutes,
      ),
      options: _authorizedOptions(),
    );
  }

  Future<void> updateTimeEntry({
    required int id,
    required int projectId,
    required DateTime entryDate,
    required String description,
    required bool isBillable,
    DateTime? startTimeUtc,
    DateTime? endTimeUtc,
    int? durationMinutes,
  }) {
    return _dio.put<dynamic>(
      '/api/v1/TimeEntries/$id',
      data: <String, dynamic>{
        'id': id,
        ..._timeEntryPayload(
          projectId: projectId,
          entryDate: entryDate,
          description: description,
          isBillable: isBillable,
          startTimeUtc: startTimeUtc,
          endTimeUtc: endTimeUtc,
          durationMinutes: durationMinutes,
        ),
      },
      options: _authorizedOptions(),
    );
  }

  Future<void> deleteTimeEntry({
    required int id,
  }) {
    return _dio.delete<dynamic>(
      '/api/v1/TimeEntries/$id',
      options: _authorizedOptions(),
    );
  }

  Map<String, dynamic> _timeEntryPayload({
    required int projectId,
    required DateTime entryDate,
    required String description,
    required bool isBillable,
    DateTime? startTimeUtc,
    DateTime? endTimeUtc,
    int? durationMinutes,
  }) {
    return <String, dynamic>{
      'projectId': projectId,
      'entryDate': entryDate.toUtc().toIso8601String(),
      'description': description,
      'isBillable': isBillable,
      if (startTimeUtc != null)
        'startTimeUtc': startTimeUtc.toUtc().toIso8601String(),
      if (endTimeUtc != null)
        'endTimeUtc': endTimeUtc.toUtc().toIso8601String(),
      if (durationMinutes != null) 'durationMinutes': durationMinutes,
    };
  }

  Options _authorizedOptions() {
    final token = accessToken;
    if (token == null || token.isEmpty) {
      throw StateError('You must be signed in to access the time tracker.');
    }

    return Options(
      headers: <String, dynamic>{
        'Authorization': 'Bearer $token',
      },
    );
  }
}

bool _isEmptyPayload(dynamic value) {
  if (value == null) {
    return true;
  }
  if (value is String) {
    return value.trim().isEmpty;
  }
  return false;
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
