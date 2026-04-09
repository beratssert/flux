import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/api_error_message.dart';
import '../data/calendar_models.dart';
import '../data/calendar_provider.dart';

class CalendarPage extends ConsumerWidget {
  const CalendarPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(calendarProvider);
    final notifier = ref.read(calendarProvider.notifier);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Calendar'),
        backgroundColor: const Color(0xFF0D5EF8),
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      body: Column(
        children: [
          _CalendarHeader(state: state, notifier: notifier),
          _CalendarGrid(state: state, notifier: notifier),
          const Divider(height: 1),
          Expanded(
            child: _DayDetail(state: state),
          ),
        ],
      ),
    );
  }
}

// ─── Month header ─────────────────────────────────────────────────────────────

class _CalendarHeader extends StatelessWidget {
  const _CalendarHeader({required this.state, required this.notifier});

  final CalendarState state;
  final CalendarNotifier notifier;

  @override
  Widget build(BuildContext context) {
    final month = state.focusedMonth ?? DateTime.now();
    final label =
        '${_monthName(month.month)} ${month.year}';

    return Container(
      color: const Color(0xFF0D5EF8),
      padding: const EdgeInsets.fromLTRB(8, 0, 8, 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left, color: Colors.white),
            onPressed: notifier.previousMonth,
          ),
          Text(
            label,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 18,
              fontWeight: FontWeight.w600,
            ),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right, color: Colors.white),
            onPressed: notifier.nextMonth,
          ),
        ],
      ),
    );
  }

  static String _monthName(int month) {
    const names = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December',
    ];
    return names[month - 1];
  }
}

// ─── Month grid ───────────────────────────────────────────────────────────────

class _CalendarGrid extends StatelessWidget {
  const _CalendarGrid({required this.state, required this.notifier});

  final CalendarState state;
  final CalendarNotifier notifier;

  @override
  Widget build(BuildContext context) {
    final month = state.focusedMonth ?? DateTime.now();
    final firstDay = DateTime(month.year, month.month, 1);
    // Monday-based: 0=Mon … 6=Sun
    final startOffset = (firstDay.weekday - 1) % 7;
    final daysInMonth = DateUtils.getDaysInMonth(month.year, month.month);
    final today = DateTime.now();

    return Container(
      color: const Color(0xFF0D5EF8),
      padding: const EdgeInsets.fromLTRB(8, 0, 8, 8),
      child: Column(
        children: [
          // Day-of-week labels
          Row(
            children: const ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su']
                .map(
                  (d) => Expanded(
                    child: Center(
                      child: Text(
                        d,
                        style: const TextStyle(
                          color: Colors.white70,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                )
                .toList(),
          ),
          const SizedBox(height: 4),
          if (state.isLoading)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: CircularProgressIndicator(
                  valueColor: AlwaysStoppedAnimation<Color>(Colors.white)),
            )
          else
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 7,
                childAspectRatio: 1.0,
              ),
              itemCount: startOffset + daysInMonth,
              itemBuilder: (context, index) {
                if (index < startOffset) return const SizedBox.shrink();

                final day = index - startOffset + 1;
                final date = DateTime(month.year, month.month, day);
                final isToday = date.year == today.year &&
                    date.month == today.month &&
                    date.day == today.day;
                final isSelected = state.selectedDay != null &&
                    date.year == state.selectedDay!.year &&
                    date.month == state.selectedDay!.month &&
                    date.day == state.selectedDay!.day;
                final hasItems = state.hasItemsOnDay(date);

                return GestureDetector(
                  onTap: () => notifier.selectDay(date),
                  child: _DayCell(
                    day: day,
                    isToday: isToday,
                    isSelected: isSelected,
                    hasItems: hasItems,
                  ),
                );
              },
            ),
        ],
      ),
    );
  }
}

class _DayCell extends StatelessWidget {
  const _DayCell({
    required this.day,
    required this.isToday,
    required this.isSelected,
    required this.hasItems,
  });

  final int day;
  final bool isToday;
  final bool isSelected;
  final bool hasItems;

