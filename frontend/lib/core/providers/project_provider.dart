import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/projects/data/projects_api_client.dart';

/// A provider that exposes the cached project names from user's assignments.
final projectNamesProvider =
    FutureProvider.autoDispose<Map<int, String>>((ref) async {
  final client = ref.watch(projectsApiClientProvider);
  final assignments = await client.getMyAssignments();
  
  final map = <int, String>{};
  for (final assignment in assignments) {
    if (assignment.projectStatus != 'Archived') {
      map[assignment.projectId] = assignment.projectName;
    }
  }
  return map;
});

/// Exposes all known project IDs for creating dropdowns.
final projectIdsProvider = FutureProvider.autoDispose<List<int>>((ref) async {
  final client = ref.watch(projectsApiClientProvider);
  final assignments = await client.getMyAssignments();
  return assignments
      .where((a) => a.projectStatus != 'Archived')
      .map((a) => a.projectId)
      .toList();
});
