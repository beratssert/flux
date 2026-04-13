import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/data/auth_session_controller.dart';
import '../data/expense_categories_controller.dart';

class ExpenseCategoriesPage extends ConsumerWidget {
  const ExpenseCategoriesPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(expenseCategoriesControllerProvider);
    final authState = ref.watch(authSessionControllerProvider);
    final userRole = authState.session?.profile.role?.toLowerCase() ?? '';

    final canEditOrDelete = userRole == 'admin' || userRole == 'manager';
    final canCreate = userRole == 'admin' || userRole == 'manager' || userRole == 'employee';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Gider Kategorileri'),
        leading: BackButton(onPressed: () => context.canPop() ? context.pop() : context.go('/settings')),
        actions: [
          if (canCreate)
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
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: state.items.length,
                  separatorBuilder: (context, index) => const Divider(),
                  itemBuilder: (context, index) {
                    final category = state.items[index];
                    return ListTile(
                      title: Text(
                        category.name,
                        style: TextStyle(
                          decoration: category.isActive ? null : TextDecoration.lineThrough,
                          color: category.isActive ? null : Colors.grey,
                        ),
                      ),
                      subtitle: Text(category.isActive ? 'Aktif' : 'Pasif'),
                      trailing: canEditOrDelete
                          ? Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                IconButton(
                                  icon: const Icon(Icons.edit_outlined),
                                  onPressed: () => _showCategoryDialog(context, ref, category: category),
                                  tooltip: 'Düzenle',
                                ),
                                IconButton(
                                  icon: const Icon(Icons.delete_outline, color: Colors.grey),
                                  onPressed: () => _showDeleteNote(context),
                                  tooltip: 'Sil (Backend Notu)',
                                ),
                              ],
                            )
                          : null,
                    );
                  },
                ),
    );
  }

  void _showDeleteNote(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.warning_amber_rounded, color: Colors.orange),
            SizedBox(width: 8),
            Text('Backend Geliştirici Notu'),
          ],
        ),
        content: const Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Bu özellik şu an kullanılamıyor. Backend tarafında '
              '"DELETE /api/v1/expense-categories/{id}" endpoint\'i henüz eklenmedi.',
            ),
            SizedBox(height: 12),
            Text('⚠️ Önemli Not:', style: TextStyle(fontWeight: FontWeight.bold)),
            SizedBox(height: 4),
            Text(
              'Gider kategorileri, projelerle many-to-one (çoktan-teke) ilişki içinde olabilir. '
              'Silme endpoint\'i eklenirken bu ilişki ve kaskad davranışı göz önünde bulundurulmalıdır.',
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Anladım'),
          ),
        ],
      ),
    );
  }

  void _showCategoryDialog(BuildContext context, WidgetRef ref, {dynamic category}) {
    final nameController = TextEditingController(text: category?.name ?? '');
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
                decoration: const InputDecoration(
                  labelText: 'Kategori Adı',
                  hintText: 'Örn: Yemek, Ulaşım...',
                ),
              ),
              if (category != null)
                SwitchListTile(
                  title: const Text('Aktif'),
                  value: isActive,
                  onChanged: (val) => setState(() => isActive = val),
                  contentPadding: EdgeInsets.zero,
                ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('İptal'),
            ),
            ElevatedButton(
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
                      SnackBar(content: Text('Hata: $e')),
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
