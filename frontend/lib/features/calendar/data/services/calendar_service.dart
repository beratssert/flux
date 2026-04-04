import 'package:dio/dio.dart';
import '../models/time_entry_model.dart';

class CalendarService {
  // 1. Token'ı static veya normal bir değişken olarak tanımla
  final String _tempToken =
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlbXBsb3llZSIsImp0aSI6ImY5MmI4MTQyLThhN2MtNDcyZS1iOTQwLTI3ZDdiYWE5YmI5ZiIsImVtYWlsIjoiZW1wbG95ZWVAZmx1eC5sb2NhbCIsInVpZCI6ImE1YjUzM2RjLTdmZDEtNGU1Yi05NzA1LTdkYzU3YzFjN2MwYiIsImlwIjoiMTcyLjE5LjAuMyIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkVtcGxveWVlIiwiZXhwIjoxNzc1NTQ2NDkwLCJpc3MiOiJDb3JlSWRlbnRpdHkiLCJhdWQiOiJDb3JlSWRlbnRpdHlVc2VyIn0.cH9TPHGmWkEUXLsA8tZbTDOEINWJxTO5h0stc3-cWYg";

  late final Dio _dio;

  CalendarService() {
    // 2. Dio'yu constructor içinde initialize ediyoruz
    _dio = Dio(BaseOptions(
      baseUrl: 'http://localhost:5001/api/v1',
      headers: {
        'Authorization': 'Bearer $_tempToken', // Artık hata vermez
        'Accept': 'application/json',
      },
    ));
  }

  Future<List<TimeEntry>> getTimeEntries(DateTime from, DateTime to) async {
    try {
      final response = await _dio.get('/TimeEntries', queryParameters: {
        'From': from.toIso8601String(),
        'To': to.toIso8601String(),
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
    } on DioException catch (e) {
      print("Sunucu Hatası: ${e.response?.data}");
      return [];
    }
  }

  Future<bool> createTimeEntry({
    required int projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    try {
      // Başlangıç saati: UI'dan gelen 'date' değerini (saat dahil) kullanıyoruz
      final startTime = date;
      // Bitiş saati: startTime + duration (dakika cinsinden)
      final endTime = startTime.add(Duration(minutes: duration));

      final response = await _dio.post('/TimeEntries', data: {
        'projectId': projectId,
        'entryDate': startTime.toIso8601String(),
        'startTimeUtc': startTime.toUtc().toIso8601String(),
        'endTimeUtc': endTime.toUtc().toIso8601String(),
        'description': description,
        'isBillable': true,
      });
      return response.statusCode == 200 || response.statusCode == 201;
    } on DioException catch (e) {
      print('Görev ekleme hatası: ${e.response?.statusCode}');
      print('Hata detayı: ${e.response?.data}');
      return false;
    } catch (e) {
      print('Görev ekleme hatası: $e');
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

      final response = await _dio.put('/TimeEntries/$id', data: {
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
    } on DioException catch (e) {
      print('Görev güncelleme hatası: ${e.response?.statusCode}');
      print('Hata detayı: ${e.response?.data}');
      return false;
    } catch (e) {
      print('Görev güncelleme hatası: $e');
      return false;
    }
  }

  Future<bool> deleteTimeEntry(int id) async {
    try {
      final response = await _dio.delete('/TimeEntries/$id');
      return response.statusCode == 200 || response.statusCode == 204;
    } catch (e) {
      print('Görev silme hatası: $e');
      return false;
    }
  }
}
