import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:logging/logging.dart';

import '../../../core/api_error_message.dart';
import '../../../core/app_config.dart';
import '../../auth/data/auth_session_controller.dart';
import 'expenses_models.dart';

final expensesApiClientProvider = Provider<ExpensesApiClient>((ref) {
  final authState = ref.watch(authSessionControllerProvider);
  return ExpensesApiClient(authState.session?.accessToken);
});

class ExpensesApiClient {
  final String? _accessToken;
  final _logger = Logger('ExpensesApiClient');
  late final Dio _dio;

  ExpensesApiClient(this._accessToken) {
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConfig.apiBaseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        contentType: 'application/json',
      ),
    );
  }

  Options _authorizedOptions() {
    return Options(headers: <String, dynamic>{
      if (_accessToken != null && _accessToken!.isNotEmpty)
        'Authorization': 'Bearer $_accessToken',
    });
  }

  Exception _handleError(DioException e) {
    return Exception(describeApiError(e));
  }

  Future<ExpensesPage> getExpenses({
    int pageNumber = 1,
    int pageSize = 50,
    int? projectId,
    int? categoryId,
    String? status,
  }) async {
    // Return mock expenses page
    return ExpensesPage(
      items: [
        ExpenseRecord(
          id: 101,
          projectId: 1,
          expenseDate: DateTime.now().subtract(const Duration(days: 1)),
          amount: 15.50,
          currencyCode: 'USD',
          categoryId: 2,
          notes: 'Team Lunch',
          status: ExpenseStatus.approved,
        ),
        ExpenseRecord(
          id: 102,
          projectId: 2,
          expenseDate: DateTime.now(),
          amount: 230.00,
          currencyCode: 'USD',
          categoryId: 1,
          notes: 'Flight to conference',
          status: ExpenseStatus.draft,
        ),
      ],
      pageNumber: pageNumber,
      pageSize: pageSize,
      totalCount: 2,
    );
  }

  Future<ExpenseRecord> getExpense(int id) async {
    try {
      final response =
          await _dio.get('/api/v1/Expenses/$id', options: _authorizedOptions());
      if (response.statusCode == 200 && response.data != null) {
        return ExpenseRecord.fromJson(
            (response.data as Map<String, dynamic>)['data'] ?? response.data);
      }
      throw Exception('Unexpected response status: ${response.statusCode}');
    } on DioException catch (e) {
      _logger.warning('getExpense failed', e);
      throw _handleError(e);
    }
  }

  Future<int> createExpense({
    required int projectId,
    required DateTime expenseDate,
    required double amount,
    required String currencyCode,
    required int categoryId,
    String? notes,
    String? receiptUrl,
  }) async {
    try {
      final payload = {
        'projectId': projectId,
        'expenseDate': expenseDate.toIso8601String(),
        'amount': amount,
        'currencyCode': currencyCode,
        'categoryId': categoryId,
        if (notes != null) 'notes': notes,
        if (receiptUrl != null) 'receiptUrl': receiptUrl,
      };

      final response = await _dio.post('/api/v1/Expenses',
          data: payload, options: _authorizedOptions());

      // Could return the ID directly or inside 'data'
      final dynamic possibleId = response.data?['data'] ?? response.data;
      if (possibleId is num) {
        return possibleId.toInt();
      }
      throw Exception('Failed to parse newly created expense ID');
    } on DioException catch (e) {
      _logger.warning('createExpense failed', e);
      throw _handleError(e);
    }
  }

  Future<void> updateExpense(
    int id, {
    required int projectId,
    required DateTime expenseDate,
    required double amount,
    required String currencyCode,
    required int categoryId,
    String? notes,
    String? receiptUrl,
  }) async {
    try {
      final payload = {
        'id': id,
        'projectId': projectId,
        'expenseDate': expenseDate.toIso8601String(),
        'amount': amount,
        'currencyCode': currencyCode,
        'categoryId': categoryId,
        if (notes != null) 'notes': notes,
        if (receiptUrl != null) 'receiptUrl': receiptUrl,
      };

      await _dio.patch('/api/v1/Expenses/$id',
          data: payload, options: _authorizedOptions());
    } on DioException catch (e) {
      _logger.warning('updateExpense failed', e);
      throw _handleError(e);
    }
  }

  Future<void> deleteExpense(int id) async {
    try {
      await _dio.delete('/api/v1/Expenses/$id', options: _authorizedOptions());
    } on DioException catch (e) {
      _logger.warning('deleteExpense failed', e);
      throw _handleError(e);
    }
  }

  Future<void> submitExpense(int id) async {
    try {
      await _dio.post('/api/v1/Expenses/$id/submit',
          options: _authorizedOptions());
    } on DioException catch (e) {
      _logger.warning('submitExpense failed', e);
      throw _handleError(e);
    }
  }

  Future<void> rejectExpense(int id, String reason) async {
    try {
      final payload = {
        'id': id,
        'reason': reason,
      };
      await _dio.post('/api/v1/Expenses/$id/reject',
          data: payload, options: _authorizedOptions());
    } on DioException catch (e) {
      _logger.warning('rejectExpense failed', e);
      throw _handleError(e);
    }
  }

  Future<List<ExpenseCategory>> getCategories() async {
    // Injecting Mock Data for Expense Categories
    return [
      const ExpenseCategory(id: 1, name: 'Travel', isActive: true),
      const ExpenseCategory(id: 2, name: 'Meals', isActive: true),
      const ExpenseCategory(id: 3, name: 'Office Supplies', isActive: true),
      const ExpenseCategory(id: 4, name: 'Software/Hardware', isActive: true),
    ];
  }
}
