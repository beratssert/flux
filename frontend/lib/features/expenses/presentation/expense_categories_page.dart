import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/data/auth_session_controller.dart';
import '../data/expense_categories_controller.dart';

// ── Authorization note ────────────────────────────────────────────────────────
// Backend: POST   /api/v1/expense-categories    → [Authorize(Roles = "Admin")]
//          PATCH  /api/v1/expense-categories/id → [Authorize(Roles = "Admin")]
//          DELETE endpoint does NOT exist.
//          Deactivation (isActive: false via PATCH) is the only "removal" path.
// Frontend mirrors this: only Admin sees the Add / Edit buttons.
// ─────────────────────────────────────────────────────────────────────────────

class ExpenseCategoriesPage extends ConsumerWidget {
  const ExpenseCategoriesPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(expenseCategoriesControllerProvider);
    final authState = ref.watch(authSessionControllerProvider);
    final userRole = authState.session?.profile.role?.toLowerCase() ?? '';

    // Only Admin can mutate categories (backend enforces [Authorize(Roles="Admin")])
    final isAdmin = userRole == 'admin';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Gider Kategorileri'),
        leading: BackButton(
          onPressed: () =>
              context.canPop() ? context.pop() : context.go('/settings'),
        ),
        actions: [
          if (isAdmin)
            IconButton(
              icon: const Icon(Icons.add),
              onPressed: () => _showCategoryDialog(context, ref),
              tooltip: 'Kategori Ekle',
            ),
        ],
      ),
      body: state.isLoading && state.items.isEmpty
          ? const Center(child: CircularProgressIndicator())
          : state.error != null && state.items.isEmpty
              ? Center(child: Text('Hata: ${state.error}'))
              : state.items.isEmpty
                  ? const Center(child: Text('Henüz kategori yok.'))
                  : ListView.separated(
                      padding: const EdgeInsets.all(16),
                      itemCount: state.items.length,
                      separatorBuilder: (_, __) => const Divider(),
                      itemBuilder: (context, index) {
                        final category = state.items[index];
                        return ListTile(
                          title: Text(
                            category.name,
                            style: TextStyle(
                              decoration: category.isActive
                                  ? null
                                  : TextDecoration.lineThrough,
                              color: category.isActive ? null : Colors.grey,
                            ),
                          ),
                          subtitle: Row(
                            children: [
                              _StatusChip(isActive: category.isActive),
                            ],
                          ),
                          trailing: isAdmin
                              ? IconButton(
                                  icon: const Icon(Icons.edit_outlined),
                                  onPressed: () => _showCategoryDialog(
                                      context, ref,
                                      category: category),
                                  tooltip: 'Düzenle',
                                )
                              : null,
                        );
                      },
                    ),
    );
  }

  void _showCategoryDialog(BuildContext context, WidgetRef ref,
      {dynamic category}) {
    final nameController =
        TextEditingController(text: category?.name ?? '');
    bool isActive = category?.isActive ?? true;

    showDialog(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: Text(category == null ? 'Yeni Kategori' : 'Kategoriyi Düzenle'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: nameController,
                autofocus: true,
                decoration: const InputDecoration(
                  labelText: 'Kategori Adı',
                  hintText: 'Örn: Yemek, Ulaşım...',
                ),
                textCapitalization: TextCapitalization.sentences,
              ),
              // Show active toggle only for existing categories (deactivate = soft delete)
              if (category != null) ...[
                const SizedBox(height: 8),
                SwitchListTile(
                  title: const Text('Aktif'),
                  subtitle: const Text(
                      'Pasif kategoriler yeni harcamalarda görünmez'),
                  value: isActive,
                  onChanged: (val) => setState(() => isActive = val),
                  contentPadding: EdgeInsets.zero,
                ),
              ],
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('İptal'),
            ),
            FilledButton(
              onPressed: () async {
                final name = nameController.text.trim();
                if (name.isEmpty) return;
                try {
                  if (category == null) {
                    await ref
                        .read(expenseCategoriesControllerProvider.notifier)
                        .createCategory(name);
                  } else {
                    await ref
                        .read(expenseCategoriesControllerProvider.notifier)
                        .updateCategory(category.id, name, isActive);
                  }
                  if (context.mounted) Navigator.pop(context);
                } catch (e) {
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text('Hata: $e'),
                        backgroundColor: Colors.red,
                      ),
                    );
                  }
                }
              },
              child: const Text('Kaydet'),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.isActive});
  final bool isActive;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: isActive
            ? Colors.green.withOpacity(0.12)
            : Colors.grey.withOpacity(0.12),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isActive
              ? Colors.green.withOpacity(0.5)
              : Colors.grey.withOpacity(0.4),
        ),
      ),
      child: Text(
        isActive ? 'Aktif' : 'Pasif',
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: isActive ? Colors.green.shade700 : Colors.grey.shade600,
        ),
      ),
    );
  }
}
