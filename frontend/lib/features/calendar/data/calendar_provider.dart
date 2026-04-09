import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'calendar_api_client.dart';
import 'calendar_models.dart';

// ─── State ────────────────────────────────────────────────────────────────────

class CalendarState {
  const CalendarState({
    required this.focusedMonth,
    required this.selectedDay,
    required this.items,
    required this.isLoading,
    this.error,
  });

  const CalendarState.initial()
      : focusedMonth = null,
        selectedDay = null,
        items = const <CalendarItemRecord>[],
        isLoading = false,
        error = null;

  final DateTime? focusedMonth;
  final DateTime? selectedDay;
  final List<CalendarItemRecord> items;
  final bool isLoading;
  final String? error;

  List<CalendarItemRecord> itemsForDay(DateTime day) {
    final d = DateTime(day.year, day.month, day.day);
    return items.where((item) {
      final localStart = item.startUtc.toLocal();
      final localEnd = item.endUtc.toLocal();
      final startDay = DateTime(localStart.year, localStart.month, localStart.day);
      final endDay = DateTime(localEnd.year, localEnd.month, localEnd.day);
      return !d.isBefore(startDay) && !d.isAfter(endDay);
    }).toList();
  }

  bool hasItemsOnDay(DateTime day) => itemsForDay(day).isNotEmpty;

  CalendarState copyWith({
    DateTime? focusedMonth,
    DateTime? selectedDay,
    List<CalendarItemRecord>? items,
    bool? isLoading,
    String? error,
    bool clearError = false,
  }) {
    return CalendarState(
      focusedMonth: focusedMonth ?? this.focusedMonth,
      selectedDay: selectedDay ?? this.selectedDay,
      items: items ?? this.items,
      isLoading: isLoading ?? this.isLoading,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

// ─── Provider ─────────────────────────────────────────────────────────────────

final calendarProvider =
    StateNotifierProvider<CalendarNotifier, CalendarState>(
  (ref) => CalendarNotifier(ref),
);

class CalendarNotifier extends StateNotifier<CalendarState> {
  CalendarNotifier(this._ref) : super(const CalendarState.initial()) {
    final now = DateTime.now();
    final month = DateTime(now.year, now.month);
    state = state.copyWith(
      focusedMonth: month,
      selectedDay: DateTime(now.year, now.month, now.day),
    );
    loadMonth(month);
  }

  final Ref _ref;

  Future<void> loadMonth(DateTime month) async {
    state = state.copyWith(
      focusedMonth: month,
      isLoading: true,
      clearError: true,
    );
    try {
      final from = DateTime(month.year, month.month, 1);
      final to = DateTime(month.year, month.month + 1, 0); // last day of month
      final items = await _ref.read(calendarApiClientProvider).getCalendarItems(
            from: from,
            to: to,
          );
      state = state.copyWith(items: items, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  void selectDay(DateTime day) {
    state = state.copyWith(selectedDay: day);
  }

  void previousMonth() {
    final current = state.focusedMonth ?? DateTime.now();
    final prev = DateTime(current.year, current.month - 1);
    loadMonth(prev);
  }

  void nextMonth() {
    final current = state.focusedMonth ?? DateTime.now();
    final next = DateTime(current.year, current.month + 1);
    loadMonth(next);
  }
}
