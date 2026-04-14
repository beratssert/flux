import 'package:flutter/material.dart';
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
  /// Currency-filtered items shown in the list.
  final List<ExpenseRecord> items;
  /// All items fetched from the API (before client-side currency filter).
  final List<ExpenseRecord> allItems;
  final int totalCount;
  final ExpensesFilter filter;

  const ExpensesState({
    this.isLoading = false,
    this.error,
    this.items = const [],
    this.allItems = const [],
    this.totalCount = 0,
    this.filter = const ExpensesFilter(),
  });

  ExpensesState copyWith({
    bool? isLoading,
    String? error,
    List<ExpenseRecord>? items,
    List<ExpenseRecord>? allItems,
    int? totalCount,
    ExpensesFilter? filter,
  }) {
    return ExpensesState(
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
      items: items ?? this.items,
      allItems: allItems ?? this.allItems,
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
      final all = page.items;
      final filtered = _applyCurrencyFilter(all, state.filter.currencyCode);
      state = state.copyWith(
        isLoading: false,
        allItems: all,
        items: filtered,
        totalCount: page.totalCount,
      );
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  Future<void> updateFilter(ExpensesFilter newFilter) async {
    // If only the currency changed and we already have data, skip the API call.
    final cur = state.filter;
    final onlyCurrencyChanged = cur.projectId == newFilter.projectId &&
        cur.categoryId == newFilter.categoryId &&
        _sameRange(cur.dateRange, newFilter.dateRange) &&
        cur.currencyCode != newFilter.currencyCode;

    if (onlyCurrencyChanged && state.allItems.isNotEmpty) {
      final filtered =
          _applyCurrencyFilter(state.allItems, newFilter.currencyCode);
      state = state.copyWith(filter: newFilter, items: filtered);
      return;
    }

    state = state.copyWith(filter: newFilter);
    await fetchExpenses();
  }

  Future<void> setProjectFilter(int? projectId) async {
    await updateFilter(state.filter
        .copyWith(projectId: projectId, clearProjectId: projectId == null));
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

  // ── helpers ──────────────────────────────────────────────────────────────

  List<ExpenseRecord> _applyCurrencyFilter(
      List<ExpenseRecord> all, String? code) {
    if (code == null) return all;
    return all.where((e) => e.currencyCode == code).toList();
  }

  bool _sameRange(DateTimeRange? a, DateTimeRange? b) {
    if (a == null && b == null) return true;
    if (a == null || b == null) return false;
    return a.start == b.start && a.end == b.end;
  }
}

final expensesControllerProvider =
    StateNotifierProvider<ExpensesController, ExpensesState>((ref) {
  final client = ref.watch(expensesApiClientProvider);
  return ExpensesController(client);
});

/// Unique, sorted currency codes extracted from the raw (unfiltered) item list.
final availableCurrenciesProvider = Provider.autoDispose<List<String>>((ref) {
  final all = ref.watch(expensesControllerProvider).allItems;
  return all.map((e) => e.currencyCode).toSet().toList()..sort();
});

/// Per-currency totals computed from the currently visible (filtered) items.
final expenseStatsProvider = Provider.autoDispose((ref) {
  final state = ref.watch(expensesControllerProvider);
  final categories = ref.watch(expenseCategoriesProvider).valueOrNull ?? [];

  if (state.items.isEmpty) return null;

  final totalsPerCurrency = <String, double>{};
  final categorySums = <int, double>{};

  for (final item in state.items) {
    totalsPerCurrency[item.currencyCode] =
        (totalsPerCurrency[item.currencyCode] ?? 0) + item.amount;
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

  return (totalsPerCurrency: totalsPerCurrency, topCategory: topCategoryName);
});
