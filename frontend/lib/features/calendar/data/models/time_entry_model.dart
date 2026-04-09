class TimeEntry {
  final int id;
  final String userId;
  final int projectId;
  final String description;
  final DateTime startTime;
  final DateTime? endTime;
  final int durationMinutes;

  TimeEntry({
    required this.id,
    required this.userId,
    required this.projectId,
    required this.description,
    required this.startTime,
    this.endTime,
    required this.durationMinutes,
  });

  // JSON'dan Dart nesnesine çevrim (Factory pattern)
  factory TimeEntry.fromJson(Map<String, dynamic> json) {
    // startTimeUtc null ise entryDate'i kullan
    String? startTimeStr =
        json['startTimeUtc'] as String? ?? json['entryDate'] as String?;

    if (startTimeStr == null) {
      throw Exception('startTimeUtc veya entryDate gerekli');
    }

    if (!startTimeStr.endsWith('Z') && !startTimeStr.contains('+')) {
      startTimeStr += 'Z';
    }

    String? endTimeStr = json['endTimeUtc'] as String?;
    if (endTimeStr != null &&
        !endTimeStr.endsWith('Z') &&
        !endTimeStr.contains('+')) {
      endTimeStr += 'Z';
    }

    return TimeEntry(
      id: json['id'] as int,
      userId: json['userId'] as String? ?? '',
      projectId: json['projectId'] as int? ?? 1,
      description: json['description'] as String? ?? '',
      startTime: DateTime.parse(startTimeStr).toLocal(),
      endTime: endTimeStr != null ? DateTime.parse(endTimeStr).toLocal() : null,
      durationMinutes: json['durationMinutes'] as int? ?? 0,
    );
  }
}
