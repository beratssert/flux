import 'dart:convert';
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

  Future<List<TimeEntry>> getTimeEntries(DateTime from, DateTime to) async {
    try {
      final response = await _dio.get('/api/v1/TimeEntries', queryParameters: {
        'from': from.toUtc().toIso8601String(),
        'to': to.toUtc().toIso8601String(),
        'StartDate':
            from.toUtc().toIso8601String(), // İki ihtimali de yollayalım
        'EndDate': to.toUtc().toIso8601String(),
        'PageSize': 1000,
        'PageNumber': 1,
        'pageSize': 1000,
        'pageNumber': 1,
      });

      if (response.statusCode == 200) {
        final responseData = (response.data is String)
            ? jsonDecode(response.data)
            : response.data;

        // Backend "items" veya "data" veya "Data" olarak dönüyor olabilir
        final items = responseData['data'] ??
            responseData['Data'] ??
            responseData['items'] ??
            responseData['Items'];

        print("Backend'den Dönen Ham Veri (TimeEntries): $responseData");

        if (items != null && items is List) {
          print("JSON İçindeki Liste Uzunluğu: ${items.length}");
          return items.map((e) {
            print("Mapping Item: $e");
            return TimeEntry.fromJson(e as Map<String, dynamic>);
          }).toList();
        } else {
          print(
              "HATA: Data içi liste olarak bulunamadı. Gelen format: ${responseData.runtimeType}");
        }
      }
      return [];
    } catch (e, stack) {
      print("Takvim verisi çekerken JSON parse hatası: $e");
      print(stack);
      return [];
    }
  }

  Future<({bool success, String? errorMessage})> createTimeEntry({
    required String projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    try {
      final startTime = date;
      final endTime = startTime.add(Duration(minutes: duration));

      final response = await _dio.post('/api/v1/TimeEntries', data: {
        'projectId': int.tryParse(projectId) ?? 0,
        'entryDate': startTime.toIso8601String(),
        'startTimeUtc': startTime.toUtc().toIso8601String(),
        'endTimeUtc': endTime.toUtc().toIso8601String(),
        'durationMinutes': duration,
        'description': description,
        'isBillable': true,
      });
      return (
        success: response.statusCode == 200 || response.statusCode == 201,
        errorMessage: null
      );
    } on DioException catch (e) {
      String? errorMsg;
      if (e.response?.data != null) {
        final data = e.response!.data;
        if (data is Map<String, dynamic>) {
          errorMsg = data['Message'] ??
              data['message'] ??
              data['detail'] ??
              data['title'] ??
              data.toString();

          // CleanArchitecture.WebApi genelde Errors array döner validation error olursa.
          if (data.containsKey('errors') &&
              data['errors'] is List &&
              (data['errors'] as List).isNotEmpty) {
            errorMsg = (data['errors'] as List).first.toString();
          }
        } else {
          errorMsg = data.toString();
        }
      } else {
        errorMsg = e.message;
      }
      return (
        success: false,
        errorMessage:
            errorMsg ?? "Bilinmeyen API hatası (Sunucu yanıt vermedi)."
      );
    } catch (e) {
      return (success: false, errorMessage: "Beklenmeyen hata: $e");
    }
  }

  Future<bool> updateTimeEntry({
    required String id,
    required String projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    try {
      final startTime = date;
      final endTime = startTime.add(Duration(minutes: duration));

      final response = await _dio.put('/api/v1/TimeEntries/$id', data: {
        'id': int.tryParse(id) ?? 0,
        'projectId': int.tryParse(projectId) ?? 0,
        'entryDate': startTime.toIso8601String(),
        'startTimeUtc': startTime.toUtc().toIso8601String(),
        'endTimeUtc': endTime.toUtc().toIso8601String(),
        'durationMinutes': duration,
        'description': description,
        'isBillable': true,
      });
      return response.statusCode == 200 || response.statusCode == 204;
    } on DioException catch (e) {
      print("Update hatası: ${e.response?.data}");
      return false;
    } catch (e) {
      return false;
    }
  }

  Future<bool> deleteTimeEntry(String id) async {
    try {
      final response = await _dio.delete('/api/v1/TimeEntries/$id');
      return response.statusCode == 200 || response.statusCode == 204;
    } on DioException catch (e) {
      print("Delete hatası: ${e.response?.data}");
      return false;
    } catch (e) {
      print("Delete istisna: $e");
      return false;
    }
  }
}
