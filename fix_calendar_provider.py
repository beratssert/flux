import re

with open('frontend/lib/features/calendar/presentation/providers/calendar_provider.dart', 'r') as f:
    content = f.read()

# 1. Remove calendarServiceProvider duplication since we moved it to the service file.
content = re.sub(r'// Service Provider\nfinal calendarServiceProvider = Provider<CalendarService>\(\(ref\) \{\n  return CalendarService\(\);\n\}\);\n', '', content, flags=re.DOTALL)

# 2. Modify _initAndFetch to use the role/id from AuthSessionController instead of token decoding.
# First, we need to inject the Ref or authSession state into the Notifier.
# The cleanest way is to pass role and userId to the Notifier constructor.

replacement_notifier_init = """// Notifier
class CalendarNotifier extends StateNotifier<CalendarState> {
  final CalendarService _service;

  CalendarNotifier(this._service, {required String role, required String userId}) 
      : super(CalendarState(role: role, userId: userId)) {
    _initAndFetch();
  }

  Future<void> _initAndFetch() async {
    state = state.copyWith(isLoading: true);
    
    // 2. Rol bazlı veri çek
    await fetchEvents(DateTime.now().subtract(const Duration(days: 30)),
        DateTime.now().add(const Duration(days: 30)));
  }"""
content = re.sub(r'// Notifier\nclass CalendarNotifier extends StateNotifier<CalendarState> \{.*?\n\s+Future<void> _initAndFetch\(\) async \{.*?\s+// 2\. Rol bazlı veri çek\n\s+await fetchEvents\(DateTime\.now\(\)\.subtract\(const Duration\(days: 30\)\),\n\s+DateTime\.now\(\)\.add\(const Duration\(days: 30\)\)\);\n\s+\}', replacement_notifier_init, content, flags=re.DOTALL)

# 3. Update the provider to pass role and userId, and watch AuthSessionController
provider_replacement = """import '../../../auth/data/auth_session_controller.dart';

final calendarNotifierProvider =
    StateNotifierProvider<CalendarNotifier, CalendarState>((ref) {
  final service = ref.watch(calendarServiceProvider);
  final authState = ref.watch(authSessionControllerProvider);
  final profile = authState.session?.profile;
  
  return CalendarNotifier(
    service, 
    role: profile?.role ?? 'Employee', 
    userId: profile?.id ?? '',
  );
});"""
content = re.sub(r'final calendarNotifierProvider =\n\s+StateNotifierProvider<CalendarNotifier, CalendarState>\(\(ref\) \{.*?\}\);', provider_replacement, content, flags=re.DOTALL)

with open('frontend/lib/features/calendar/presentation/providers/calendar_provider.dart', 'w') as f:
    f.write(content)

