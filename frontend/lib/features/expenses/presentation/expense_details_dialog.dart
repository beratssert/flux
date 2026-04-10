import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/data/auth_session_controller.dart';
import '../data/expenses_controller.dart';
import '../data/expenses_models.dart';

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
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Confirm Delete'),
        content: const Text('Are you sure you want to delete this expense?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text('Cancel')),
          TextButton(
            onPressed: () {
              Navigator.of(context).pop();
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

  void _reject() {
    final reasonController = TextEditingController();
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Reject Expense'),
        content: TextField(
          controller: reasonController,
          decoration: const InputDecoration(
              labelText: 'Reason for rejection', isDense: true),
          autofocus: true,
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () {
              final reason = reasonController.text.trim();
              if (reason.isEmpty) return;
              Navigator.of(context).pop();
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
    final userRole = authState.session?.profile.role ?? 'Employee';
    final isManager = userRole == 'Manager' || userRole == 'Admin';

    // MVP roles rules:
    // Owner can submit draft or rejected
    // Owner can delete draft
    final isDraftOrRejected = widget.expense.status == ExpenseStatus.draft ||
        widget.expense.status == ExpenseStatus.rejected;
    final isDraft = widget.expense.status == ExpenseStatus.draft;
    final canSubmit = !isManager && isDraftOrRejected;
    final canDelete = !isManager && isDraft;

    // Manager can reject submitted
    final isSubmitted = widget.expense.status == ExpenseStatus.submitted;
    final canReject = isManager && isSubmitted;

    return AlertDialog(
      title: const Text('Expense Details'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildRow('Project', widget.projectName),
            _buildRow('Category', widget.categoryName),
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
