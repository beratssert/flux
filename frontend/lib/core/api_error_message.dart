import 'package:dio/dio.dart';

String describeApiError(
  Object error, {
  String fallback = 'Something went wrong.',
}) {
  if (error is DioException) {
    final responseData = error.response?.data;
    final normalizedData = _normalizeMap(responseData);

    if (normalizedData != null) {
      final errors = normalizedData['errors'];
      if (errors is Map) {
        final details = errors.entries
            .map((entry) {
              final value = entry.value;
              if (value is List) {
                return '${entry.key}: ${value.join(', ')}';
              }
              return '${entry.key}: $value';
            })
            .where((line) => line.trim().isNotEmpty)
            .join('\n');

        if (details.isNotEmpty) {
          final title = normalizedData['title']?.toString();
          return title == null || title.isEmpty ? details : '$title\n$details';
        }
      }

      for (final key in const ['detail', 'message', 'Message', 'title']) {
        final value = normalizedData[key];
        if (value is String && value.trim().isNotEmpty) {
          return value.trim();
        }
      }
    }

    if (responseData is String && responseData.trim().isNotEmpty) {
      return responseData.trim();
    }

    if (error.message != null && error.message!.trim().isNotEmpty) {
      return error.message!.trim();
    }

    return fallback;
  }

  final message = error.toString().trim();
  if (message.startsWith('Exception: ')) {
    return message.substring('Exception: '.length).trim();
  }
  if (message.startsWith('Unsupported operation: ')) {
    return message.substring('Unsupported operation: '.length).trim();
  }

  return message.isEmpty ? fallback : message;
}

Map<String, dynamic>? _normalizeMap(dynamic value) {
  if (value is Map<String, dynamic>) {
    return value;
  }
  if (value is Map) {
    return value.map(
      (key, dynamic item) => MapEntry(key.toString(), item),
    );
  }
  return null;
}
