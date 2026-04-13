import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flux_app/features/projects/data/projects_api_client.dart';

void main() {
  group('ProjectsApiClient', () {
    test('builds project list request and parses paged response', () async {
      RequestOptions? captured;
      final dio = Dio(BaseOptions(baseUrl: 'http://localhost'))
        ..interceptors.add(
          InterceptorsWrapper(
            onRequest: (options, handler) {
              captured = options;
              handler.resolve(
                Response<dynamic>(
                  requestOptions: options,
                  data: <String, dynamic>{
                    'Items': [
                      <String, dynamic>{
                        'Id': 9,
                        'Name': 'Ops Hub',
                        'ManagerUserId': 'mgr-1',
                        'Status': 'Active',
                      },
                    ],
                    'Page': 2,
                    'PageSize': 10,
                    'TotalCount': 11,
                    'TotalPages': 2,
                    'HasNext': false,
                    'HasPrevious': true,
                  },
                ),
              );
            },
          ),
        );

      final client = ProjectsApiClient(accessToken: 'token', dio: dio);
      final page = await client.getProjects(
        page: 2,
        pageSize: 10,
        status: 'Active',
        query: 'ops',
      );

      expect(captured?.path, '/api/v1/projects');
      expect(captured?.headers['Authorization'], 'Bearer token');
      expect(captured?.queryParameters['page'], 2);
      expect(captured?.queryParameters['pageSize'], 10);
      expect(captured?.queryParameters['status'], 'Active');
      expect(captured?.queryParameters['q'], 'ops');
      expect(page.items.single.id, 9);
      expect(page.hasPrevious, isTrue);
    });

    test('sends status patch payload to backend contract', () async {
      RequestOptions? captured;
      final dio = Dio(BaseOptions(baseUrl: 'http://localhost'))
        ..interceptors.add(
          InterceptorsWrapper(
            onRequest: (options, handler) {
              captured = options;
              handler.resolve(
                Response<dynamic>(
                  requestOptions: options,
                  data: <String, dynamic>{
                    'id': 4,
                    'name': 'North Star',
                    'managerUserId': 'mgr-1',
                    'status': 'Closed',
                  },
                ),
              );
            },
          ),
        );

      final client = ProjectsApiClient(accessToken: 'token', dio: dio);
      final project = await client.updateProjectStatus(id: 4, status: 'Closed');

      expect(captured?.method, 'PATCH');
      expect(captured?.path, '/api/v1/projects/4/status');
      expect(captured?.data, <String, dynamic>{'status': 'Closed'});
      expect(project.status, 'Closed');
    });

    test('sends employee lookup filters for assignment search', () async {
      RequestOptions? captured;
      final dio = Dio(BaseOptions(baseUrl: 'http://localhost'))
        ..interceptors.add(
          InterceptorsWrapper(
            onRequest: (options, handler) {
              captured = options;
              handler.resolve(
                Response<dynamic>(
                  requestOptions: options,
                  data: <String, dynamic>{
                    'items': [
                      <String, dynamic>{
                        'id': 'emp-1',
                        'email': 'ada@example.com',
                        'firstName': 'Ada',
                        'lastName': 'Lovelace',
                        'role': 'Employee',
                        'isActive': true,
                      },
                    ],
                    'page': 1,
                    'pageSize': 20,
                    'totalCount': 1,
                  },
                ),
              );
            },
          ),
        );

      final client = ProjectsApiClient(accessToken: 'token', dio: dio);
      final page =
          await client.getEmployees(query: 'ada', page: 1, pageSize: 20);

      expect(captured?.path, '/api/v1/users');
      expect(captured?.queryParameters['role'], 'Employee');
      expect(captured?.queryParameters['isActive'], true);
      expect(captured?.queryParameters['q'], 'ada');
      expect(page.items.single.email, 'ada@example.com');
    });
  });
}
