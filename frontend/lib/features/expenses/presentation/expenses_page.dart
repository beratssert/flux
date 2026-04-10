import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/providers/project_provider.dart';
import '../data/expenses_controller.dart';
import '../data/expenses_models.dart';
import 'add_expense_dialog.dart';
import 'expense_details_dialog.dart';

class ExpensesPage extends ConsumerStatefulWidget {
  const ExpensesPage({super.key});

  @override
  ConsumerState<ExpensesPage> createState() => _ExpensesPageState();
}

class _ExpensesPageState extends ConsumerState<ExpensesPage> {
  final _dateFormat = DateFormat.yMMMd();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(expensesControllerProvider.notifier).fetchExpenses();
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(expensesControllerProvider);
    final projectsAsync = ref.watch(projectNamesProvider);
    final categoriesAsync = ref.watch(expenseCategoriesProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Expenses'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: state.isLoading
                ? null
                : () => ref
                    .read(expensesControllerProvider.notifier)
                    .fetchExpenses(),
          )
        ],
      ),
      body: _buildBody(state, projectsAsync, categoriesAsync),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () =>
            _showAddExpenseDialog(context, projectsAsync, categoriesAsync),
        icon: const Icon(Icons.add),
        label: const Text('Add Expense'),
      ),
    );
  }

  Widget _buildBody(
    ExpensesState state,
    AsyncValue<Map<int, String>> projectsAsync,
    AsyncValue<List<ExpenseCategory>> categoriesAsync,
  ) {
    if (state.isLoading && state.items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null && state.items.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text('Error: ${state.error}',
                style: const TextStyle(color: Colors.red)),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () =>
                  ref.read(expensesControllerProvider.notifier).fetchExpenses(),
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    if (state.items.isEmpty) {
      return const Center(
        child: Text('No expenses found.', style: TextStyle(color: Colors.grey)),
      );
    }

    final projectsMap = projectsAsync.valueOrNull ?? {};
    final categories = categoriesAsync.valueOrNull ?? [];

    return ListView.builder(
      padding: const EdgeInsets.only(bottom: 80),
      itemCount: state.items.length,
      itemBuilder: (context, index) {
        final expense = state.items[index];
        final projectName =
            projectsMap[expense.projectId] ?? 'Project ${expense.projectId}';
        final categoryName = categories
            .firstWhere(
              (c) => c.id == expense.categoryId,
              orElse: () =>
                  const ExpenseCategory(id: 0, name: 'Unknown', isActive: true),
            )
            .name;

        return Card(
          margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          elevation: 2,
          clipBehavior: Clip.hardEdge,
          child: InkWell(
            onTap: () => _showExpenseDetails(
                context, expense, projectName, categoryName),
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Text(
                              projectName,
                              style: const TextStyle(
                                  fontWeight: FontWeight.bold, fontSize: 16),
                            ),
                            const SizedBox(width: 8),
                            _buildStatusBadge(expense.status),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${_dateFormat.format(expense.expenseDate)} • $categoryName',
                          style: TextStyle(
                              color: Colors.grey.shade600, fontSize: 13),
                        ),
                        if (expense.notes?.isNotEmpty == true) ...[
                          const SizedBox(height: 4),
                          Text(
                            expense.notes!,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(color: Colors.grey.shade800),
                          ),
                        ]
                      ],
                    ),
                  ),
                  const SizedBox(width: 16),
                  Text(
                    NumberFormat.simpleCurrency(name: expense.currencyCode)
                        .format(expense.amount),
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _buildStatusBadge(ExpenseStatus status) {
    Color bgColor;
    Color fgColor;
    String label;

    switch (status) {
      case ExpenseStatus.draft:
        bgColor = Colors.grey.shade200;
        fgColor = Colors.grey.shade800;
        label = 'Draft';
        break;
      case ExpenseStatus.submitted:
        bgColor = Colors.blue.shade100;
        fgColor = Colors.blue.shade900;
        label = 'Submitted';
        break;
      case ExpenseStatus.approved:
        bgColor = Colors.green.shade100;
        fgColor = Colors.green.shade900;
        label = 'Approved';
        break;
      case ExpenseStatus.rejected:
        bgColor = Colors.red.shade100;
        fgColor = Colors.red.shade900;
        label = 'Rejected';
        break;
      case ExpenseStatus.unknown:
        bgColor = Colors.grey.shade300;
        fgColor = Colors.black;
        label = 'Unknown';
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: fgColor,
          fontSize: 12,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  void _showAddExpenseDialog(
    BuildContext context,
    AsyncValue<Map<int, String>> projectsAsync,
    AsyncValue<List<ExpenseCategory>> categoriesAsync,
  ) {
    showDialog(
      context: context,
      builder: (context) => AddExpenseDialog(
        projects: projectsAsync.valueOrNull ?? {},
        categories:
            categoriesAsync.valueOrNull?.where((c) => c.isActive).toList() ??
                [],
      ),
    );
  }

  void _showExpenseDetails(
    BuildContext context,
    ExpenseRecord expense,
    String projectName,
    String categoryName,
  ) {
    showDialog(
      context: context,
      builder: (context) => ExpenseDetailsDialog(
        expense: expense,
        projectName: projectName,
        categoryName: categoryName,
      ),
    );
  }
}
