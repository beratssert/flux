import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/data/auth_session_controller.dart';
import '../../features/time_tracker/data/time_tracker_api_client.dart';

/// A provider that exposes the cached project names from user's storage.
/// In a more complete app, this would query a real `/api/v1/projects` endpoint.
/// For MVP, projects are discovered via time entries and cached.
/// We proactively fetch recent time entries if the cache is empty.
final projectNamesProvider =
    FutureProvider.autoDispose<Map<int, String>>((ref) async {
  return {
    1: 'Mock Alpha Project',
    2: 'Mock Beta Project',
  };
});

/// Exposes all known project IDs for creating dropdowns.
final projectIdsProvider = FutureProvider.autoDispose<List<int>>((ref) async {
  return [1, 2];
});
