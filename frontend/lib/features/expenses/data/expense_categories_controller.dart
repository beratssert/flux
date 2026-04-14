import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../data/expenses_api_client.dart';
import '../data/expenses_models.dart';
import '../data/expenses_controller.dart';

class ExpenseCategoriesState {
  final bool isLoading;
  final String? error;
  final List<ExpenseCategory> items;

  const ExpenseCategoriesState({
    this.isLoading = false,
    this.error,
    this.items = const [],
  });

  ExpenseCategoriesState copyWith({
    bool? isLoading,
    String? error,
    List<ExpenseCategory>? items,
  }) {
    return ExpenseCategoriesState(
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
      items: items ?? this.items,
    );
  }
}

class ExpenseCategoriesController
    extends StateNotifier<ExpenseCategoriesState> {
  final ExpensesApiClient _client;
  final Ref _ref;

  ExpenseCategoriesController(this._client, this._ref)
      : super(const ExpenseCategoriesState()) {
    fetchCategories();
  }

  Future<void> fetchCategories() async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final categories = await _client.getCategories();
      state = state.copyWith(isLoading: false, items: categories);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
    }
  }

  Future<void> createCategory(String name) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      await _client.createCategory(name);
      await fetchCategories();
      // Refresh the shared categories provider used in the expenses module
      _ref.invalidate(expenseCategoriesProvider);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
      rethrow;
    }
  }

  Future<void> updateCategory(int id, String name, bool isActive) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      await _client.updateCategory(id, name, isActive);
      await fetchCategories();
      // Refresh the shared categories provider used in the expenses module
      _ref.invalidate(expenseCategoriesProvider);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
      rethrow;
    }
  }

  Future<void> deleteCategory(int id) async {
    // Backend DELETE endpoint is not implemented yet.
    // Note for backend team: Expense categories may have a many-to-one
    // relationship with projects. Handle cascade accordingly.
    throw UnimplementedError('Backend delete endpoint eksik.');
  }
}

final expenseCategoriesControllerProvider = StateNotifierProvider.autoDispose<
    ExpenseCategoriesController, ExpenseCategoriesState>((ref) {
  final client = ref.watch(expensesApiClientProvider);
  return ExpenseCategoriesController(client, ref);
});