  @override
  Widget build(BuildContext context) {
    Color? bg;
    Color textColor = Colors.white;

    if (isSelected) {
      bg = Colors.white;
      textColor = const Color(0xFF0D5EF8);
    } else if (isToday) {
      bg = Colors.white.withOpacity(0.3);
    }

    return Container(
      margin: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        color: bg,
        shape: BoxShape.circle,
      ),
      child: Stack(
        alignment: Alignment.center,
        children: [
          Text(
            '$day',
            style: TextStyle(
              color: textColor,
              fontWeight: isToday || isSelected
                  ? FontWeight.w700
                  : FontWeight.w400,
              fontSize: 14,
            ),
          ),
          if (hasItems)
            Positioned(
              bottom: 4,
              child: Container(
                width: 4,
                height: 4,
                decoration: BoxDecoration(
                  color: isSelected
                      ? const Color(0xFF0D5EF8)
                      : Colors.white,
                  shape: BoxShape.circle,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

// ─── Day detail ───────────────────────────────────────────────────────────────

class _DayDetail extends StatelessWidget {
  const _DayDetail({required this.state});

  final CalendarState state;

  @override
  Widget build(BuildContext context) {
    if (state.error != null) {
      return Padding(
        padding: const EdgeInsets.all(24),
        child: Text(
          describeApiError(state.error!),
          style: const TextStyle(color: Colors.red),
          textAlign: TextAlign.center,
        ),
      );
    }

    final selected = state.selectedDay;
    if (selected == null) {
      return const Center(child: Text('Select a day to view entries'));
    }

    final items = state.itemsForDay(selected);

    if (items.isEmpty) {
      return Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            _DayTitle(day: selected),
            const SizedBox(height: 16),
            const Text(
              'No entries for this day.',
              style: TextStyle(color: Colors.grey),
            ),
          ],
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: _DayTitle(day: selected),
        ),
        Expanded(
          child: ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            itemCount: items.length,
            separatorBuilder: (_, __) => const SizedBox(height: 8),
            itemBuilder: (context, index) =>
                _CalendarItemCard(item: items[index]),
          ),
        ),
      ],
    );
  }
}

class _DayTitle extends StatelessWidget {
  const _DayTitle({required this.day});

  final DateTime day;

  @override
  Widget build(BuildContext context) {
    final weekday = _weekdayName(day.weekday);
    final label = '$weekday, ${day.day} ${_monthName(day.month)} ${day.year}';
    return Text(
      label,
      style: Theme.of(context)
          .textTheme
          .titleMedium
          ?.copyWith(fontWeight: FontWeight.w700),
    );
  }

  static String _weekdayName(int wd) {
    const n = [
      'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday',
    ];
    return n[wd - 1];
  }

  static String _monthName(int m) {
    const n = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return n[m - 1];
  }
}

class _CalendarItemCard extends StatelessWidget {
  const _CalendarItemCard({required this.item});

  final CalendarItemRecord item;

  @override
  Widget build(BuildContext context) {
    final isEntry = item.isTimeEntry;
    final color = isEntry ? const Color(0xFF34A853) : const Color(0xFF0D5EF8);
    final icon = isEntry ? Icons.access_time_rounded : Icons.event_rounded;

    final timeLabel = _buildTimeLabel(item);

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 4,
              height: 48,
              decoration: BoxDecoration(
                color: color,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(width: 12),
            Icon(icon, color: color, size: 20),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    style: const TextStyle(
                      fontWeight: FontWeight.w600,
                      fontSize: 14,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (timeLabel.isNotEmpty) ...[
                    const SizedBox(height: 2),
                    Text(
                      timeLabel,
                      style: const TextStyle(
                        color: Colors.grey,
                        fontSize: 12,
                      ),
                    ),
                  ],
                  if (item.projectId != null) ...[
                    const SizedBox(height: 2),
                    Text(
                      'Project #${item.projectId}',
                      style: TextStyle(
                        color: color.withOpacity(0.8),
                        fontSize: 12,
                      ),
                    ),
                  ],
                ],
              ),
            ),
            if (item.isBillable == true)
              Padding(
                padding: const EdgeInsets.only(left: 4),
                child: Tooltip(
                  message: 'Billable',
                  child: Icon(Icons.attach_money_rounded,
                      size: 16, color: Colors.amber[700]),
                ),
              ),
          ],
        ),
      ),
    );
  }

  String _buildTimeLabel(CalendarItemRecord item) {
    if (item.isTimeEntry && item.durationMinutes != null) {
      final h = item.durationMinutes! ~/ 60;
      final m = item.durationMinutes! % 60;
      if (h > 0 && m > 0) return '${h}h ${m}m';
      if (h > 0) return '${h}h';
      return '${m}m';
    }

    if (!item.allDay) {
      final s = item.startUtc.toLocal();
      final e = item.endUtc.toLocal();
      return '${_fmt(s)} – ${_fmt(e)}';
    }

    return 'All day';
  }

  String _fmt(DateTime dt) {
    final h = dt.hour.toString().padLeft(2, '0');
    final m = dt.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }
}
