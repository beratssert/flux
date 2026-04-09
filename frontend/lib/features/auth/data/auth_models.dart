class AuthProfile {
  const AuthProfile({
    required this.id,
    required this.email,
    this.userName,
    this.firstName,
    this.lastName,
    this.role,
    this.isActive = true,
    this.lastLoginAtUtc,
  });

  final String id;
  final String email;
  final String? userName;
  final String? firstName;
  final String? lastName;
  final String? role;
  final bool isActive;
  final DateTime? lastLoginAtUtc;

  String get displayName {
    final fullName = [
      if (firstName != null && firstName!.trim().isNotEmpty) firstName!.trim(),
      if (lastName != null && lastName!.trim().isNotEmpty) lastName!.trim(),
    ].join(' ');

    if (fullName.isNotEmpty) {
      return fullName;
    }
    if (userName != null && userName!.trim().isNotEmpty) {
      return userName!.trim();
    }
    return email;
  }

  AuthProfile copyWith({
    String? id,
    String? email,
    String? userName,
    String? firstName,
    String? lastName,
    String? role,
    bool? isActive,
    DateTime? lastLoginAtUtc,
  }) {
    return AuthProfile(
      id: id ?? this.id,
      email: email ?? this.email,
      userName: userName ?? this.userName,
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      role: role ?? this.role,
      isActive: isActive ?? this.isActive,
      lastLoginAtUtc: lastLoginAtUtc ?? this.lastLoginAtUtc,
    );
  }

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'id': id,
      'email': email,
      'userName': userName,
      'firstName': firstName,
      'lastName': lastName,
      'role': role,
      'isActive': isActive,
      'lastLoginAtUtc': lastLoginAtUtc?.toIso8601String(),
    };
  }

  factory AuthProfile.fromJson(Map<String, dynamic> json) {
    return AuthProfile(
      id: _readString(json, const ['id', 'Id']) ?? '',
      email: _readString(json, const ['email', 'Email']) ?? '',
      userName: _readString(
        json,
        const [
          'userName',
          'UserName',
          'displayName',
          'DisplayName',
          'fullName',
          'FullName',
          'name',
          'Name',
        ],
      ),
      firstName: _readString(json, const ['firstName', 'FirstName']),
      lastName: _readString(json, const ['lastName', 'LastName']),
      role: _readString(json, const ['role', 'Role']),
      isActive: _readBool(json, const ['isActive', 'IsActive']) ?? true,
      lastLoginAtUtc: _readDateTime(
        json,
        const ['lastLoginAtUtc', 'LastLoginAtUtc'],
      ),
    );
  }

  factory AuthProfile.fromAuthPayload(Map<String, dynamic> json) {
    final roles = _readStringList(json, const ['roles', 'Roles']);

    return AuthProfile(
      id: _readString(json, const ['id', 'Id']) ?? '',
      email: _readString(json, const ['email', 'Email']) ?? '',
      userName: _readString(json, const ['userName', 'UserName']),
      role: roles.isNotEmpty ? roles.first : null,
      isActive: true,
    );
  }
}

class AuthSession {
  const AuthSession({
    required this.accessToken,
    required this.expiresAtUtc,
    required this.roles,
    required this.profile,
  });

  final String accessToken;
  final DateTime expiresAtUtc;
  final List<String> roles;
  final AuthProfile profile;

  bool get isExpired => expiresAtUtc.isBefore(DateTime.now().toUtc());

  AuthSession copyWith({
    String? accessToken,
    DateTime? expiresAtUtc,
    List<String>? roles,
    AuthProfile? profile,
  }) {
    return AuthSession(
      accessToken: accessToken ?? this.accessToken,
      expiresAtUtc: expiresAtUtc ?? this.expiresAtUtc,
      roles: roles ?? this.roles,
      profile: profile ?? this.profile,
    );
  }

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'accessToken': accessToken,
      'expiresAtUtc': expiresAtUtc.toIso8601String(),
      'roles': roles,
      'profile': profile.toJson(),
    };
  }

  factory AuthSession.fromJson(Map<String, dynamic> json) {
    final profileValue = json['profile'];

    return AuthSession(
      accessToken:
          _readString(json, const ['accessToken', 'AccessToken']) ?? '',
      expiresAtUtc: _readDateTime(
            json,
            const ['expiresAtUtc', 'ExpiresAtUtc'],
          ) ??
          DateTime.now().toUtc(),
      roles: _readStringList(json, const ['roles', 'Roles']),
      profile: AuthProfile.fromJson(
        profileValue is Map<String, dynamic>
            ? profileValue
            : profileValue is Map
                ? profileValue.map(
                    (key, dynamic value) => MapEntry(key.toString(), value),
                  )
                : const <String, dynamic>{},
      ),
    );
  }

  factory AuthSession.fromAuthResponse(Map<String, dynamic> json) {
    return AuthSession(
      accessToken: _readString(
            json,
            const ['accessToken', 'AccessToken', 'jwToken', 'JWToken'],
          ) ??
          '',
      expiresAtUtc: _readDateTime(
            json,
            const ['expiresAtUtc', 'ExpiresAtUtc'],
          ) ??
          DateTime.now().toUtc(),
      roles: _readStringList(json, const ['roles', 'Roles']),
      profile: AuthProfile.fromAuthPayload(json),
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

bool? _readBool(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is bool) {
      return value;
    }
  }
  return null;
}

DateTime? _readDateTime(Map<String, dynamic> json, List<String> keys) {
  final raw = _readString(json, keys);
  if (raw == null || raw.isEmpty) {
    return null;
  }
  return DateTime.tryParse(raw)?.toUtc();
}

List<String> _readStringList(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is List) {
      return value
          .map((item) => item?.toString() ?? '')
          .where((item) => item.isNotEmpty)
          .toList(growable: false);
    }
  }
  return const <String>[];
}
