import 'package:dio/dio.dart';
import '../models/time_entry_model.dart';

class CalendarService {
  // Test ve Test Senaryosu: Bu kısmı login ekranı tamamlanana kadar kullanacağız.
  // Lütfen Swagger API'den aldığınız 2 gerçek token'ı buraya yapıştırın:
  static const String _employeeToken =
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlbXBsb3llZSIsImp0aSI6ImY5MmI4MTQyLThhN2MtNDcyZS1iOTQwLTI3ZDdiYWE5YmI5ZiIsImVtYWlsIjoiZW1wbG95ZWVAZmx1eC5sb2NhbCIsInVpZCI6ImE1YjUzM2RjLTdmZDEtNGU1Yi05NzA1LTdkYzU3YzFjN2MwYiIsImlwIjoiMTcyLjE5LjAuMyIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkVtcGxveWVlIiwiZXhwIjoxNzc1NTQ2NDkwLCJpc3MiOiJDb3JlSWRlbnRpdHkiLCJhdWQiOiJDb3JlSWRlbnRpdHlVc2VyIn0.cH9TPHGmWkEUXLsA8tZbTDOEINWJxTO5h0stc3-cWYg";
  static const String _managerToken =
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtYW5hZ2VyIiwianRpIjoiY2JhM2I5MWYtNzJmMi00MzJjLTgxNDAtZWVjZjUxMDBkZmUwIiwiZW1haWwiOiJtYW5hZ2VyQGZsdXgubG9jYWwiLCJ1aWQiOiI0YTI4NjljMi1jM2EzLTQwZjgtYTJiYy1kMTdmYjlkZTNmMjEiLCJpcCI6IjE3Mi4xOS4wLjQiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJNYW5hZ2VyIiwiZXhwIjoxNzc1NjEyNjQ0LCJpc3MiOiJDb3JlSWRlbnRpdHkiLCJhdWQiOiJDb3JlSWRlbnRpdHlVc2VyIn0.KXt0MKPozCKkiUOgEGt8nbG9jfqtSuK-tjl4pbyPtJI";

  // TEST İÇİN DEĞİŞTİR: true yaparsan uygulama Manager gibi davranır (ve /team uçlarını çeker)
  // false yaparsan Employee gibi davranır (sadece kendi TimeEntries uçlarını çeker).
  final bool _isTestingManager = false;

  late final Dio _dio;

  CalendarService() {
    _dio = Dio(BaseOptions(
      baseUrl: 'http://localhost:5001/api/v1',
      headers: {
        'Authorization':
            'Bearer ${getTempToken()}', // Dinamik test token seçimi
        'Accept': 'application/json',
      },
    ));
  }

  String getTempToken() => _isTestingManager ? _managerToken : _employeeToken;

  Future<List<TimeEntry>> getTeamTimeEntries(DateTime from, DateTime to) async {
    try {
      final response = await _dio.get('/TimeEntries/team', queryParameters: {
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
      print("Sunucu Hatası (Team): ${e.response?.data}");
      return [];
    }
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
