import re

with open('frontend/lib/features/calendar/data/services/calendar_service.dart', 'r') as f:
    service_content = f.read()

# Replace endpoint case
service_content = service_content.replace('/api/v1/TimeEntries', '/api/v1/time-entries')

# Handle team method if it exists
# We will just remove getTeamTimeEntries to be clean
service_content = re.sub(r'  Future<List<TimeEntry>> getTeamTimeEntries\(DateTime from, DateTime to\) async \{.*?\n  \}\n\n', '', service_content, flags=re.DOTALL)

# Ensure query parameters are lower-case and UTC as per doc:
# - from
# - to
# also `.toUtc().toIso8601String()`
getTimeEntries_replacement = """  Future<List<TimeEntry>> getTimeEntries(DateTime from, DateTime to) async {
    try {
      final response = await _dio.get('/api/v1/time-entries', queryParameters: {
        'from': from.toUtc().toIso8601String(),
        'to': to.toUtc().toIso8601String(),
      });"""
service_content = re.sub(r'  Future<List<TimeEntry>> getTimeEntries\(DateTime from, DateTime to\) async \{\n\s+try \{\n\s+final response = await _dio\.get\(\'/api/v1/time-entries\', queryParameters: \{\n\s+\'From\': from\.toIso8601String\(\),\n\s+\'To\': to\.toIso8601String\(\),\n\s+\}\);', getTimeEntries_replacement, service_content, flags=re.DOTALL)

with open('frontend/lib/features/calendar/data/services/calendar_service.dart', 'w') as f:
    f.write(service_content)

with open('frontend/lib/features/calendar/presentation/providers/calendar_provider.dart', 'r') as f:
    provider_content = f.read()

provider_fetch = """  Future<void> fetchEvents(DateTime from, DateTime to,
      {bool silent = false}) async {
    if (!silent) {
      state = state.copyWith(isLoading: true);
    }
    try {
      final data = await _service.getTimeEntries(from, to);
      state = state.copyWith(entries: data, isLoading: false);
    } catch (e) {
      state = state.copyWith(isLoading: false);
    }
  }"""

provider_content = re.sub(r'  Future<void> fetchEvents\(DateTime from, DateTime to,\n\s+\{bool silent = false\}\) async \{.*?\n\s+\}\n\s+\}\n\s+catch \(e\) \{\n\s+state = state\.copyWith\(isLoading: false\);\n\s+\}\n\s+\}', provider_fetch, provider_content, flags=re.DOTALL)

with open('frontend/lib/features/calendar/presentation/providers/calendar_provider.dart', 'w') as f:
    f.write(provider_content)

