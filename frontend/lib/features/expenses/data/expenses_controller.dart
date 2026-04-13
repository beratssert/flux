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

  const ExpensesState({
    this.isLoading = false,
    this.error,
    this.items = const [],
    this.totalCount = 0,
  });

  ExpensesState copyWith({
    bool? isLoading,
    String? error,
    List<ExpenseRecord>? items,
    int? totalCount,
  }) {
    return ExpensesState(
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
      items: items ?? this.items,
      totalCount: totalCount ?? this.totalCount,
    );
  }
}

class ExpensesController extends StateNotifier<ExpensesState> {
  final ExpensesApiClient _client;
  int? currentProjectId;

  ExpensesController(this._client) : super(const ExpensesState());

  Future<void> fetchExpenses() async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final page = await _client.getExpenses(
        pageNumber: 1, 
        pageSize: 100,
        projectId: currentProjectId,
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

  Future<void> setProjectFilter(int? projectId) async {
    currentProjectId = projectId;
    await fetchExpenses();
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
