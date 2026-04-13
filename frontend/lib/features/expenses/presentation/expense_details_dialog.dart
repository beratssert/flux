import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers/project_provider.dart';
import '../../auth/data/auth_session_controller.dart';
import '../data/expenses_controller.dart';
import '../data/expenses_models.dart';
import 'edit_expense_dialog.dart';

class ExpenseDetailsDialog extends ConsumerStatefulWidget {
  final ExpenseRecord expense;
  final String projectName;
  final String categoryName;

  const ExpenseDetailsDialog({
    super.key,
    required this.expense,
    required this.projectName,
    required this.categoryName,
  });

  @override
  ConsumerState<ExpenseDetailsDialog> createState() =>
      _ExpenseDetailsDialogState();
}

class _ExpenseDetailsDialogState extends ConsumerState<ExpenseDetailsDialog> {
  bool _isWorking = false;

  void _runAction(Future<void> Function() action) async {
    setState(() => _isWorking = true);
    try {
      await action();
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        setState(() => _isWorking = false);
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Failed: $e')));
      }
    }
  }

  void _submit() => _runAction(() => ref
      .read(expensesControllerProvider.notifier)
      .submitExpense(widget.expense.id));

  void _delete() {
    final outerContext = context; // Capture before entering nested builder
    showDialog(
      context: outerContext,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Confirm Delete'),
        content: const Text('Are you sure you want to delete this expense?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Cancel')),
          TextButton(
            onPressed: () {
              Navigator.of(dialogContext).pop();
              _runAction(() => ref
                  .read(expensesControllerProvider.notifier)
                  .deleteExpense(widget.expense.id));
            },
            child: const Text('Delete', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
  }

  void _edit() async {
    // Fetch a fresh snapshot to avoid stale-data overwrites
    setState(() => _isWorking = true);
    try {
      final freshExpense = await ref
          .read(expensesControllerProvider.notifier)
          .fetchExpenseById(widget.expense.id);

      if (!mounted) return;
      setState(() => _isWorking = false);

      final projectsAsync = ref.read(projectNamesProvider);
      final categoriesAsync = ref.read(expenseCategoriesProvider);
      final projects = projectsAsync.valueOrNull ?? {};
      final categories = categoriesAsync.valueOrNull ?? [];

      showDialog(
        context: context,
        builder: (context) => EditExpenseDialog(
          expense: freshExpense,
          projects: projects,
          categories: categories,
        ),
      );
    } catch (e) {
      if (mounted) {
        setState(() => _isWorking = false);
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Failed to load expense: $e')));
      }
    }
  }

  void _reject() {
    final reasonController = TextEditingController();
    final outerContext = context; // Capture before entering nested builder
    showDialog(
      context: outerContext,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Reject Expense'),
        content: TextField(
          controller: reasonController,
          decoration: const InputDecoration(
              labelText: 'Reason for rejection', isDense: true),
          autofocus: true,
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () {
              final reason = reasonController.text.trim();
              if (reason.isEmpty) return;
              Navigator.of(dialogContext).pop();
              _runAction(() => ref
                  .read(expensesControllerProvider.notifier)
                  .rejectExpense(widget.expense.id, reason));
            },
            style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red, foregroundColor: Colors.white),
            child: const Text('Reject'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isWorking) {
      return const AlertDialog(
        content: SizedBox(
          width: 100,
          height: 100,
          child: Center(child: CircularProgressIndicator()),
        ),
      );
    }

    final authState = ref.watch(authSessionControllerProvider);
    final userProfile = authState.session?.profile;
    final userRole = userProfile?.role ?? 'Employee';
    final currentUserId = userProfile?.id ?? '';
    
    final isOwner = widget.expense.userId == currentUserId;
    final isManager = userRole == 'Manager';
    final isAdmin = userRole == 'Admin';

    // Business Rules from docs/authorization-matrix.md:
    // 1. Employee/Manager can manage SELF expenses (Edit, Delete, Submit).
    // 2. Manager can REJECT team expenses (but not edit them).
    // 3. Admin is Read-only in MVP.

    final isDraftOrRejected = widget.expense.status == ExpenseStatus.draft ||
        widget.expense.status == ExpenseStatus.rejected;
    final isDraft = widget.expense.status == ExpenseStatus.draft;
    final isSubmitted = widget.expense.status == ExpenseStatus.submitted;

    // Actions for Owners (Employee or Manager on their own data)
    final canSubmit = isOwner && !isAdmin && isDraftOrRejected;
    final canDelete = isOwner && !isAdmin && isDraft;
    final canEdit = isOwner && !isAdmin && isDraftOrRejected;

    // Actions for Managers on Team data
    // Manager cannot reject their own expense (they should edit/delete it)
    final canReject = isManager && isSubmitted && !isOwner;
    
    // UI Helpers
    final showRejectInfo = !isManager && isAdmin && isSubmitted;
    final showOwnershipLabel = !isOwner && !isAdmin;

    return AlertDialog(
      title: const Text('Expense Details'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildRow('Project', widget.projectName),
            _buildRow('Category', widget.categoryName),
            if (showOwnershipLabel)
              _buildRow('User ID', widget.expense.userId, color: Colors.blueGrey),
            _buildRow('Amount',
                '${widget.expense.amount} ${widget.expense.currencyCode}'),
            _buildRow('Date',
                widget.expense.expenseDate.toLocal().toString().split(' ')[0]),
            _buildRow('Status', widget.expense.status.name),
            if (widget.expense.notes?.isNotEmpty == true)
              _buildRow('Notes', widget.expense.notes!),
            if (widget.expense.rejectionReason?.isNotEmpty == true)
              _buildRow('Rejection Reason', widget.expense.rejectionReason!,
                  color: Colors.red),
            if (widget.expense.reviewedBy?.isNotEmpty == true)
              _buildRow('Reviewed By', widget.expense.reviewedBy!),
            if (showRejectInfo) ...[
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.orange.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.orange.shade300),
                ),
                child: Row(
                  children: [
                    Icon(Icons.info_outline, color: Colors.orange.shade700, size: 20),
                    const SizedBox(width: 8),
                    const Expanded(
                      child: Text(
                        'Sadece yöneticiler (Manager) harcamaları reddedebilir.',
                        style: TextStyle(fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Close')),
        if (canDelete)
          TextButton(
              onPressed: _delete,
              child: const Text('Delete', style: TextStyle(color: Colors.red))),
        if (canEdit)
          OutlinedButton(
              onPressed: _edit,
              child: const Text('Edit')),
        if (canReject)
          TextButton(
              onPressed: _reject,
              child: const Text('Reject', style: TextStyle(color: Colors.red))),
        if (canSubmit)
          ElevatedButton(
              onPressed: _submit, child: const Text('Submit for Review')),
      ],
    );
  }

  Widget _buildRow(String label, String value, {Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(
              '$label:',
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(color: color, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }
}
