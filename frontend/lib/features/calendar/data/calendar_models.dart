class CalendarItemRecord {
  const CalendarItemRecord({
    required this.itemType,
    required this.id,
    required this.title,
    required this.description,
    required this.startUtc,
    required this.endUtc,
    required this.allDay,
    this.projectId,
    this.visibility,
    this.durationMinutes,
    this.isBillable,
  });

  final String itemType; // "Event" or "TimeEntry"
  final int id;
  final String title;
  final String description;
  final DateTime startUtc;
  final DateTime endUtc;
  final bool allDay;
  final int? projectId;
  final String? visibility;
  final int? durationMinutes;
  final bool? isBillable;

  bool get isTimeEntry => itemType == 'TimeEntry';
  bool get isEvent => itemType == 'Event';

  factory CalendarItemRecord.fromJson(Map<String, dynamic> json) {
    return CalendarItemRecord(
      itemType: _readString(json, const ['itemType', 'ItemType']) ?? 'Event',
      id: _readInt(json, const ['id', 'Id']) ?? 0,
      title: _readString(json, const ['title', 'Title']) ?? '',
      description: _readString(json, const ['description', 'Description']) ?? '',
      startUtc: _readUtcDateTime(json, const ['startUtc', 'StartUtc']) ??
          DateTime.now().toUtc(),
      endUtc: _readUtcDateTime(json, const ['endUtc', 'EndUtc']) ??
          DateTime.now().toUtc(),
      allDay: _readBool(json, const ['allDay', 'AllDay']) ?? false,
      projectId: _readInt(json, const ['projectId', 'ProjectId']),
      visibility: _readString(json, const ['visibility', 'Visibility']),
      durationMinutes:
          _readInt(json, const ['durationMinutes', 'DurationMinutes']),
      isBillable: _readBool(json, const ['isBillable', 'IsBillable']),
    );
  }
}

class CreateCalendarEventRequest {
  const CreateCalendarEventRequest({
    this.projectId,
    required this.title,
    this.description = '',
    required this.startUtc,
    required this.endUtc,
    this.allDay = false,
    this.visibility = 'Project',
  });

  final int? projectId;
  final String title;
  final String description;
  final DateTime startUtc;
  final DateTime endUtc;
  final bool allDay;
  final String visibility;

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      if (projectId != null) 'projectId': projectId,
      'title': title,
      'description': description,
      'startUtc': startUtc.toUtc().toIso8601String(),
      'endUtc': endUtc.toUtc().toIso8601String(),
      'allDay': allDay,
      'visibility': visibility,
    };
  }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

String? _readString(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is String) return value;
  }
  return null;
}

int? _readInt(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is int) return value;
    if (value is String) return int.tryParse(value);
  }
  return null;
}

bool? _readBool(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is bool) return value;
  }
  return null;
}

DateTime? _readUtcDateTime(Map<String, dynamic> json, List<String> keys) {
  final value = _readString(json, keys);
  if (value == null || value.isEmpty) return null;

  final parsed = DateTime.tryParse(value);
  if (parsed == null) return null;

  final hasExplicitOffset =
      value.endsWith('Z') || RegExp(r'[+-]\d{2}:\d{2}$').hasMatch(value);

  if (hasExplicitOffset) return parsed.toUtc();

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
