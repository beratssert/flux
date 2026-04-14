import 'dart:convert';
// ignore: avoid_web_libraries_in_flutter
import 'dart:html' as html;

import 'package:flutter_riverpod/flutter_riverpod.dart';

// ── Currency Settings Provider ─────────────────────────────────────────────────
//
// Persists the currency list in the browser's localStorage.
// No native plugin needed — works on Flutter Web without a full restart.
//
// Key: 'expense_currency_codes' → JSON-encoded List<String>
// Default: ['USD', 'EUR', 'TRY', 'GBP']
// ──────────────────────────────────────────────────────────────────────────────

const _kStorageKey = 'expense_currency_codes';
const _defaultCurrencies = ['USD', 'EUR', 'TRY', 'GBP'];

class CurrencySettingsNotifier extends AsyncNotifier<List<String>> {
  @override
  Future<List<String>> build() async {
    return _load();
  }

  List<String> _load() {
    try {
      final raw = html.window.localStorage[_kStorageKey];
      if (raw != null && raw.isNotEmpty) {
        final decoded = jsonDecode(raw) as List<dynamic>;
        final list = decoded.cast<String>();
        if (list.isNotEmpty) return list;
      }
    } catch (_) {
      // Corrupt data — fall back to defaults
    }
    // First run or corrupt — persist & return defaults
    _save(_defaultCurrencies);
    return List<String>.from(_defaultCurrencies);
  }

  void _save(List<String> codes) {
    html.window.localStorage[_kStorageKey] = jsonEncode(codes);
  }

  Future<void> addCurrency(String code) async {
    final current = List<String>.from(state.valueOrNull ?? []);
    final normalised = code.trim().toUpperCase();
    if (normalised.isEmpty || current.contains(normalised)) return;

    final updated = [...current, normalised];
    _save(updated);
    state = AsyncData(updated);
  }

  Future<void> removeCurrency(String code) async {
    final current = List<String>.from(state.valueOrNull ?? []);
    final updated = current.where((c) => c != code).toList();
    _save(updated);
    state = AsyncData(updated);
  }

  Future<void> reorder(int oldIndex, int newIndex) async {
    final current = <String>[...(state.valueOrNull ?? [])];
    if (newIndex > oldIndex) newIndex--;
    final item = current.removeAt(oldIndex);
    current.insert(newIndex, item);
    _save(current);
    state = AsyncData(current);
  }
}

final currencySettingsProvider =
    AsyncNotifierProvider<CurrencySettingsNotifier, List<String>>(
  CurrencySettingsNotifier.new,
);
