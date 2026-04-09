import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/app_config.dart';
import '../../../auth/data/auth_session_controller.dart';
import '../models/time_entry_model.dart';

final calendarServiceProvider = Provider<CalendarService>((ref) {
  final authState = ref.watch(authSessionControllerProvider);
  final token = authState.session?.accessToken ?? '';

  final dio = Dio(
    BaseOptions(
      baseUrl: AppConfig.apiBaseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {
        'Accept': 'application/json',
        if (token.isNotEmpty) 'Authorization': 'Bearer $token',
      },
    ),
  );

  return CalendarService(dio);
});

class CalendarService {
  final Dio _dio;

  CalendarService(this._dio);

  // Employee: fetches their own time entries
  Future<List<TimeEntry>> getTimeEntries(DateTime from, DateTime to) async {
    try {
      final response = await _dio.get('/api/v1/TimeEntries', queryParameters: {
        'From': from.toUtc().toIso8601String(),
        'To': to.toUtc().toIso8601String(),
        'PageSize': 200,
      });

      if (response.statusCode == 200) {
        final items = response.data['items'];
        if (items != null && items is List) {
          return items
              .map((e) => TimeEntry.fromJson(e as Map<String, dynamic>))
              .toList();
        }
      }
      return [];
    } on DioException {
      return [];
    }
  }

  // Manager: fetches time entries for the team they manage
  Future<List<TimeEntry>> getTeamTimeEntries(DateTime from, DateTime to) async {
    try {
      final response =
          await _dio.get('/api/v1/TimeEntries/team', queryParameters: {
        'From': from.toUtc().toIso8601String(),
        'To': to.toUtc().toIso8601String(),
        'PageSize': 200,
      });

      if (response.statusCode == 200) {
        final items = response.data['items'];
        if (items != null && items is List) {
          return items
              .map((e) => TimeEntry.fromJson(e as Map<String, dynamic>))
              .toList();
        }
      }
      return [];
    } on DioException {
      return [];
    }
  }

  Future<bool> addTimeEntry({
    required int projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    try {
      final startTime = date;
      final endTime = startTime.add(Duration(minutes: duration));

      final response = await _dio.post('/api/v1/TimeEntries', data: {
        'projectId': projectId,
        'entryDate': startTime.toIso8601String(),
        'startTimeUtc': startTime.toUtc().toIso8601String(),
        'endTimeUtc': endTime.toUtc().toIso8601String(),
        'durationMinutes': duration,
        'description': description,
        'isBillable': true,
      });
      return response.statusCode == 200 || response.statusCode == 201;
    } on DioException {
      return false;
    } catch (e) {
      return false;
    }
  }

  Future<bool> updateTimeEntry({
    required int id,
    required int projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    try {
      final startTime = date;
      final endTime = startTime.add(Duration(minutes: duration));

      final response = await _dio.put('/api/v1/TimeEntries/$id', data: {
        'id': id,
        'projectId': projectId,
        'entryDate': startTime.toIso8601String(),
        'startTimeUtc': startTime.toUtc().toIso8601String(),
        'endTimeUtc': endTime.toUtc().toIso8601String(),
        'durationMinutes': duration,
        'description': description,
        'isBillable': true,
      });
      return response.statusCode == 200 || response.statusCode == 204;
    } on DioException {
      return false;
    } catch (e) {
      return false;
    }
  }

  Future<bool> deleteTimeEntry(int id) async {
    try {
      final response = await _dio.delete('/api/v1/TimeEntries/$id');
      return response.statusCode == 200 || response.statusCode == 204;
    } catch (e) {
      return false;
    }
  }
}
