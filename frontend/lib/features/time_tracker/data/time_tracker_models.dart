class TimeEntryRecord {
  const TimeEntryRecord({
    required this.id,
    required this.projectId,
    required this.entryDate,
    required this.durationMinutes,
    required this.description,
    required this.isBillable,
    required this.sourceType,
    this.startTimeUtc,
    this.endTimeUtc,
  });

  final int id;
  final int projectId;
  final DateTime entryDate;
  final DateTime? startTimeUtc;
  final DateTime? endTimeUtc;
  final int durationMinutes;
  final String description;
  final bool isBillable;
  final String sourceType;

  factory TimeEntryRecord.fromJson(Map<String, dynamic> json) {
    return TimeEntryRecord(
      id: _readInt(json, const ['id', 'Id']) ?? 0,
      projectId: _readInt(json, const ['projectId', 'ProjectId']) ?? 0,
      entryDate: _readUtcDateTime(json, const ['entryDate', 'EntryDate']) ??
          DateTime.now().toUtc(),
      startTimeUtc: _readUtcDateTime(
        json,
        const ['startTimeUtc', 'StartTimeUtc'],
      ),
      endTimeUtc: _readUtcDateTime(
        json,
        const ['endTimeUtc', 'EndTimeUtc'],
      ),
      durationMinutes:
          _readInt(json, const ['durationMinutes', 'DurationMinutes']) ?? 0,
      description:
          _readString(json, const ['description', 'Description']) ?? '',
      isBillable: _readBool(json, const ['isBillable', 'IsBillable']) ?? false,
      sourceType:
          _readString(json, const ['sourceType', 'SourceType']) ?? 'Manual',
    );
  }
}

class RunningTimerRecord {
  const RunningTimerRecord({
    required this.id,
    required this.projectId,
    required this.startedAtUtc,
    required this.description,
    required this.isBillable,
  });

  final int id;
  final int projectId;
  final DateTime startedAtUtc;
  final String description;
  final bool isBillable;

  factory RunningTimerRecord.fromJson(Map<String, dynamic> json) {
    return RunningTimerRecord(
      id: _readInt(json, const ['id', 'Id']) ?? 0,
      projectId: _readInt(json, const ['projectId', 'ProjectId']) ?? 0,
      startedAtUtc: _readUtcDateTime(
            json,
            const ['startedAtUtc', 'StartedAtUtc'],
          ) ??
          DateTime.now().toUtc(),
      description:
          _readString(json, const ['description', 'Description']) ?? '',
      isBillable: _readBool(json, const ['isBillable', 'IsBillable']) ?? false,
    );
  }
}

class TimeEntriesPage {
  const TimeEntriesPage({
    required this.items,
    required this.totalCount,
  });

  final List<TimeEntryRecord> items;
  final int totalCount;

  factory TimeEntriesPage.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] ?? json['Items'];
    final items = rawItems is List
        ? rawItems
            .whereType<Map>()
            .map(
              (item) => TimeEntryRecord.fromJson(
                item.map(
                  (key, dynamic value) => MapEntry(key.toString(), value),
                ),
              ),
            )
            .toList(growable: false)
        : const <TimeEntryRecord>[];

    return TimeEntriesPage(
      items: items,
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
