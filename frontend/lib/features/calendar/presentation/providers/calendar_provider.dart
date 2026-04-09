import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/time_entry_model.dart';
import '../../data/services/calendar_service.dart';
import '../../../auth/data/auth_session_controller.dart';

// State for Calendar
class CalendarState {
  final String role;
  final String userId;
  final bool isLoading;
  final List<TimeEntry> entries;
  final String? error;

  const CalendarState({
    this.role = 'Employee',
    this.userId = '',
    this.isLoading = false,
    this.entries = const [],
    this.error,
  });

  CalendarState copyWith({
    String? role,
    String? userId,
    bool? isLoading,
    List<TimeEntry>? entries,
    String? error,
  }) {
    return CalendarState(
      role: role ?? this.role,
      userId: userId ?? this.userId,
      isLoading: isLoading ?? this.isLoading,
      entries: entries ?? this.entries,
      error: error,
    );
  }
}

class CalendarNotifier extends StateNotifier<CalendarState> {
  final CalendarService _service;

  CalendarNotifier(this._service,
      {required String role, required String userId})
      : super(CalendarState(role: role, userId: userId)) {
    _initAndFetch();
  }

  Future<void> _initAndFetch() async {
    state = state.copyWith(isLoading: true);
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
        // Manager: fetches all time entries for their team
        data = await _service.getTeamTimeEntries(from, to);
      } else {
        // Employee: fetches only their own time entries
        data = await _service.getTimeEntries(from, to);
      }
      state = state.copyWith(entries: data, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  Future<bool> addEntry({
    required int projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    final success = await _service.addTimeEntry(
      projectId: projectId,
      description: description,
      duration: duration,
      date: date,
    );
    if (success) {
      await _refreshCurrentRange();
    }
    return success;
  }

  Future<bool> updateEntry({
    required int id,
    required int projectId,
    required String description,
    required int duration,
    required DateTime date,
  }) async {
    final success = await _service.updateTimeEntry(
      id: id,
      projectId: projectId,
      description: description,
      duration: duration,
      date: date,
    );
    if (success) {
      await _refreshCurrentRange();
    }
    return success;
  }

  Future<bool> deleteEntry(int id) async {
    final success = await _service.deleteTimeEntry(id);
    if (success) {
      state = state.copyWith(
        entries: state.entries.where((e) => e.id != id).toList(),
      );
    }
    return success;
  }

  Future<void> _refreshCurrentRange() async {
    await fetchEvents(
      DateTime.now().subtract(const Duration(days: 30)),
      DateTime.now().add(const Duration(days: 30)),
      silent: true,
    );
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
