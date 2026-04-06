import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract class AuthStorage {
  Future<String?> readSession();
  Future<void> writeSession(String value);
  Future<void> clearSession();
  Future<List<int>> readKnownProjectIds();
  Future<void> writeKnownProjectIds(List<int> projectIds);
  Future<Map<int, String>> readKnownProjectNames();
  Future<void> writeKnownProjectNames(Map<int, String> projectNames);
}

class SecureAuthStorage implements AuthStorage {
  SecureAuthStorage([FlutterSecureStorage? storage])
      : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  static const String _sessionKey = 'flux.auth.session';
  static const String _projectIdsKey = 'flux.timeTracker.projectIds';
  static const String _projectNamesKey = 'flux.timeTracker.projectNames';

  @override
  Future<void> clearSession() async {
    await _storage.delete(key: _sessionKey);
  }

  @override
  Future<List<int>> readKnownProjectIds() async {
    final raw = await _storage.read(key: _projectIdsKey);
    return _decodeProjectIds(raw);
  }

  @override
  Future<Map<int, String>> readKnownProjectNames() async {
    final raw = await _storage.read(key: _projectNamesKey);
    return _decodeProjectNames(raw);
  }

  @override
  Future<String?> readSession() {
    return _storage.read(key: _sessionKey);
  }

  @override
  Future<void> writeKnownProjectIds(List<int> projectIds) {
    return _storage.write(
      key: _projectIdsKey,
      value: jsonEncode(projectIds.toSet().toList()..sort()),
    );
  }

  @override
  Future<void> writeKnownProjectNames(Map<int, String> projectNames) {
    final normalized = <String, String>{
      for (final entry in projectNames.entries)
        entry.key.toString(): entry.value.trim(),
    };

    return _storage.write(
      key: _projectNamesKey,
      value: jsonEncode(normalized),
    );
  }

  @override
  Future<void> writeSession(String value) {
    return _storage.write(key: _sessionKey, value: value);
  }
}

class MemoryAuthStorage implements AuthStorage {
  final Map<String, String> _memory = <String, String>{};

  static const String _sessionKey = 'flux.auth.session';
  static const String _projectIdsKey = 'flux.timeTracker.projectIds';
  static const String _projectNamesKey = 'flux.timeTracker.projectNames';

  @override
  Future<void> clearSession() async {
    _memory.remove(_sessionKey);
  }

  @override
  Future<List<int>> readKnownProjectIds() async {
    return _decodeProjectIds(_memory[_projectIdsKey]);
  }

  @override
  Future<Map<int, String>> readKnownProjectNames() async {
    return _decodeProjectNames(_memory[_projectNamesKey]);
  }

  @override
  Future<String?> readSession() async {
    return _memory[_sessionKey];
  }

  @override
  Future<void> writeKnownProjectIds(List<int> projectIds) async {
    _memory[_projectIdsKey] = jsonEncode(projectIds.toSet().toList()..sort());
  }

  @override
  Future<void> writeKnownProjectNames(Map<int, String> projectNames) async {
    _memory[_projectNamesKey] = jsonEncode(
      <String, String>{
        for (final entry in projectNames.entries)
          entry.key.toString(): entry.value.trim(),
      },
    );
  }

  @override
  Future<void> writeSession(String value) async {
    _memory[_sessionKey] = value;
  }
}

List<int> _decodeProjectIds(String? raw) {
  if (raw == null || raw.isEmpty) {
    return <int>[];
  }

  final decoded = jsonDecode(raw);
  if (decoded is! List) {
    return <int>[];
  }

  final values = decoded
      .map<int?>((dynamic item) {
        if (item is int) {
          return item;
        }
        if (item is String) {
          return int.tryParse(item);
        }
        return null;
      })
      .whereType<int>()
      .toSet()
      .toList()
    ..sort();

  return values;
}

Map<int, String> _decodeProjectNames(String? raw) {
  if (raw == null || raw.isEmpty) {
    return <int, String>{};
  }

  final decoded = jsonDecode(raw);
  if (decoded is! Map) {
    return <int, String>{};
  }

  final values = <int, String>{};
  for (final entry in decoded.entries) {
    final id = int.tryParse(entry.key.toString());
    final name = entry.value?.toString().trim() ?? '';
    if (id != null && name.isNotEmpty) {
      values[id] = name;
    }
  }

  return values;
}
