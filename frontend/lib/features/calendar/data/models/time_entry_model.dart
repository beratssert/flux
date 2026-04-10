class TimeEntry {
  final String id;
  final String userId;
  final String projectId;
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
    try {
      String? startTimeStr = json['startTime']?.toString() ??
          json['startTimeUtc']?.toString() ??
          json['entryDate']?.toString() ??
          json['StartTime']?.toString() ??
          json['StartTimeUtc']?.toString();
      DateTime parsedStartTime;

      if (startTimeStr != null && startTimeStr.isNotEmpty) {
        if (!startTimeStr.endsWith('Z') && !startTimeStr.contains('+')) {
          startTimeStr += 'Z';
        }
        parsedStartTime = DateTime.parse(startTimeStr).toLocal();
      } else {
        parsedStartTime = DateTime.now();
      }

      String? endTimeStr = json['endTime']?.toString() ??
          json['endTimeUtc']?.toString() ??
          json['EndTime']?.toString() ??
          json['EndTimeUtc']?.toString();
      DateTime? parsedEndTime;

      if (endTimeStr != null && endTimeStr.isNotEmpty) {
        if (!endTimeStr.endsWith('Z') && !endTimeStr.contains('+')) {
          endTimeStr += 'Z';
        }
        parsedEndTime = DateTime.parse(endTimeStr).toLocal();
      }

      int duration = (json['durationInMinutes'] as num?)?.toInt() ??
          (json['DurationInMinutes'] as num?)?.toInt() ??
          (json['durationMinutes'] as num?)?.toInt() ??
          (json['DurationMinutes'] as num?)?.toInt() ??
          0;

      if (duration == 0 && parsedEndTime == null) {
        duration = 60;
      }

      return TimeEntry(
        id: json['id']?.toString() ?? json['Id']?.toString() ?? '',
        userId: json['userId']?.toString() ?? json['UserId']?.toString() ?? '',
        projectId: json['projectId']?.toString() ??
            json['ProjectId']?.toString() ??
            '',
        description: json['description']?.toString() ??
            json['Description']?.toString() ??
            '',
        startTime: parsedStartTime,
        endTime: parsedEndTime,
        durationMinutes: duration,
      );
    } catch (e, stack) {
      print("TimeEntry.fromJson HATASI: $e \n JSON İçeriği: $json");
      print(stack);
      rethrow; // Bu liste okumasını dışarda kırar
    }
  }
}
