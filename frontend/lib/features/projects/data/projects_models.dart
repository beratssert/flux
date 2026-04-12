class ProjectRecord {
  const ProjectRecord({
    required this.id,
    required this.name,
    required this.code,
    required this.description,
    required this.managerUserId,
    required this.status,
    this.startDate,
    this.endDate,
  });

  final int id;
  final String name;
  final String? code;
  final String? description;
  final String managerUserId;
  final String status;
  final DateTime? startDate;
  final DateTime? endDate;

  factory ProjectRecord.fromJson(Map<String, dynamic> json) {
    return ProjectRecord(
      id: _readInt(json, const ['id', 'Id']) ?? 0,
      name: _readString(json, const ['name', 'Name']) ?? '',
      code: _readString(json, const ['code', 'Code']),
      description: _readString(json, const ['description', 'Description']),
      managerUserId:
          _readString(json, const ['managerUserId', 'ManagerUserId']) ?? '',
      status: _readString(json, const ['status', 'Status']) ?? 'Active',
      startDate: _readDate(json, const ['startDate', 'StartDate']),
      endDate: _readDate(json, const ['endDate', 'EndDate']),
    );
  }
}

class ProjectsPage {
  const ProjectsPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
    required this.hasNext,
    required this.hasPrevious,
  });

  final List<ProjectRecord> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;
  final bool hasNext;
  final bool hasPrevious;

  factory ProjectsPage.fromJson(Map<String, dynamic> json) {
    final rawItems =
        json['items'] ?? json['Items'] ?? json['data'] ?? json['Data'];
    final items = rawItems is List
        ? rawItems
            .whereType<Map>()
            .map(
              (item) => ProjectRecord.fromJson(
                item.map(
                  (key, dynamic value) => MapEntry(key.toString(), value),
                ),
              ),
            )
            .toList(growable: false)
        : const <ProjectRecord>[];

    final page = _readInt(json, const ['page', 'Page', 'pageNumber']) ?? 1;
    final pageSize =
        _readInt(json, const ['pageSize', 'PageSize']) ?? items.length;
    final totalCount =
        _readInt(json, const ['totalCount', 'TotalCount']) ?? items.length;
    final totalPages = _readInt(json, const ['totalPages', 'TotalPages']) ??
        (pageSize == 0 ? 0 : (totalCount / pageSize).ceil());

    return ProjectsPage(
      items: items,
      page: page,
      pageSize: pageSize,
      totalCount: totalCount,
      totalPages: totalPages,
      hasNext: _readBool(json, const ['hasNext', 'HasNext']) ??
          (totalPages > 0 && page < totalPages),
      hasPrevious:
          _readBool(json, const ['hasPrevious', 'HasPrevious']) ?? page > 1,
    );
  }
}

class ProjectAssignmentRecord {
  const ProjectAssignmentRecord({
    required this.userId,
    required this.assignedAtUtc,
    required this.isActive,
  });

  final String userId;
  final DateTime assignedAtUtc;
  final bool isActive;

  factory ProjectAssignmentRecord.fromJson(Map<String, dynamic> json) {
    return ProjectAssignmentRecord(
      userId: _readString(json, const ['userId', 'UserId']) ?? '',
      assignedAtUtc: _readUtcDateTime(
            json,
            const ['assignedAtUtc', 'AssignedAtUtc'],
          ) ??
          DateTime.now().toUtc(),
      isActive: _readBool(json, const ['isActive', 'IsActive']) ?? true,
    );
  }
}

class MyProjectAssignmentRecord {
  const MyProjectAssignmentRecord({
    required this.projectId,
    required this.projectName,
    required this.projectCode,
    required this.projectStatus,
    required this.assignedAtUtc,
  });

  final int projectId;
  final String projectName;
  final String? projectCode;
  final String projectStatus;
  final DateTime assignedAtUtc;

