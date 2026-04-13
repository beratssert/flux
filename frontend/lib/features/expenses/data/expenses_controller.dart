import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'expenses_api_client.dart';
import 'expenses_models.dart';

final expenseCategoriesProvider =
    FutureProvider.autoDispose<List<ExpenseCategory>>((ref) async {
  final client = ref.watch(expensesApiClientProvider);
  return await client.getCategories();
});

class ExpensesState {
  final bool isLoading;
  final String? error;
  final List<ExpenseRecord> items;
  final int totalCount;
  final ExpensesFilter filter;

  const ExpensesState({
    this.isLoading = false,
    this.error,
    this.items = const [],
    this.totalCount = 0,
    this.filter = const ExpensesFilter(),
  });

  ExpensesState copyWith({
    bool? isLoading,
    String? error,
    List<ExpenseRecord>? items,
    int? totalCount,
    ExpensesFilter? filter,
  }) {
    return ExpensesState(
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
      items: items ?? this.items,
      totalCount: totalCount ?? this.totalCount,
      filter: filter ?? this.filter,
    );
  }
}

class ExpensesController extends StateNotifier<ExpensesState> {
  final ExpensesApiClient _client;

  ExpensesController(this._client) : super(const ExpensesState());

  Future<void> fetchExpenses() async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final page = await _client.getExpenses(
        pageNumber: 1, 
        pageSize: 100,
        projectId: state.filter.projectId,
        categoryId: state.filter.categoryId,
        from: state.filter.dateRange?.start,
        to: state.filter.dateRange?.end,
      );
      state = state.copyWith(
        isLoading: false,
        items: page.items,
        totalCount: page.totalCount,
      );
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  Future<void> updateFilter(ExpensesFilter newFilter) async {
    state = state.copyWith(filter: newFilter);
    await fetchExpenses();
  }

  Future<void> setProjectFilter(int? projectId) async {
    await updateFilter(state.filter.copyWith(projectId: projectId, clearProjectId: projectId == null));
  }

  Future<void> createExpense({
    required int projectId,
    required DateTime expenseDate,
    required double amount,
    required String currencyCode,
    required int categoryId,
    String? notes,
  }) async {
    await _client.createExpense(
      projectId: projectId,
      expenseDate: expenseDate,
      amount: amount,
      currencyCode: currencyCode,
      categoryId: categoryId,
      notes: notes,
    );
    await fetchExpenses();
  }

  Future<void> updateExpense(
    int id, {
    required int projectId,
    required DateTime expenseDate,
    required double amount,
    required String currencyCode,
    required int categoryId,
    String? notes,
  }) async {
    await _client.updateExpense(
      id,
      projectId: projectId,
      expenseDate: expenseDate,
      amount: amount,
      currencyCode: currencyCode,
      categoryId: categoryId,
      notes: notes,
    );
    await fetchExpenses();
  }

  Future<void> deleteExpense(int id) async {
    await _client.deleteExpense(id);
    await fetchExpenses();
  }

  /// Returns a fresh [ExpenseRecord] directly from the API.
  /// Use this before opening the edit dialog to avoid stale-snapshot overwrites.
  Future<ExpenseRecord> fetchExpenseById(int id) {
    return _client.getExpense(id);
  }

  Future<void> submitExpense(int id) async {
    await _client.submitExpense(id);
    await fetchExpenses();
  }

  Future<void> rejectExpense(int id, String reason) async {
    await _client.rejectExpense(id, reason);
    await fetchExpenses();
  }
}

final expensesControllerProvider =
    StateNotifierProvider<ExpensesController, ExpensesState>((ref) {
  final client = ref.watch(expensesApiClientProvider);
  return ExpensesController(client);
});

final expenseStatsProvider = Provider.autoDispose((ref) {
  final state = ref.watch(expensesControllerProvider);
  final categories = ref.watch(expenseCategoriesProvider).valueOrNull ?? [];

  if (state.items.isEmpty) return null;

  double total = 0;
  final categorySums = <int, double>{};

  for (final item in state.items) {
    total += item.amount;
    categorySums[item.categoryId] =
        (categorySums[item.categoryId] ?? 0) + item.amount;
  }

  int? topCategoryId;
  double maxAmount = -1;

  categorySums.forEach((id, sum) {
    if (sum > maxAmount) {
      maxAmount = sum;
      topCategoryId = id;
    }
  });

  String topCategoryName = 'None';
  if (topCategoryId != null) {
    topCategoryName = categories
        .firstWhere(
          (c) => c.id == topCategoryId,
          orElse: () =>
              const ExpenseCategory(id: 0, name: 'Unknown', isActive: true),
        )
        .name;
  }

  return (total: total, topCategory: topCategoryName);
});
