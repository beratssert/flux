import 'package:flutter/material.dart';
import '../../data/models/time_entry_model.dart';

enum EntryActionType { edit, delete }

class EntryAction {
  final EntryActionType type;
  final TimeEntry? updated;
  const EntryAction({required this.type, this.updated});
}

class TaskDetailsDialog extends StatefulWidget {
  const TaskDetailsDialog({super.key, required this.entry});

  final TimeEntry entry;

  @override
  State<TaskDetailsDialog> createState() => _TaskDetailsDialogState();
}

class _TaskDetailsDialogState extends State<TaskDetailsDialog> {
  bool _editing = false;

  late TextEditingController _descriptionController;
  late TextEditingController _durationController;
  late DateTime _selectedDate;
  String? _error;

  @override
  void initState() {
    super.initState();
    _descriptionController =
        TextEditingController(text: widget.entry.description);
    _durationController = TextEditingController(
        text: widget.entry.durationMinutes.toString());
    _selectedDate = widget.entry.startTime;
  }

  @override
  void dispose() {
    _descriptionController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime(2020),
      lastDate: DateTime(2030),
    );
    if (picked != null) {
      setState(() {
        _selectedDate = DateTime(
          picked.year,
          picked.month,
          picked.day,
          _selectedDate.hour,
          _selectedDate.minute,
        );
      });
    }
  }

  Future<void> _pickTime() async {
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(_selectedDate),
    );
    if (picked != null) {
      setState(() {
        _selectedDate = DateTime(
          _selectedDate.year,
          _selectedDate.month,
          _selectedDate.day,
          picked.hour,
          picked.minute,
        );
      });
    }
  }

  String _formatDate(DateTime dt) {
    return '${dt.day.toString().padLeft(2, '0')}.'
        '${dt.month.toString().padLeft(2, '0')}.'
        '${dt.year}';
  }

  String _formatTime(DateTime dt) {
    return '${dt.hour.toString().padLeft(2, '0')}:'
        '${dt.minute.toString().padLeft(2, '0')}';
  }

  void _saveEdit() {
    final description = _descriptionController.text.trim();
    final durationStr = _durationController.text.trim();

    if (description.isEmpty) {
      setState(() => _error = 'Description is required.');
      return;
    }
    final duration = int.tryParse(durationStr);
    if (duration == null || duration <= 0) {
      setState(() => _error = 'Enter a valid duration (minutes).');
      return;
    }

    final updated = TimeEntry(
      id: widget.entry.id,
      userId: widget.entry.userId,
      projectId: widget.entry.projectId,
      description: description,
      startTime: _selectedDate,
      endTime: widget.entry.endTime,
      durationMinutes: duration,
    );

    Navigator.of(context)
        .pop(EntryAction(type: EntryActionType.edit, updated: updated));
  }

  void _confirmDelete() {
    showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete Entry'),
        content:
            const Text('Are you sure you want to delete this time entry?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Delete'),
          ),
        ],
      ),
    ).then((confirmed) {
      if (confirmed == true && mounted) {
        Navigator.of(context)
            .pop(EntryAction(type: EntryActionType.delete));
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      _editing ? 'Edit Entry' : 'Entry Details',
                      style: const TextStyle(
                        color: Color(0xFF132039),
                        fontWeight: FontWeight.w700,
                        fontSize: 18,
                      ),
                    ),
                  ),
                  if (!_editing)
                    IconButton(
                      icon: const Icon(Icons.edit_rounded,
                          color: Color(0xFF1E7BF2)),
                      onPressed: () => setState(() => _editing = true),
                      tooltip: 'Edit',
                    ),
                  if (!_editing)
                    IconButton(
                      icon: const Icon(Icons.delete_rounded,
                          color: Colors.red),
                      onPressed: _confirmDelete,
                      tooltip: 'Delete',
                    ),
                ],
              ),
              const SizedBox(height: 16),
              if (!_editing) ...[
                _DetailRow(
                  label: 'Project',
                  value: 'Project #${widget.entry.projectId}',
                ),
                _DetailRow(
                  label: 'Description',
                  value: widget.entry.description.isNotEmpty
                      ? widget.entry.description
                      : '—',
                ),
                _DetailRow(
                  label: 'Date',
                  value: _formatDate(widget.entry.startTime),
                ),
                _DetailRow(
                  label: 'Start Time',
                  value: _formatTime(widget.entry.startTime),
                ),
                if (widget.entry.endTime != null)
                  _DetailRow(
                    label: 'End Time',
                    value: _formatTime(widget.entry.endTime!),
                  ),
                _DetailRow(
                  label: 'Duration',
                  value: '${widget.entry.durationMinutes} min',
                ),
                const SizedBox(height: 16),
                SizedBox(
                  width: double.infinity,
                  child: OutlinedButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Close'),
                  ),
                ),
              ] else ...[
                TextField(
                  controller: _descriptionController,
                  decoration:
                      const InputDecoration(labelText: 'Description'),
                  maxLines: 2,
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _durationController,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                      labelText: 'Duration (minutes)'),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: _pickDate,
                        icon: const Icon(Icons.calendar_today_rounded,
                            size: 18),
                        label: Text(_formatDate(_selectedDate)),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: _pickTime,
                        icon: const Icon(Icons.access_time_rounded,
                            size: 18),
                        label: Text(_formatTime(_selectedDate)),
                      ),
                    ),
                  ],
                ),
                if (_error != null) ...[
                  const SizedBox(height: 10),
                  Text(
                    _error!,
                    style:
                        const TextStyle(color: Colors.red, fontSize: 13),
                  ),
                ],
                const SizedBox(height: 20),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () =>
                            setState(() => _editing = false),
                        child: const Text('Cancel'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: ElevatedButton(
                        onPressed: _saveEdit,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF1E7BF2),
                          foregroundColor: Colors.white,
                        ),
                        child: const Text('Save'),
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 90,
            child: Text(
              label,
              style: const TextStyle(
                color: Color(0xFF61708C),
                fontSize: 13,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                color: Color(0xFF132039),
                fontSize: 14,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
