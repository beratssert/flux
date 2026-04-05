import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/calendar_provider.dart';
import '../../data/models/time_entry_model.dart';

class TaskDetailsDialog extends ConsumerWidget {
  final TimeEntry entry;
  final String role;
  final String userId;

  const TaskDetailsDialog({
    Key? key,
    required this.entry,
    required this.role,
    required this.userId,
  }) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return AlertDialog(
      title: const Text("Görev Detayı"),
      content: Text(
          "Görev: ${entry.description}\nSüre: ${entry.durationMinutes} dakika"),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text("Kapat")),
        if (entry.userId == userId ||
            userId.isEmpty) // Sadece kendi göreviyse sil
          TextButton(
            onPressed: () {
              ref.read(calendarNotifierProvider.notifier).deleteEvent(entry.id);
              Navigator.pop(context);
            },
            child: const Text("Sil", style: TextStyle(color: Colors.red)),
          ),
      ],
    );
  }
}
