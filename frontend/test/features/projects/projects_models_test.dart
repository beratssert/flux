import 'package:flutter_test/flutter_test.dart';
import 'package:flux_app/features/projects/data/projects_models.dart';

void main() {
  group('Projects models', () {
    test('parses paged projects from PascalCase payloads', () {
      final page = ProjectsPage.fromJson(<String, dynamic>{
        'Items': [
          <String, dynamic>{
            'Id': 42,
            'Name': 'Mobile Revamp',
            'Code': 'MOB-2026',
            'Description': 'Refresh the app',
            'ManagerUserId': 'mgr-1',
            'Status': 'Active',
            'StartDate': '2026-03-01',
            'EndDate': '2026-06-30',
          },
        ],
        'Page': 2,
        'PageSize': 25,
        'TotalCount': 51,
        'TotalPages': 3,
        'HasNext': true,
        'HasPrevious': true,
      });

      expect(page.items, hasLength(1));
      expect(page.items.first.id, 42);
      expect(page.items.first.code, 'MOB-2026');
      expect(page.page, 2);
      expect(page.totalPages, 3);
      expect(page.hasNext, isTrue);
      expect(page.hasPrevious, isTrue);
    });

    test('parses assignments and users from camelCase payloads', () {
      final assignment = MyProjectAssignmentRecord.fromJson(<String, dynamic>{
        'projectId': 7,
        'projectName': 'Client Portal',
        'projectCode': 'PORTAL',
        'projectStatus': 'Archived',
        'assignedAtUtc': '2026-04-10T14:30:00Z',
      });

      final usersPage = UsersPage.fromJson(<String, dynamic>{
        'items': [
          <String, dynamic>{
            'id': 'emp-1',
            'firstName': 'Ada',
            'lastName': 'Lovelace',
            'email': 'ada@example.com',
            'role': 'Employee',
            'isActive': true,
          },
        ],
        'page': 1,
        'pageSize': 20,
        'totalCount': 1,
      });

      expect(assignment.projectId, 7);
      expect(assignment.projectStatus, 'Archived');
      expect(usersPage.items.single.displayName, 'Ada Lovelace');
      expect(usersPage.items.single.isActive, isTrue);
    });
  });
}
