import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/calendar_provider.dart';

class AddTaskDialog extends ConsumerStatefulWidget {
  final DateTime selectedDate;

  const AddTaskDialog({Key? key, required this.selectedDate}) : super(key: key);

  @override
  _AddTaskDialogState createState() => _AddTaskDialogState();
}

class _AddTaskDialogState extends ConsumerState<AddTaskDialog> {
  final descCtrl = TextEditingController();
  final durCtrl = TextEditingController();
  final projCtrl = TextEditingController();
  late TimeOfDay _selectedTime;

  @override
  void initState() {
    super.initState();
    _selectedTime = TimeOfDay.fromDateTime(widget.selectedDate);
  }

  @override
  void dispose() {
    descCtrl.dispose();
    durCtrl.dispose();
    projCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text("Yeni Görev Ekle"),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              title: Text("Saat: ${_selectedTime.format(context)}"),
              trailing: const Icon(Icons.access_time),
              contentPadding: EdgeInsets.zero,
              onTap: () async {
                final TimeOfDay? picked = await showTimePicker(
                  context: context,
                  initialTime: _selectedTime,
                );
                if (picked != null) {
                  setState(() {
                    _selectedTime = picked;
                  });
                }
              },
            ),
            TextField(
                controller: descCtrl,
                decoration: const InputDecoration(labelText: "Açıklama")),
            TextField(
                controller: durCtrl,
                decoration: const InputDecoration(labelText: "Süre (Dakika)"),
                keyboardType: TextInputType.number),
            TextField(
                controller: projCtrl,
                decoration: const InputDecoration(labelText: "Proje ID"),
                keyboardType: TextInputType.number),
          ],
        ),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text("İptal")),
        ElevatedButton(
          onPressed: () async {
            final dur = int.tryParse(durCtrl.text) ?? 60;
            final proj = projCtrl.text.isEmpty ? "1" : projCtrl.text;

            final finalDateTime = DateTime(
              widget.selectedDate.year,
              widget.selectedDate.month,
              widget.selectedDate.day,
              _selectedTime.hour,
              _selectedTime.minute,
            );

            final result = await ref
                .read(calendarNotifierProvider.notifier)
                .addEvent(proj, descCtrl.text, dur, finalDateTime);

            if (!context.mounted) return;

            if (result.success) {
              Navigator.pop(context);
            } else {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    (result.errorMessage != null &&
                            result.errorMessage!.isNotEmpty)
                        ? result.errorMessage!
                        : "Görev eklenirken bir hata oluştu veya yetkiniz yok.",
                  ),
                  backgroundColor: Colors.red,
                  duration: const Duration(seconds: 4),
                ),
              );
            }
          },
          child: const Text("Kaydet"),
        ),
      ],
    );
  }
}
