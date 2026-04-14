import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:table_calendar/table_calendar.dart';

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
            // Top Category
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
            Container(height: 32, width: 1, color: const Color(0xFFE2E8F0)),
            const SizedBox(width: 24),
            // Per-currency totals
            Expanded(
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    for (final entry
                        in stats.totalsPerCurrency.entries) ...[  
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'TOTAL (${entry.key})',
                            style: const TextStyle(
                              color: Color(0xFF728099),
                              fontSize: 10,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 0.5,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            NumberFormat.simpleCurrency(name: entry.key)
                                .format(entry.value),
                            style: const TextStyle(
                              color: Color(0xFF1E7BF2),
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(width: 20),
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(width: 140), // Reserved space for FAB
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
    return SafeArea(
      bottom: false,
      child: Container(
        color: Colors.white,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Row(
          children: [
            Expanded(
              child: SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    _FilterChip(
                      icon: Icons.work_outline,
                      label: 'Project',
                      menuChildren: [
                        if ((projectsAsync.valueOrNull ?? {}).isEmpty)
                          const MenuItemButton(
                            onPressed: null,
                            child: Text('No projects available'),
                          )
                        else
                          for (final e in (projectsAsync.valueOrNull ?? {}).entries)
                            MenuItemButton(
                              onPressed: () {
                                final f = ref.read(expensesControllerProvider).filter;
                                ref.read(expensesControllerProvider.notifier)
                                   .updateFilter(f.copyWith(projectId: e.key));
                              },
                              child: Text(e.value),
                            ),
                      ],
                    ),
                    const SizedBox(width: 8),
                    _FilterChip(
                      icon: Icons.category_outlined,
                      label: 'Category',
                      menuChildren: [
                        if ((categoriesAsync.valueOrNull ?? []).isEmpty)
                          const MenuItemButton(
                            onPressed: null,
                            child: Text('No categories available'),
                          )
                        else
                          for (final c in (categoriesAsync.valueOrNull ?? []))
                            MenuItemButton(
                              onPressed: () {
                                final f = ref.read(expensesControllerProvider).filter;
                                ref.read(expensesControllerProvider.notifier)
                                   .updateFilter(f.copyWith(categoryId: c.id));
                              },
                              child: Text(c.name),
                            ),
                      ],
                    ),
                    const SizedBox(width: 8),
                    ActionChip(
                      avatar: const Icon(Icons.date_range, size: 18),
                      label: const Text('Time Range'),
                      onPressed: () => _showTimeRangePicker(state.filter.dateRange),
                    ),
                    const SizedBox(width: 8),
                    Consumer(
                      builder: (context, ref, _) {
                        final currencies = ref.watch(availableCurrenciesProvider);
                        return _FilterChip(
                          icon: Icons.currency_exchange,
                          label: 'Currency',
                          menuChildren: [
                            if (currencies.isEmpty)
                              const MenuItemButton(
                                onPressed: null,
                                child: Text('No data yet'),
                              )
                            else
                              for (final c in currencies)
                                MenuItemButton(
                                  onPressed: () {
                                    final f = ref.read(expensesControllerProvider).filter;
                                    ref.read(expensesControllerProvider.notifier)
                                       .updateFilter(f.copyWith(currencyCode: c));
                                  },
                                  child: Text(c),
                                ),
                          ],
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
            IconButton(
              icon: const Icon(Icons.refresh),
              tooltip: 'Yenile',
              onPressed: state.isLoading
                  ? null
                  : () => ref
                      .read(expensesControllerProvider.notifier)
                      .fetchExpenses(),
            ),
            IconButton(
              icon: const Icon(Icons.settings),
              tooltip: 'Ayarlar',
              onPressed: () => context.go('/settings'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActiveFilters(
    ExpensesState state,
    AsyncValue<Map<int, String>> projectsAsync,
    AsyncValue<List<ExpenseCategory>> categoriesAsync,
  ) {
    final filter = state.filter;
    if (filter.projectId == null &&
        filter.categoryId == null &&
        filter.dateRange == null &&
        filter.currencyCode == null) {
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
          if (filter.currencyCode != null)
            InputChip(
              avatar: const Icon(Icons.currency_exchange, size: 16),
              label: Text(filter.currencyCode!),
              onDeleted: () {
                ref.read(expensesControllerProvider.notifier)
                   .updateFilter(filter.copyWith(clearCurrencyCode: true));
              },
            ),
        ],
      ),
    );
  }

  Future<void> _showTimeRangePicker(DateTimeRange? initialRange) async {
    final picked = await showDialog<DateTimeRange>(
      context: context,
      builder: (_) => _DateRangePickerDialog(initialRange: initialRange),
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

/// A chip that opens a [MenuAnchor] dropdown directly below it.
/// Uses Flutter's M3 [MenuAnchor] + [TapRegion] system so switching between
/// open menus requires only a single tap (no ModalBarrier interference).
class _FilterChip extends StatefulWidget {
  const _FilterChip({
    required this.icon,
    required this.label,
    required this.menuChildren,
  });

  final IconData icon;
  final String label;
  final List<Widget> menuChildren;

  @override
  State<_FilterChip> createState() => _FilterChipState();
}

class _FilterChipState extends State<_FilterChip> {
  final _controller = MenuController();

  @override
  Widget build(BuildContext context) {
    return MenuAnchor(
      controller: _controller,
      style: const MenuStyle(
        elevation: WidgetStatePropertyAll(4),
        // Reasonable max size to avoid huge menus.
        maximumSize: WidgetStatePropertyAll(Size(280, 320)),
      ),
      menuChildren: widget.menuChildren,
      child: ActionChip(
        avatar: Icon(widget.icon, size: 18),
        label: Text(widget.label),
        onPressed: () => _controller.isOpen
            ? _controller.close()
            : _controller.open(),
      ),
    );
  }
}

// ─── Date Range Picker Dialog (table_calendar) ────────────────────────────────

/// A compact date range picker dialog built on [TableCalendar].
/// Avoids Flutter's full-screen Material picker and its shader-compilation jank.
class _DateRangePickerDialog extends StatefulWidget {
  const _DateRangePickerDialog({this.initialRange});
  final DateTimeRange? initialRange;

  @override
  State<_DateRangePickerDialog> createState() => _DateRangePickerDialogState();
}

class _DateRangePickerDialogState extends State<_DateRangePickerDialog> {
  DateTime _focusedDay = DateTime.now();
  DateTime? _rangeStart;
  DateTime? _rangeEnd;

  @override
  void initState() {
    super.initState();
    _rangeStart = widget.initialRange?.start;
    _rangeEnd = widget.initialRange?.end;
    _focusedDay = widget.initialRange?.start ?? DateTime.now();
  }

  void _onRangeSelected(DateTime? start, DateTime? end, DateTime focused) {
    setState(() {
      _rangeStart = start;
      _rangeEnd = end;
      _focusedDay = focused;
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final primary = theme.colorScheme.primary;

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8, 16, 8, 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Title
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8.0),
              child: Row(
                children: [
                  Icon(Icons.date_range, color: primary, size: 20),
                  const SizedBox(width: 8),
                  Text('Select Date Range',
                      style: theme.textTheme.titleMedium
                          ?.copyWith(fontWeight: FontWeight.bold)),
                ],
              ),
            ),
            const SizedBox(height: 8),
            // Range hint
            if (_rangeStart != null)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8.0),
                child: Row(
                  children: [
                    _RangeChip(
                      label: DateFormat.yMMMd().format(_rangeStart!),
                      color: primary,
                    ),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 6),
                      child: Icon(Icons.arrow_forward,
                          size: 14, color: Colors.grey.shade500),
                    ),
                    if (_rangeEnd != null)
                      _RangeChip(
                        label: DateFormat.yMMMd().format(_rangeEnd!),
                        color: primary,
                      )
                    else
                      Text('pick end date',
                          style: TextStyle(
                              fontSize: 12, color: Colors.grey.shade500)),
                  ],
                ),
              ),
            const SizedBox(height: 4),
            // Calendar
            TableCalendar(
              firstDay: DateTime(2020),
              lastDay: DateTime(2030),
              focusedDay: _focusedDay,
              rangeStartDay: _rangeStart,
              rangeEndDay: _rangeEnd,
              rangeSelectionMode: RangeSelectionMode.toggledOn,
              onRangeSelected: _onRangeSelected,
              onPageChanged: (focused) {
                _focusedDay = focused;
              },
              headerStyle: const HeaderStyle(
                formatButtonVisible: false,
                titleCentered: true,
              ),
              calendarStyle: CalendarStyle(
                rangeHighlightColor: primary.withOpacity(0.15),
                rangeStartDecoration: BoxDecoration(
                  color: primary,
                  shape: BoxShape.circle,
                ),
                rangeEndDecoration: BoxDecoration(
                  color: primary,
                  shape: BoxShape.circle,
                ),
                todayDecoration: BoxDecoration(
                  color: primary.withOpacity(0.3),
                  shape: BoxShape.circle,
                ),
                selectedDecoration: BoxDecoration(
                  color: primary,
                  shape: BoxShape.circle,
                ),
              ),
            ),
            // Buttons
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: const Text('Cancel'),
                ),
                const SizedBox(width: 8),
                FilledButton(
                  onPressed: _rangeStart != null && _rangeEnd != null
                      ? () => Navigator.of(context).pop(
                            DateTimeRange(
                              start: _rangeStart!,
                              end: _rangeEnd!,
                            ),
                          )
                      : null,
                  child: const Text('Apply'),
                ),
                const SizedBox(width: 8),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _RangeChip extends StatelessWidget {
  const _RangeChip({required this.label, required this.color});
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withOpacity(0.4)),
      ),
      child: Text(label,
          style: TextStyle(
              fontSize: 12, color: color, fontWeight: FontWeight.w600)),
    );
  }
}
