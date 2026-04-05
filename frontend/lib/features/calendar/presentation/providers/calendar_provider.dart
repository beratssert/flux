import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:jwt_decoder/jwt_decoder.dart';
import '../../data/models/time_entry_model.dart';
import '../../data/services/calendar_service.dart';

// Service Provider
final calendarServiceProvider = Provider<CalendarService>((ref) {
  return CalendarService();
});

// State for Calendar
class CalendarState {
  final List<TimeEntry> entries;
  final bool isLoading;
  final String role;
  final String userId;

  CalendarState({
    this.entries = const [],
    this.isLoading = false,
    this.role = 'Employee',
    this.userId = '',
  });

  CalendarState copyWith({
    List<TimeEntry>? entries,
    bool? isLoading,
    String? role,
    String? userId,
  }) {
    return CalendarState(
      entries: entries ?? this.entries,
      isLoading: isLoading ?? this.isLoading,
      role: role ?? this.role,
      userId: userId ?? this.userId,
    );
  }
}

// Notifier
class CalendarNotifier extends StateNotifier<CalendarState> {
  final CalendarService _service;

  CalendarNotifier(this._service) : super(CalendarState()) {
    _initAndFetch();
  }

  Future<void> _initAndFetch() async {
    // 1. Token'dan Rolü Oku
    final token = _service.getTempToken();
    Map<String, dynamic> decodedToken = JwtDecoder.decode(token);

    // Rol claim'ini bul (Genelde standart JWT veya Microsoft claim'i olur)
    String role = 'Employee';
    const roleClaim =
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    if (decodedToken.containsKey(roleClaim)) {
      role = decodedToken[roleClaim];
    } else if (decodedToken.containsKey('role')) {
      role = decodedToken['role'];
    }

    String userId = '';
    if (decodedToken.containsKey('uid')) {
      userId = decodedToken['uid'];
    } else if (decodedToken.containsKey('sub')) {
      userId = decodedToken['sub'];
    }

    state = state.copyWith(role: role, userId: userId, isLoading: true);

    // 2. Rol bazlı veri çek
    await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
        DateTime.now().add(const Duration(days: 30)));
  }

  Future<void> fetchEvents(DateTime from, DateTime to,
      {bool silent = false}) async {
    if (!silent) {
      state = state.copyWith(isLoading: true);
    }
    try {
      List<TimeEntry> data;
      if (state.role == 'Manager') {
        data = await _service.getTeamTimeEntries(from, to);
      } else {
        data = await _service.getTimeEntries(from, to); // Employee (kendisi)
      }
      state = state.copyWith(entries: data, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false);
    }
  }

  Future<bool> addEvent(
      int projectId, String description, int duration, DateTime date) async {
    final success = await _service.createTimeEntry(
      projectId: projectId,
      description: description,
      duration: duration,
      date: date,
    );
    if (success) {
      // Yenile, ancak loading spinneri çıkarma
      await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
          DateTime.now().add(const Duration(days: 30)),
          silent: true);
    }
    return success;
  }

  Future<bool> updateEventTime(TimeEntry entry, DateTime newStartTime) async {
    // İyimser (Optimistic) Güncelleme: UI anında yenilensin (göz kırpma olmasın)
    final optimisticEntries = state.entries.map((e) {
      if (e.id == entry.id) {
        return TimeEntry(
          id: e.id,
          userId: e.userId,
          projectId: e.projectId,
          description: e.description,
          startTime: newStartTime,
          endTime: newStartTime.add(Duration(minutes: e.durationMinutes)),
          durationMinutes: e.durationMinutes,
        );
      }
      return e;
    }).toList();
    state = state.copyWith(entries: optimisticEntries);

    final success = await _service.updateTimeEntry(
      id: entry.id,
      projectId: entry.projectId,
      description: entry.description,
      duration: entry.durationMinutes,
      date: newStartTime,
    );
    if (success) {
      await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
          DateTime.now().add(const Duration(days: 30)),
          silent: true);
    }
    return success;
  }

  Future<bool> updateEventTimeAndDuration(
      TimeEntry entry, DateTime newStartTime, int newDurationMinutes) async {
    // İyimser Güncelleme
    final optimisticEntries = state.entries.map((e) {
      if (e.id == entry.id) {
        return TimeEntry(
          id: e.id,
          userId: e.userId,
          projectId: e.projectId,
          description: e.description,
          startTime: newStartTime,
          endTime: newStartTime.add(Duration(minutes: newDurationMinutes)),
          durationMinutes: newDurationMinutes,
        );
      }
      return e;
    }).toList();
    state = state.copyWith(entries: optimisticEntries);

    final success = await _service.updateTimeEntry(
      id: entry.id,
      projectId: entry.projectId,
      description: entry.description,
      duration: newDurationMinutes,
      date: newStartTime,
    );
    if (success) {
      await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
          DateTime.now().add(const Duration(days: 30)),
          silent: true);
    }
    return success;
  }

  Future<bool> deleteEvent(int id) async {
    // İyimser Güncelleme: Listeden anında çıkar (göz kırpma olmasın)
    final optimisticEntries = state.entries.where((e) => e.id != id).toList();
    state = state.copyWith(entries: optimisticEntries);

    final success = await _service.deleteTimeEntry(id);
    if (success) {
      await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
          DateTime.now().add(const Duration(days: 30)),
          silent: true);
    }
    return success;
  }
}

final calendarNotifierProvider =
    StateNotifierProvider<CalendarNotifier, CalendarState>((ref) {
  final service = ref.watch(calendarServiceProvider);
  return CalendarNotifier(service);
});
