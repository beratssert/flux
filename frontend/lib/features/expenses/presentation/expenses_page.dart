import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/providers/project_provider.dart';
import '../../auth/data/auth_session_controller.dart';
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
    final userRole = ref.watch(authSessionControllerProvider).session?.profile.role ?? 'Employee';
    final canManage = userRole != 'Admin'; // Expenses.Manage.Self: Employee + Manager only

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
      floatingActionButton: canManage
          ? FloatingActionButton.extended(
              onPressed: () =>
                  _showAddExpenseDialog(context, projectsAsync, categoriesAsync),
              icon: const Icon(Icons.add),
              label: const Text('Add Expense'),
            )
          : null,
    );
  }

  Widget _buildBody(
    ExpensesState state,
    AsyncValue<Map<int, String>> projectsAsync,
    AsyncValue<List<ExpenseCategory>> categoriesAsync,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _buildTopBar(state, projectsAsync, categoriesAsync),
        _buildActiveFilters(state, projectsAsync, categoriesAsync),
        Expanded(
          child: _buildListContent(state, projectsAsync, categoriesAsync),
        ),
        _buildSummaryBar(),
      ],
    );
  }

  Widget _buildSummaryBar() {
    final stats = ref.watch(expenseStatsProvider);
    if (stats == null) return const SizedBox.shrink();

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        border: const Border(top: BorderSide(color: Color(0xFFE2E8F0))),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, -5),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            // Top Category Column
            Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'TOP CATEGORY',
                  style: TextStyle(
                    color: Color(0xFF728099),
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.5,
                  ),
                ),
                const SizedBox(height: 2),
                Row(
                  children: [
                    const Icon(Icons.stars_rounded,
                        size: 14, color: Color(0xFF1E7BF2)),
                    const SizedBox(width: 4),
                    Text(
                      stats.topCategory,
                      style: const TextStyle(
                        color: Color(0xFF132039),
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(width: 24),
            // Divider
            Container(
              height: 32,
              width: 1,
              color: const Color(0xFFE2E8F0),
            ),
            const SizedBox(width: 24),
            // Total Spent Column
            Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'TOTAL SPENT',
                  style: TextStyle(
                    color: Color(0xFF728099),
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.5,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  NumberFormat.simpleCurrency().format(stats.total),
                  style: const TextStyle(
                    color: Color(0xFF1E7BF2),
                    fontSize: 16,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
            const Spacer(), // Pushes FAB space to the right
            const SizedBox(width: 140), // Reserved space for "Add Expense" FAB
          ],
        ),
      ),
    );
  }

  Widget _buildTopBar(
    ExpensesState state,
    AsyncValue<Map<int, String>> projectsAsync,
    AsyncValue<List<ExpenseCategory>> categoriesAsync,
  ) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      color: Theme.of(context).colorScheme.surface,
      child: Row(
        children: [
          Expanded(
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  ActionChip(
                    avatar: const Icon(Icons.work_outline, size: 18),
                    label: const Text('Project'),
                    onPressed: () => _showProjectPicker(projectsAsync.valueOrNull ?? {}),
                  ),
                  const SizedBox(width: 8),
                  ActionChip(
                    avatar: const Icon(Icons.category_outlined, size: 18),
                    label: const Text('Category'),
                    onPressed: () => _showCategoryPicker(categoriesAsync.valueOrNull ?? []),
                  ),
                  const SizedBox(width: 8),
                  ActionChip(
                    avatar: const Icon(Icons.date_range, size: 18),
                    label: const Text('Time Range'),
                    onPressed: () => _showTimeRangePicker(state.filter.dateRange),
                  ),
                ],
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.settings),
            tooltip: 'Ayarlar',
            onPressed: () => context.go('/settings'),
          ),
        ],
      ),
    );
  }

  Widget _buildActiveFilters(
    ExpensesState state,
    AsyncValue<Map<int, String>> projectsAsync,
    AsyncValue<List<ExpenseCategory>> categoriesAsync,
  ) {
    final filter = state.filter;
    if (filter.projectId == null && filter.categoryId == null && filter.dateRange == null) {
      return const SizedBox.shrink();
    }

    final projects = projectsAsync.valueOrNull ?? {};
    final categories = categoriesAsync.valueOrNull ?? [];

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 4.0),
      child: Wrap(
        spacing: 8,
        runSpacing: 4,
        children: [
          if (filter.projectId != null)
            InputChip(
              label: Text('Project: ${projects[filter.projectId] ?? filter.projectId}'),
              onDeleted: () {
                ref.read(expensesControllerProvider.notifier)
                   .updateFilter(filter.copyWith(clearProjectId: true));
              },
            ),
          if (filter.categoryId != null)
            InputChip(
              label: Text('Category: ${categories.firstWhere((c) => c.id == filter.categoryId, orElse: () => const ExpenseCategory(id: 0, name: 'Unknown', isActive: true)).name}'),
              onDeleted: () {
                ref.read(expensesControllerProvider.notifier)
                   .updateFilter(filter.copyWith(clearCategoryId: true));
              },
            ),
          if (filter.dateRange != null)
            InputChip(
              label: Text('${_dateFormat.format(filter.dateRange!.start)} - ${_dateFormat.format(filter.dateRange!.end)}'),
              onDeleted: () {
                ref.read(expensesControllerProvider.notifier)
                   .updateFilter(filter.copyWith(clearDateRange: true));
              },
            ),
        ],
      ),
    );
  }

  void _showProjectPicker(Map<int, String> projects) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Padding(
                padding: EdgeInsets.all(16.0),
                child: Text('Select Project', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
              ),
              const Divider(height: 1),
              if (projects.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(32.0),
                  child: Text('No projects available.'),
                )
              else
                Flexible(
                  child: ListView.builder(
                    shrinkWrap: true,
                    itemCount: projects.length,
                    itemBuilder: (context, index) {
                      final entry = projects.entries.elementAt(index);
                      return ListTile(
                        title: Text(entry.value),
                        onTap: () {
                          Navigator.pop(context);
                          final currentFilter = ref.read(expensesControllerProvider).filter;
                          ref.read(expensesControllerProvider.notifier)
                             .updateFilter(currentFilter.copyWith(projectId: entry.key));
                        },
                      );
                    },
                  ),
                ),
            ],
          ),
        );
      },
    );
  }

  void _showCategoryPicker(List<ExpenseCategory> categories) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Padding(
                padding: EdgeInsets.all(16.0),
                child: Text('Select Category', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
              ),
              const Divider(height: 1),
              if (categories.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(32.0),
                  child: Text('No categories available.'),
                )
              else
                Flexible(
                  child: ListView.builder(
                    shrinkWrap: true,
                    itemCount: categories.length,
                    itemBuilder: (context, index) {
                      final category = categories[index];
                      return ListTile(
                        title: Text(category.name),
                        onTap: () {
                          Navigator.pop(context);
                          final currentFilter = ref.read(expensesControllerProvider).filter;
                          ref.read(expensesControllerProvider.notifier)
                             .updateFilter(currentFilter.copyWith(categoryId: category.id));
                        },
                      );
                    },
                  ),
                ),
            ],
          ),
        );
      },
    );
  }

  Future<void> _showTimeRangePicker(DateTimeRange? initialRange) async {
    final DateTimeRange? picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
      initialDateRange: initialRange,
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: Theme.of(context).colorScheme.copyWith(
              primary: Theme.of(context).colorScheme.primary,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      final currentFilter = ref.read(expensesControllerProvider).filter;
      ref.read(expensesControllerProvider.notifier)
         .updateFilter(currentFilter.copyWith(dateRange: picked));
    }
  }

  Widget _buildListContent(
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
    final initialProjectId = ref.read(expensesControllerProvider).filter.projectId;
    showDialog(
      context: context,
      builder: (context) => AddExpenseDialog(
        initialProjectId: initialProjectId,
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
