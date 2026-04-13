
enum ExpenseStatus {
  draft,
  submitted,
  approved,
  rejected,
  unknown,
}

class ExpenseCategory {
  final int id;
  final String name;
  final bool isActive;

  const ExpenseCategory({
    required this.id,
    required this.name,
    required this.isActive,
  });

  factory ExpenseCategory.fromJson(Map<String, dynamic> json) {
    return ExpenseCategory(
      id: _readInt(json, const ['id', 'Id']) ?? 0,
      name: _readString(json, const ['name', 'Name']) ?? '',
      isActive: _readBool(json, const ['isActive', 'IsActive']) ?? true,
    );
  }
}

class ExpenseRecord {
  final int id;
  final int projectId;
  final DateTime expenseDate;
  final double amount;
  final String currencyCode;
  final int categoryId;
  final String? notes;
  final String? receiptUrl;
  final ExpenseStatus status;
  final String? rejectionReason;
  final String? reviewedBy;

  const ExpenseRecord({
    required this.id,
    required this.projectId,
    required this.expenseDate,
    required this.amount,
    required this.currencyCode,
    required this.categoryId,
    this.notes,
    this.receiptUrl,
    required this.status,
    this.rejectionReason,
    this.reviewedBy,
  });

  factory ExpenseRecord.fromJson(Map<String, dynamic> json) {
    return ExpenseRecord(
      id: _readInt(json, const ['id', 'Id']) ?? 0,
      projectId: _readInt(json, const ['projectId', 'ProjectId']) ?? 0,
      expenseDate:
          _readUtcDateTime(json, const ['expenseDate', 'ExpenseDate']) ??
              DateTime.now(),
      amount: _readDouble(json, const ['amount', 'Amount']) ?? 0.0,
      currencyCode:
          _readString(json, const ['currencyCode', 'CurrencyCode']) ?? 'USD',
      categoryId: _readInt(json, const ['categoryId', 'CategoryId']) ?? 0,
      notes: _readString(json, const ['notes', 'Notes']),
      receiptUrl: _readString(json, const ['receiptUrl', 'ReceiptUrl']),
      status: _parseStatus(_readString(json, const ['status', 'Status'])),
      rejectionReason:
          _readString(json, const ['rejectionReason', 'RejectionReason']),
      reviewedBy: _readString(json, const ['reviewedBy', 'ReviewedBy']),
    );
  }
}

class ExpensesPage {
  final List<ExpenseRecord> items;
  final int pageNumber;
  final int pageSize;
  final int totalCount;

  const ExpensesPage({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalCount,
  });

  factory ExpensesPage.fromJson(Map<String, dynamic> json) {
    final list = _readList(json, const ['items', 'Items', 'data', 'Data']) ?? [];
    return ExpensesPage(
      items: list
          .map((e) => ExpenseRecord.fromJson(e as Map<String, dynamic>))
          .toList(),
      pageNumber: _readInt(json, const ['pageNumber', 'PageNumber']) ?? 1,
      pageSize: _readInt(json, const ['pageSize', 'PageSize']) ?? 10,
      totalCount: _readInt(json, const ['totalCount', 'TotalCount']) ?? 0,
    );
  }
}

// Helpers

String? _readString(Map<String, dynamic> json, List<String> keys) {
  for (final k in keys) {
    if (json.containsKey(k) && json[k] != null) {
      return json[k].toString();
    }
  }
  return null;
}

int? _readInt(Map<String, dynamic> json, List<String> keys) {
  for (final k in keys) {
    if (json.containsKey(k) && json[k] != null) {
      final val = json[k];
      if (val is int) return val;
      if (val is num) return val.toInt();
      if (val is String) return int.tryParse(val);
    }
  }
  return null;
}

double? _readDouble(Map<String, dynamic> json, List<String> keys) {
  for (final k in keys) {
    if (json.containsKey(k) && json[k] != null) {
      final val = json[k];
      if (val is double) return val;
      if (val is num) return val.toDouble();
      if (val is String) return double.tryParse(val);
    }
  }
  return null;
}

bool? _readBool(Map<String, dynamic> json, List<String> keys) {
  for (final k in keys) {
    if (json.containsKey(k) && json[k] != null) {
      final val = json[k];
      if (val is bool) return val;
      if (val is String) return val.toLowerCase() == 'true';
    }
  }
  return null;
}

DateTime? _readUtcDateTime(Map<String, dynamic> json, List<String> keys) {
  final s = _readString(json, keys);
  if (s == null) return null;
  final dt = DateTime.tryParse(s);
  return dt?.toLocal(); // Convert to local for display
}

List<dynamic>? _readList(Map<String, dynamic> json, List<String> keys) {
  for (final k in keys) {
    if (json.containsKey(k) && json[k] is List) {
      return json[k] as List<dynamic>;
    }
  }
  return null;
}

ExpenseStatus _parseStatus(String? statusStr) {
  if (statusStr == null) return ExpenseStatus.unknown;
  switch (statusStr.toLowerCase()) {
    case 'draft':
      return ExpenseStatus.draft;
    case 'submitted':
      return ExpenseStatus.submitted;
    case 'approved':
      return ExpenseStatus.approved;
    case 'rejected':
      return ExpenseStatus.rejected;
    default:
      return ExpenseStatus.unknown;
  }
}
