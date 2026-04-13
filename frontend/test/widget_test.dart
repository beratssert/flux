import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flux_app/features/auth/data/auth_models.dart';
import 'package:flux_app/features/projects/data/projects_api_client.dart';
import 'package:flux_app/features/projects/data/projects_models.dart';
import 'package:flux_app/features/projects/presentation/projects_page.dart';
import 'package:flux_app/features/shell/presentation/authenticated_shell.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('Projects UI', () {
    testWidgets('authenticated shell shows Projects tab for managers',
        (tester) async {
      final fakeClient = FakeProjectsApiClient(
        projectsPage: const ProjectsPage(
          items: [
            ProjectRecord(
              id: 1,
              name: 'Mobile Revamp',
              code: 'MOB-1',
              description: 'New app release',
              managerUserId: 'mgr-1',
              status: 'Active',
              startDate: null,
              endDate: null,
            ),
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
          hasNext: false,
          hasPrevious: false,
        ),
        projectDetail: const ProjectRecord(
          id: 1,
          name: 'Mobile Revamp',
          code: 'MOB-1',
          description: 'New app release',
          managerUserId: 'mgr-1',
          status: 'Active',
          startDate: null,
          endDate: null,
        ),
        assignments: [
          ProjectAssignmentRecord(
            userId: 'emp-1',
            assignedAtUtc: DateTime.utc(2026, 4, 10, 12),
            isActive: true,
          ),
        ],
        employeesPage: const UsersPage(
          items: [
            UserOption(
              id: 'emp-1',
              firstName: 'Ada',
              lastName: 'Lovelace',
              email: 'ada@example.com',
              role: 'Employee',
              isActive: true,
            ),
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
        ),
      );

      await _pumpDesktop(
        tester,
        ProviderScope(
          overrides: [
            projectsApiClientProvider.overrideWithValue(fakeClient),
          ],
          child: MaterialApp(
            home: AuthenticatedShell(
              session: _session(role: 'Manager'),
              initialIndex: 1,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Projects'), findsWidgets);
      expect(find.text('New project'), findsOneWidget);
      expect(find.text('Assign employee'), findsOneWidget);
      expect(find.text('Ada Lovelace'), findsOneWidget);
    });

    testWidgets('employee view stays read only', (tester) async {
      final fakeClient = FakeProjectsApiClient(
        projectsPage: const ProjectsPage(
          items: [
            ProjectRecord(
              id: 7,
              name: 'Client Portal',
              code: 'PORTAL',
              description: 'Self-service workspace',
              managerUserId: 'mgr-2',
              status: 'Archived',
              startDate: null,
              endDate: null,
            ),
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
          hasNext: false,
          hasPrevious: false,
        ),
        projectDetail: const ProjectRecord(
          id: 7,
          name: 'Client Portal',
          code: 'PORTAL',
          description: 'Self-service workspace',
          managerUserId: 'mgr-2',
          status: 'Archived',
          startDate: null,
          endDate: null,
        ),
        myAssignments: [
          MyProjectAssignmentRecord(
            projectId: 7,
            projectName: 'Client Portal',
            projectCode: 'PORTAL',
            projectStatus: 'Archived',
            assignedAtUtc: DateTime.utc(2026, 4, 10, 14, 30),
          ),
        ],
      );

      await _pumpDesktop(
        tester,
        ProviderScope(
          overrides: [
            projectsApiClientProvider.overrideWithValue(fakeClient),
          ],
          child: MaterialApp(
            home: ProjectsWorkspacePage(session: _session(role: 'Employee')),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Assigned on 2026-04-10'), findsOneWidget);
      expect(find.text('New project'), findsNothing);
      expect(find.text('Assign employee'), findsNothing);
      expect(find.text('Edit'), findsNothing);
    });

    testWidgets('shows loading then empty state', (tester) async {
      final fakeClient = FakeProjectsApiClient(
        projectsPage: const ProjectsPage(
          items: [],
          page: 1,
          pageSize: 20,
          totalCount: 0,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false,
        ),
        loadDelay: const Duration(milliseconds: 40),
      );

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            projectsApiClientProvider.overrideWithValue(fakeClient),
          ],
          child: MaterialApp(
            home: ProjectsWorkspacePage(session: _session(role: 'Manager')),
          ),
        ),
      );

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      await tester.pumpAndSettle();
      expect(find.text('No projects matched the current filters.'),
          findsOneWidget);
    });

    testWidgets('shows safe error state when list fetch fails', (tester) async {
      final fakeClient = FakeProjectsApiClient(
        projectsError: DioException(
          requestOptions: RequestOptions(path: '/api/v1/projects'),
          response: Response<dynamic>(
            requestOptions: RequestOptions(path: '/api/v1/projects'),
            statusCode: 403,
            data: <String, dynamic>{
              'detail': 'You are not allowed to access projects.',
            },
          ),
        ),
      );

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            projectsApiClientProvider.overrideWithValue(fakeClient),
          ],
          child: MaterialApp(
            home: ProjectsWorkspacePage(session: _session(role: 'Manager')),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(
          find.text('You are not allowed to access projects.'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);
    });
  });
}

Future<void> _pumpDesktop(WidgetTester tester, Widget child) async {
  tester.view.physicalSize = const Size(1440, 1024);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });
  await tester.pumpWidget(child);
}

AuthSession _session({required String role}) {
  return AuthSession(
    accessToken: 'token',
    expiresAtUtc: DateTime.utc(2030, 1, 1),
    roles: [role],
    profile: AuthProfile(
      id: role == 'Manager' ? 'mgr-1' : 'emp-1',
      email: '${role.toLowerCase()}@example.com',
      firstName: role,
      lastName: 'User',
      role: role,
      isActive: true,
    ),
  );
}

class FakeProjectsApiClient extends ProjectsApiClient {
  FakeProjectsApiClient({
    this.projectsPage,
    this.projectDetail,
    this.assignments = const <ProjectAssignmentRecord>[],
    this.myAssignments = const <MyProjectAssignmentRecord>[],
    this.employeesPage = const UsersPage(
      items: <UserOption>[],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    ),
    this.projectsError,
    this.loadDelay = Duration.zero,
  }) : super(accessToken: 'token');

  final ProjectsPage? projectsPage;
  final ProjectRecord? projectDetail;
  final List<ProjectAssignmentRecord> assignments;
  final List<MyProjectAssignmentRecord> myAssignments;
  final UsersPage employeesPage;
  final Object? projectsError;
  final Duration loadDelay;

  @override
  Future<ProjectsPage> getProjects({
    int page = 1,
    int pageSize = 20,
    String? status,
    String? managerUserId,
    String? query,
  }) async {
    if (loadDelay > Duration.zero) {
      await Future<void>.delayed(loadDelay);
    }
    if (projectsError != null) {
      throw projectsError!;
    }
    return projectsPage ??
        const ProjectsPage(
          items: <ProjectRecord>[],
          page: 1,
          pageSize: 20,
          totalCount: 0,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false,
        );
  }

  @override
  Future<ProjectRecord> getProjectById(int id) async {
    return projectDetail ??
        ProjectRecord(
          id: id,
          name: 'Project $id',
          code: null,
          description: null,
          managerUserId: 'mgr-1',
          status: 'Active',
          startDate: null,
          endDate: null,
        );
  }

  @override
  Future<List<ProjectAssignmentRecord>> getProjectAssignments(
      int projectId) async {
    return assignments;
  }

  @override
  Future<List<MyProjectAssignmentRecord>> getMyAssignments() async {
    return myAssignments;
  }

  @override
  Future<UsersPage> getEmployees({
    String? query,
    int page = 1,
    int pageSize = 20,
    int? projectId,
  }) async {
    return employeesPage;
  }
}