  factory MyProjectAssignmentRecord.fromJson(Map<String, dynamic> json) {
    return MyProjectAssignmentRecord(
      projectId: _readInt(json, const ['projectId', 'ProjectId']) ?? 0,
      projectName:
          _readString(json, const ['projectName', 'ProjectName']) ?? '',
      projectCode: _readString(json, const ['projectCode', 'ProjectCode']),
      projectStatus:
          _readString(json, const ['projectStatus', 'ProjectStatus']) ??
              'Active',
      assignedAtUtc: _readUtcDateTime(
            json,
            const ['assignedAtUtc', 'AssignedAtUtc'],
          ) ??
          DateTime.now().toUtc(),
    );
  }
}

class UserOption {
  const UserOption({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.role,
    required this.isActive,
    this.lastLoginAtUtc,
  });

  final String id;
  final String? firstName;
  final String? lastName;
  final String email;
  final String? role;
  final bool isActive;
  final DateTime? lastLoginAtUtc;

  String get displayName {
    final fullName = [
      if (firstName != null && firstName!.trim().isNotEmpty) firstName!.trim(),
      if (lastName != null && lastName!.trim().isNotEmpty) lastName!.trim(),
    ].join(' ');

    return fullName.isEmpty ? email : fullName;
  }

  factory UserOption.fromJson(Map<String, dynamic> json) {
    return UserOption(
      id: _readString(json, const ['id', 'Id']) ?? '',
      firstName: _readString(json, const ['firstName', 'FirstName']),
      lastName: _readString(json, const ['lastName', 'LastName']),
      email: _readString(json, const ['email', 'Email']) ?? '',
      role: _readString(json, const ['role', 'Role']),
      isActive: _readBool(json, const ['isActive', 'IsActive']) ?? true,
      lastLoginAtUtc: _readUtcDateTime(
        json,
        const ['lastLoginAtUtc', 'LastLoginAtUtc'],
      ),
    );
  }
}

class UsersPage {
  const UsersPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });

  final List<UserOption> items;
  final int page;
  final int pageSize;
  final int totalCount;

  factory UsersPage.fromJson(Map<String, dynamic> json) {
    final rawItems =
        json['items'] ?? json['Items'] ?? json['data'] ?? json['Data'];
    final items = rawItems is List
        ? rawItems
            .whereType<Map>()
            .map(
              (item) => UserOption.fromJson(
                item.map(
                  (key, dynamic value) => MapEntry(key.toString(), value),
                ),
              ),
            )
            .toList(growable: false)
        : const <UserOption>[];

    return UsersPage(
      items: items,
      page: _readInt(json, const ['page', 'Page', 'pageNumber']) ?? 1,
      pageSize: _readInt(json, const ['pageSize', 'PageSize']) ?? items.length,
      totalCount:
          _readInt(json, const ['totalCount', 'TotalCount']) ?? items.length,
    );
  }
}

String? _readString(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is String) {
      return value;
    }
  }
  return null;
}

int? _readInt(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is int) {
      return value;
    }
    if (value is String) {
      return int.tryParse(value);
    }
  }
  return null;
}

bool? _readBool(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is bool) {
      return value;
    }
  }
  return null;
}

DateTime? _readUtcDateTime(Map<String, dynamic> json, List<String> keys) {
  final value = _readString(json, keys);
  if (value == null || value.isEmpty) {
    return null;
  }

  final parsed = DateTime.tryParse(value);
  if (parsed == null) {
    return null;
  }

  final hasExplicitOffset =
      value.endsWith('Z') || RegExp(r'[+-]\d{2}:\d{2}$').hasMatch(value);

  if (hasExplicitOffset) {
    return parsed.toUtc();
  }

  return DateTime.utc(
    parsed.year,
    parsed.month,
    parsed.day,
    parsed.hour,
    parsed.minute,
    parsed.second,
    parsed.millisecond,
    parsed.microsecond,
  );
}

DateTime? _readDate(Map<String, dynamic> json, List<String> keys) {
  final value = _readString(json, keys);
  if (value == null || value.isEmpty) {
    return null;
  }

  final parsed = DateTime.tryParse(value);
  if (parsed == null) {
    return null;
  }

  return DateTime(parsed.year, parsed.month, parsed.day);
}
