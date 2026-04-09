import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/app_config.dart';
import '../../auth/data/auth_session_controller.dart';
import 'calendar_models.dart';

final calendarApiClientProvider = Provider<CalendarApiClient>(
  (ref) {
    final session = ref.watch(authSessionControllerProvider).session;
    return CalendarApiClient(accessToken: session?.accessToken);
  },
);

class CalendarApiClient {
  CalendarApiClient({
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

  Future<List<CalendarItemRecord>> getCalendarItems({
    required DateTime from,
    required DateTime to,
    int? projectId,
    bool includeTimeEntries = true,
  }) async {
    final response = await _dio.get<dynamic>(
      '/api/v1/Calendar',
      queryParameters: <String, dynamic>{
        'from': from.toUtc().toIso8601String(),
        'to': to.toUtc().toIso8601String(),
        if (projectId != null) 'projectId': projectId,
        'includeTimeEntries': includeTimeEntries,
      },
      options: _authorizedOptions(),
    );

    final data = response.data;
    if (data is! List) return const <CalendarItemRecord>[];

    return data
        .whereType<Map>()
        .map(
          (item) => CalendarItemRecord.fromJson(
            item.map(
              (key, dynamic value) => MapEntry(key.toString(), value),
            ),
          ),
        )
        .toList(growable: false);
  }

  Future<int> createCalendarEvent(CreateCalendarEventRequest request) async {
    final response = await _dio.post<dynamic>(
      '/api/v1/Calendar',
      data: request.toJson(),
      options: _authorizedOptions(),
    );
    if (response.data is int) return response.data as int;
    return 0;
  }

  Future<void> deleteCalendarEvent(int id) {
    return _dio.delete<dynamic>(
      '/api/v1/Calendar/$id',
      options: _authorizedOptions(),
    );
  }

  Options _authorizedOptions() {
    final token = accessToken;
    if (token == null || token.isEmpty) {
      throw StateError('You must be signed in to access the calendar.');
    }
    return Options(
      headers: <String, dynamic>{
        'Authorization': 'Bearer $token',
      },
    );
  }
}
