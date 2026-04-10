import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:syncfusion_flutter_calendar/calendar.dart';
import '../providers/calendar_provider.dart';
import '../../data/models/time_entry_model.dart';
import '../widgets/add_task_dialog.dart';
import '../widgets/task_details_dialog.dart';

class CalendarPage extends ConsumerStatefulWidget {
  const CalendarPage({Key? key}) : super(key: key);

  @override
  _CalendarPageState createState() => _CalendarPageState();
}

class _CalendarPageState extends ConsumerState<CalendarPage> {
  final CalendarController _calendarController = CalendarController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(calendarNotifierProvider.notifier).fetchEvents(
            DateTime.now().subtract(const Duration(days: 30)),
            DateTime.now().add(const Duration(days: 30)),
          );
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(calendarNotifierProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Flux Calendar'),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16.0),
            child: Center(
              child: Text(
                'Role: ${state.role}',
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
          )
        ],
      ),
      body: state.isLoading
          ? const Center(child: CircularProgressIndicator())
          : SfCalendar(
              controller: _calendarController,
              view: CalendarView.day,
              allowedViews: const [
                CalendarView.day,
                CalendarView.week,
                CalendarView.workWeek,
                CalendarView.month,
              ],
              timeSlotViewSettings: const TimeSlotViewSettings(
                timeIntervalHeight: 80,
                minimumAppointmentDuration: Duration(minutes: 15),
              ),
              allowDragAndDrop: true,
              onDragEnd: (AppointmentDragEndDetails details) {
                if (details.appointment != null &&
                    details.droppingTime != null) {
                  final Appointment app = details.appointment! as Appointment;
                  final TimeEntry entry = app.id as TimeEntry;
                  final DateTime newStartTime = details.droppingTime!;

                  // Update via Riverpod
                  ref
                      .read(calendarNotifierProvider.notifier)
                      .updateEventTime(entry, newStartTime);
                }
              },
              allowAppointmentResize: true,
              onAppointmentResizeEnd: (AppointmentResizeEndDetails details) {
                if (details.appointment != null &&
                    details.startTime != null &&
                    details.endTime != null) {
                  final Appointment app = details.appointment! as Appointment;
                  final TimeEntry entry = app.id as TimeEntry;
                  final DateTime newStartTime = details.startTime!;
                  final int newDuration =
                      details.endTime!.difference(details.startTime!).inMinutes;

                  // Update via Riverpod
                  ref
                      .read(calendarNotifierProvider.notifier)
                      .updateEventTimeAndDuration(
                          entry, newStartTime, newDuration);
                }
              },
              dataSource: TimeEntryDataSource(state.entries),
              onTap: (CalendarTapDetails details) {
                if (details.targetElement == CalendarElement.appointment) {
                  final TimeEntry entry =
                      details.appointments!.first.id as TimeEntry;
                  showDialog(
                    context: context,
                    builder: (ctx) => TaskDetailsDialog(
                        entry: entry, role: state.role, userId: state.userId),
                  );
                }
              },
              appointmentBuilder: (context, calendarAppointmentDetails) {
                final appointment =
                    calendarAppointmentDetails.appointments.first;

                return Container(
                  decoration: BoxDecoration(
                    color: appointment.color,
                    borderRadius: BorderRadius.circular(4),
                  ),
                  padding: const EdgeInsets.all(4),
                  child: Text(
                    appointment.subject,
                    style: const TextStyle(color: Colors.white, fontSize: 12),
                    overflow: TextOverflow.ellipsis,
                  ),
                );
              },
            ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          final date = _calendarController.selectedDate ?? DateTime.now();
          showDialog(
            context: context,
            builder: (ctx) => AddTaskDialog(selectedDate: date),
          );
        },
        backgroundColor: Colors.deepPurple,
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }
}

// ---------------------------------------------------------
// Syncfusion Calendar Data Source
// ---------------------------------------------------------
class TimeEntryDataSource extends CalendarDataSource {
  TimeEntryDataSource(List<TimeEntry> source) {
    appointments = source.map((entry) {
      return Appointment(
        startTime: entry.startTime,
        endTime: entry.endTime ??
            entry.startTime.add(Duration(minutes: entry.durationMinutes)),
        subject: entry.description,
        color: _getColorForEntry(entry),
        id: entry, // ID'ye asıl Entry objesini gömüyoruz, kolay erişim için
      );
    }).toList();
  }

  Color _getColorForEntry(TimeEntry entry) {
    // Manager view the team with different colors based on userId/projectId if data is available
    // For now we do a simple hash
    final colors = [
      Colors.deepPurple,
      Colors.blueAccent,
      Colors.teal,
      Colors.orange
    ];
    return colors[(entry.id.hashCode).abs() % colors.length];
  }
}
