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

  // Convert from JSON to Dart object (Factory pattern)
  factory TimeEntry.fromJson(Map<String, dynamic> json) {
    // Use startTimeUtc if available, otherwise use entryDate and set time to 09:00
    DateTime parsedStartTime;
    String? startTimeUtc = json['startTimeUtc'] as String?;

    if (startTimeUtc != null) {
      if (!startTimeUtc.endsWith('Z') && !startTimeUtc.contains('+')) {
        startTimeUtc += 'Z';
      }
      parsedStartTime = DateTime.parse(startTimeUtc).toLocal();
    } else {
      String? entryDate = json['entryDate'] as String?;
      if (entryDate == null) {
        throw Exception('startTimeUtc or entryDate is required');
      }
      // entryDate is usually in format "yyyy-MM-dd" or "yyyy-MM-ddT00:00:00Z"
      DateTime date = DateTime.parse(entryDate).toLocal();
      parsedStartTime =
          DateTime(date.year, date.month, date.day, 9, 0); // Default 09:00
    }

    String? endTimeUtc = json['endTimeUtc'] as String?;
    DateTime? parsedEndTime;
    if (endTimeUtc != null) {
      if (!endTimeUtc.endsWith('Z') && !endTimeUtc.contains('+')) {
        endTimeUtc += 'Z';
      }
      parsedEndTime = DateTime.parse(endTimeUtc).toLocal();
    }

    int duration = json['durationMinutes'] as int? ?? 0;
    // If recorded without time (no duration and no endTime), set 60 minutes for calendar display
    if (duration == 0 && parsedEndTime == null) {
      duration = 60;
    }

    return TimeEntry(
      id: json['id'] as int? ?? 0,
      userId: json['userId'] as String? ?? '',
      projectId: json['projectId'] as int? ?? 1,
      description: json['description'] as String? ?? '',
      startTime: parsedStartTime,
      endTime: parsedEndTime,
      durationMinutes: duration,
    );
  }
}
