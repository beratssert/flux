import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/time_entry_model.dart';
import '../providers/calendar_provider.dart';
import '../widgets/add_task_dialog.dart';
import '../widgets/task_details_dialog.dart'
    show TaskDetailsDialog, EntryAction, EntryActionType;

class CalendarPage extends ConsumerStatefulWidget {
  const CalendarPage({super.key});

  @override
  ConsumerState<CalendarPage> createState() => _CalendarPageState();
}

class _CalendarPageState extends ConsumerState<CalendarPage> {
  late DateTime _weekStart;

  @override
  void initState() {
    super.initState();
    _weekStart = _mondayOf(DateTime.now());
  }

  DateTime _mondayOf(DateTime date) {
    final d = DateTime(date.year, date.month, date.day);
    return d.subtract(Duration(days: d.weekday - 1));
  }

  void _previousWeek() {
    setState(() {
      _weekStart = _weekStart.subtract(const Duration(days: 7));
    });
    _fetchWeek();
  }

  void _nextWeek() {
    setState(() {
      _weekStart = _weekStart.add(const Duration(days: 7));
    });
    _fetchWeek();
  }

  void _fetchWeek() {
    final from = _weekStart;
    final to = _weekStart.add(const Duration(days: 7));
    ref
        .read(calendarNotifierProvider.notifier)
        .fetchEvents(from, to, silent: true);
  }

  List<TimeEntry> _entriesForDay(List<TimeEntry> all, DateTime day) {
    return all.where((e) {
      final d = e.startTime.toLocal();
      return d.year == day.year && d.month == day.month && d.day == day.day;
    }).toList()
      ..sort((a, b) => a.startTime.compareTo(b.startTime));
  }

  String _formatTime(DateTime dt) {
    final h = dt.hour.toString().padLeft(2, '0');
    final m = dt.minute.toString().padLeft(2, '0');
    return '$h:$m';
  }

  String _formatDuration(int minutes) {
    final h = minutes ~/ 60;
    final m = minutes % 60;
    if (h > 0 && m > 0) return '${h}h ${m}m';
    if (h > 0) return '${h}h';
    return '${m}m';
  }

  String _weekLabel() {
    final end = _weekStart.add(const Duration(days: 6));
    final months = [
      '',
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec'
    ];
    if (_weekStart.month == end.month) {
      return '${months[_weekStart.month]} ${_weekStart.day}–${end.day}, ${_weekStart.year}';
    }
    return '${months[_weekStart.month]} ${_weekStart.day} – ${months[end.month]} ${end.day}, ${_weekStart.year}';
  }

  String _dayLabel(DateTime day) {
    const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return '${days[day.weekday - 1]} ${day.day}';
  }

  bool _isToday(DateTime day) {
    final now = DateTime.now();
    return day.year == now.year && day.month == now.month && day.day == now.day;
  }

