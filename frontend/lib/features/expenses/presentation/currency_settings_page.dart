import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/data/auth_session_controller.dart';
import '../data/currency_settings_provider.dart';

// ── Authorization ──────────────────────────────────────────────────────────────
// Only Manager and Admin may manage the currency list.
// Admin is allowed as well (read-all, system management role).
// Employees see the page as read-only if they navigate here directly.
// The settings page only shows the item to Manager+ (see settings_page.dart).
// ──────────────────────────────────────────────────────────────────────────────

class CurrencySettingsPage extends ConsumerWidget {
  const CurrencySettingsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authSessionControllerProvider);
    final userRole = authState.session?.profile.role?.toLowerCase() ?? '';
    final canEdit = userRole == 'manager' || userRole == 'admin';

    final currenciesAsync = ref.watch(currencySettingsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Para Birimleri'),
        leading: BackButton(
          onPressed: () =>
              context.canPop() ? context.pop() : context.go('/settings'),
        ),
        actions: [
          if (canEdit)
            IconButton(
              icon: const Icon(Icons.add),
              tooltip: 'Para Birimi Ekle',
              onPressed: () => _showAddDialog(context, ref),
            ),
        ],
      ),
      body: currenciesAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Hata: $e')),
        data: (currencies) => currencies.isEmpty
            ? const Center(child: Text('Henüz para birimi eklenmedi.'))
            : _CurrencyList(
                currencies: currencies,
                canEdit: canEdit,
              ),
      ),
    );
  }

  void _showAddDialog(BuildContext context, WidgetRef ref) {
    final controller = TextEditingController();
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Para Birimi Ekle'),
        content: TextField(
          controller: controller,
          autofocus: true,
          textCapitalization: TextCapitalization.characters,
          maxLength: 10,
          decoration: const InputDecoration(
            labelText: 'Kod (örn: USD, TRY, GBP)',
            hintText: 'ISO 4217 kod',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('İptal'),
          ),
          FilledButton(
            onPressed: () async {
              final code = controller.text.trim().toUpperCase();
              if (code.isEmpty) return;
              await ref
                  .read(currencySettingsProvider.notifier)
                  .addCurrency(code);
              if (ctx.mounted) Navigator.pop(ctx);
            },
            child: const Text('Ekle'),
          ),
        ],
      ),
    );
  }
}

// ─── Reorderable list (only interactive when canEdit) ─────────────────────────

class _CurrencyList extends ConsumerWidget {
  const _CurrencyList({
    required this.currencies,
    required this.canEdit,
  });

  final List<String> currencies;
  final bool canEdit;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (canEdit)
          Padding(
            padding:
                const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Text(
              'Sürükleyerek sıralayabilir, kaydırarak silebilirsiniz.',
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.outline),
            ),
          ),
        Expanded(
          child: canEdit
              ? ReorderableListView.builder(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 16, vertical: 4),
                  itemCount: currencies.length,
                  onReorder: (oldIndex, newIndex) {
                    ref
                        .read(currencySettingsProvider.notifier)
                        .reorder(oldIndex, newIndex);
                  },
                  itemBuilder: (context, index) {
                    final code = currencies[index];
                    return _CurrencyTile(
                      key: ValueKey(code),
                      code: code,
                      canEdit: canEdit,
                      onDelete: () => _confirmDelete(context, ref, code),
                    );
                  },
                )
              : ListView.builder(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 16, vertical: 4),
                  itemCount: currencies.length,
                  itemBuilder: (context, index) => _CurrencyTile(
                    key: ValueKey(currencies[index]),
                    code: currencies[index],
                    canEdit: false,
                    onDelete: null,
                  ),
                ),
        ),
      ],
    );
  }

  void _confirmDelete(
      BuildContext context, WidgetRef ref, String code) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Para Birimini Kaldır'),
        content: Text(
            '"$code" para birimini listeden kaldırmak istiyor musunuz?\n\n'
            'Mevcut harcamalar etkilenmez, yalnızca yeni harcama oluştururken bu seçenek görünmez olur.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('İptal'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(
                backgroundColor: Colors.red.shade700),
            onPressed: () async {
              await ref
                  .read(currencySettingsProvider.notifier)
                  .removeCurrency(code);
              if (ctx.mounted) Navigator.pop(ctx);
            },
            child: const Text('Kaldır'),
          ),
        ],
      ),
    );
  }
}

// ─── Currency tile ─────────────────────────────────────────────────────────────

class _CurrencyTile extends StatelessWidget {
  const _CurrencyTile({
    super.key,
    required this.code,
    required this.canEdit,
    required this.onDelete,
  });

  final String code;
  final bool canEdit;
  final VoidCallback? onDelete;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: ListTile(
        leading: Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            color: theme.colorScheme.primaryContainer,
            borderRadius: BorderRadius.circular(8),
          ),
          alignment: Alignment.center,
          child: Text(
            code.length <= 3 ? code : code.substring(0, 3),
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w800,
              color: theme.colorScheme.onPrimaryContainer,
            ),
          ),
        ),
        title: Text(code, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(_currencyName(code),
            style: theme.textTheme.bodySmall),
        trailing: canEdit
            ? Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                    icon: const Icon(Icons.delete_outline),
                    color: Colors.red.shade400,
                    tooltip: 'Kaldır',
                    onPressed: onDelete,
                  ),
                  const Icon(Icons.drag_handle, color: Colors.grey),
                ],
              )
            : null,
      ),
    );
  }

  static String _currencyName(String code) {
    const names = {
      'USD': 'ABD Doları',
      'EUR': 'Euro',
      'TRY': 'Türk Lirası',
      'GBP': 'Sterlin',
      'JPY': 'Japon Yeni',
      'CHF': 'İsviçre Frangı',
      'CAD': 'Kanada Doları',
      'AUD': 'Avustralya Doları',
      'CNY': 'Çin Yuanı',
      'AED': 'BAE Dirhemi',
      'SAR': 'Suudi Riyali',
      'RUB': 'Rus Rublesi',
    };
    return names[code] ?? 'ISO 4217';
  }
}
