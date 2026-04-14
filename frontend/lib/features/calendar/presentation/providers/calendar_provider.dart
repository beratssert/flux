import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/time_entry_model.dart';
import '../../data/services/calendar_service.dart';
import '../../../auth/data/auth_session_controller.dart';

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

  CalendarNotifier(this._service,
      {required String role, required String userId})
      : super(CalendarState(role: role, userId: userId)) {
    _initAndFetch();
  }

  Future<void> _initAndFetch() async {
    state = state.copyWith(isLoading: true);

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
      // API dokümantasyonu gereği "GET /api/v1/time-entries" ucu, manager için hem kendi hem de takımının time entry'lerini döner.
      final data = await _service.getTimeEntries(from, to);
      state = state.copyWith(entries: data, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false);
    }
  }

  Future<({bool success, String? errorMessage})> addEvent(
      String projectId, String description, int duration, DateTime date) async {
    final result = await _service.createTimeEntry(
      projectId: projectId,
      description: description,
      duration: duration,
      date: date,
    );
    if (result.success) {
      // Yenile, ancak loading spinneri çıkarma
      await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
          DateTime.now().add(const Duration(days: 30)),
          silent: true);
    }
    return result;
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

  Future<bool> deleteEvent(String id) async {
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
  final authState = ref.watch(authSessionControllerProvider);
  final profile = authState.session?.profile;

  return CalendarNotifier(
    service,
    role: profile?.role ?? 'Employee',
    userId: profile?.id ?? '',
  );
});