  @override
  Widget build(BuildContext context) {
    final calState = ref.watch(calendarNotifierProvider);
    final notifier = ref.read(calendarNotifierProvider.notifier);

    final days = List.generate(7, (i) => _weekStart.add(Duration(days: i)));

    return Scaffold(
      backgroundColor: const Color(0xFFF4F7FB),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        leading: const BackButton(color: Color(0xFF132039)),
        title: const Text(
          'Calendar',
          style: TextStyle(
            color: Color(0xFF132039),
            fontWeight: FontWeight.w700,
            fontSize: 20,
          ),
        ),
        actions: [
          if (calState.isLoading)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
        ],
      ),
      body: Column(
        children: [
          // Week navigation bar
          Container(
            color: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                IconButton(
                  icon: const Icon(Icons.chevron_left_rounded),
                  onPressed: _previousWeek,
                  color: const Color(0xFF132039),
                ),
                Expanded(
                  child: Text(
                    _weekLabel(),
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: Color(0xFF132039),
                      fontWeight: FontWeight.w700,
                      fontSize: 16,
                    ),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_right_rounded),
                  onPressed: _nextWeek,
                  color: const Color(0xFF132039),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          // Day column headers
          Container(
            color: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: days.map((day) {
                final isToday = _isToday(day);
                return Expanded(
                  child: Column(
                    children: [
                      Text(
                        _dayLabel(day),
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight:
                              isToday ? FontWeight.w700 : FontWeight.w500,
                          color: isToday
                              ? const Color(0xFF1E7BF2)
                              : const Color(0xFF61708C),
                        ),
                        textAlign: TextAlign.center,
                      ),
                      if (isToday)
                        Container(
                          margin: const EdgeInsets.only(top: 4),
                          width: 6,
                          height: 6,
                          decoration: const BoxDecoration(
                            color: Color(0xFF1E7BF2),
                            shape: BoxShape.circle,
                          ),
                        ),
                    ],
                  ),
                );
              }).toList(),
            ),
          ),
          const Divider(height: 1),
          // Entries list
          Expanded(
            child: calState.isLoading && calState.entries.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: days.length,
                    itemBuilder: (context, index) {
                      final day = days[index];
                      final entries =
                          _entriesForDay(calState.entries, day);
                      if (entries.isEmpty) return const SizedBox.shrink();

                      return Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            // Day header
                            Padding(
                              padding:
                                  const EdgeInsets.only(bottom: 8),
                              child: Row(
                                children: [
                                  if (_isToday(day))
                                    Container(
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 10, vertical: 4),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFF1E7BF2),
                                        borderRadius:
                                            BorderRadius.circular(20),
                                      ),
                                      child: Text(
                                        _dayLabel(day),
                                        style: const TextStyle(
                                          color: Colors.white,
                                          fontWeight: FontWeight.w700,
                                          fontSize: 13,
                                        ),
                                      ),
                                    )
                                  else
                                    Text(
                                      _dayLabel(day),
                                      style: const TextStyle(
                                        color: Color(0xFF132039),
                                        fontWeight: FontWeight.w700,
                                        fontSize: 15,
                                      ),
                                    ),
                                  const SizedBox(width: 8),
                                  Text(
                                    '${entries.length} entr${entries.length == 1 ? 'y' : 'ies'}',
                                    style: const TextStyle(
                                      color: Color(0xFF61708C),
                                      fontSize: 13,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            // Entry cards
                            ...entries.map(
                              (entry) => _EntryCard(
                                entry: entry,
                                formatTime: _formatTime,
                                formatDuration: _formatDuration,
                                onTap: () async {
                                  final result =
                                      await showDialog<EntryAction>(
                                    context: context,
                                    builder: (_) => TaskDetailsDialog(
                                      entry: entry,
                                    ),
                                  );
                                  if (result == null) return;
                                  if (result.type ==
                                      EntryActionType.delete) {
                                    await notifier
                                        .deleteEntry(entry.id);
                                  } else if (result.type ==
                                      EntryActionType.edit) {
                                    if (result.updated != null) {
                                      await notifier.updateEntry(
                                        id: entry.id,
                                        projectId:
                                            result.updated!.projectId,
                                        description:
                                            result.updated!.description,
                                        duration:
                                            result.updated!.durationMinutes,
                                        date: result.updated!.startTime,
                                      );
                                    }
                                  }
                                },
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: const Color(0xFF1E7BF2),
        onPressed: () async {
          final result = await showDialog<TimeEntry>(
            context: context,
            builder: (_) => AddTaskDialog(initialDate: DateTime.now()),
          );
          if (result != null) {
            await notifier.addEntry(
              projectId: result.projectId,
              description: result.description,
              duration: result.durationMinutes,
              date: result.startTime,
            );
          }
        },
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }
}

class _EntryCard extends StatelessWidget {
  const _EntryCard({
    required this.entry,
    required this.formatTime,
    required this.formatDuration,
    required this.onTap,
  });

  final TimeEntry entry;
  final String Function(DateTime) formatTime;
  final String Function(int) formatDuration;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final timeLabel = entry.endTime != null
        ? '${formatTime(entry.startTime)} – ${formatTime(entry.endTime!)}'
        : formatTime(entry.startTime);

    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFE8EDF4)),
        ),
        child: Row(
          children: [
            Container(
              width: 4,
              height: 40,
              decoration: BoxDecoration(
                color: const Color(0xFF1E7BF2),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    entry.description.isNotEmpty
                        ? entry.description
                        : 'No description',
                    style: const TextStyle(
                      color: Color(0xFF132039),
                      fontWeight: FontWeight.w600,
                      fontSize: 14,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      Text(
                        'Project #${entry.projectId}',
                        style: const TextStyle(
                          color: Color(0xFF61708C),
                          fontSize: 12,
                        ),
                      ),
                      const SizedBox(width: 8),
                      const Text(
                        '·',
                        style: TextStyle(color: Color(0xFF61708C)),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        timeLabel,
                        style: const TextStyle(
                          color: Color(0xFF61708C),
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            Text(
              formatDuration(entry.durationMinutes),
              style: const TextStyle(
                color: Color(0xFF1E7BF2),
                fontWeight: FontWeight.w700,
                fontSize: 13,
              ),
            ),
          ],
        ),
      ),
    );
  }
}


