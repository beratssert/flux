import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/expenses_controller.dart';
import '../data/expenses_models.dart';

class AddExpenseDialog extends ConsumerStatefulWidget {
  final Map<int, String> projects;
  final List<ExpenseCategory> categories;

  const AddExpenseDialog(
      {super.key, required this.projects, required this.categories});

  @override
  ConsumerState<AddExpenseDialog> createState() => _AddExpenseDialogState();
}

class _AddExpenseDialogState extends ConsumerState<AddExpenseDialog> {
  final _formKey = GlobalKey<FormState>();
  final _amountController = TextEditingController();
  final _notesController = TextEditingController();

  int? _selectedProjectId;
  int? _selectedCategoryId;
  DateTime _selectedDate = DateTime.now();
  String _selectedCurrency = 'USD';

  bool _isSubmitting = false;

  void _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedProjectId == null || _selectedCategoryId == null) return;

    final amountStr = _amountController.text.trim();
    if (amountStr.isEmpty) return;

    final amount = double.tryParse(amountStr);
    if (amount == null || amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter a valid positive amount.')),
      );
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      await ref.read(expensesControllerProvider.notifier).createExpense(
            projectId: _selectedProjectId!,
            expenseDate: _selectedDate,
            amount: amount,
            currencyCode: _selectedCurrency,
            categoryId: _selectedCategoryId!,
            notes: _notesController.text.trim(),
          );

      if (mounted) {
        Navigator.of(context).pop();
      }
    } catch (e) {
      setState(() => _isSubmitting = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to create expense: $e')),
        );
      }
    }
  }

  @override
  void initState() {
    super.initState();
    if (widget.projects.isNotEmpty) {
      _selectedProjectId = widget.projects.keys.first;
    }
    if (widget.categories.isNotEmpty) {
      _selectedCategoryId = widget.categories.first.id;
    }
  }

  Future<void> _pickDate() async {
    final date = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime(2000),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );

    if (date != null && mounted) {
      setState(() {
        _selectedDate = date;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (widget.projects.isEmpty) {
      return AlertDialog(
        title: const Text('Add Expense'),
        content: const Text(
            'You do not have any projects or known projects are empty. Please register time first.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Close'),
          ),
        ],
      );
    }

    if (widget.categories.isEmpty) {
      return AlertDialog(
        title: const Text('Add Expense'),
        content: const Text('No expense categories available.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Close'),
          ),
        ],
      );
    }

    return AlertDialog(
      title: const Text('Add Expense'),
      content: SingleChildScrollView(
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              DropdownButtonFormField<int>(
                decoration:
                    const InputDecoration(labelText: 'Project', isDense: true),
                initialValue: _selectedProjectId,
                items: widget.projects.entries.map((e) {
                  return DropdownMenuItem<int>(
                      value: e.key, child: Text(e.value));
                }).toList(),
                onChanged: (val) {
                  setState(() => _selectedProjectId = val);
                },
                validator: (val) => val == null ? 'Required' : null,
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<int>(
                decoration:
                    const InputDecoration(labelText: 'Category', isDense: true),
                initialValue: _selectedCategoryId,
                items: widget.categories.map((c) {
                  return DropdownMenuItem<int>(
                      value: c.id, child: Text(c.name));
                }).toList(),
                onChanged: (val) {
                  setState(() => _selectedCategoryId = val);
                },
                validator: (val) => val == null ? 'Required' : null,
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    flex: 2,
                    child: TextFormField(
                      controller: _amountController,
                      decoration: const InputDecoration(
                          labelText: 'Amount', isDense: true),
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      validator: (val) {
                        if (val == null || val.isEmpty) return 'Required';
                        if (double.tryParse(val) == null) return 'Invalid';
                        return null;
                      },
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      decoration: const InputDecoration(
                          labelText: 'Currency', isDense: true),
                      initialValue: _selectedCurrency,
                      items: const [
                        DropdownMenuItem(value: 'USD', child: Text('USD')),
                        DropdownMenuItem(value: 'EUR', child: Text('EUR')),
                        DropdownMenuItem(value: 'TRY', child: Text('TRY')),
                      ],
                      onChanged: (val) {
                        setState(() => _selectedCurrency = val!);
                      },
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              InkWell(
                onTap: _pickDate,
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Date',
                    isDense: true,
                    suffixIcon: Icon(Icons.calendar_today, size: 20),
                  ),
                  child: Text(
                    '${_selectedDate.year}-${_selectedDate.month.toString().padLeft(2, '0')}-${_selectedDate.day.toString().padLeft(2, '0')}',
                  ),
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _notesController,
                decoration: const InputDecoration(
                    labelText: 'Notes (optional)', isDense: true),
                maxLines: 2,
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isSubmitting ? null : () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: _isSubmitting ? null : _submit,
          child: _isSubmitting
              ? const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Save'),
        ),
      ],
    );
  }
}
