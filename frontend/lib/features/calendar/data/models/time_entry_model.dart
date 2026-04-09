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
    // startTimeUtc varsa kullan, yoksa entryDate'i kullanıp saat olarak 09:00 ata
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
        throw Exception('startTimeUtc veya entryDate gerekli');
      }
      // entryDate genelde "yyyy-MM-dd" veya "yyyy-MM-ddT00:00:00Z" şeklindedir
      DateTime date = DateTime.parse(entryDate).toLocal();
      parsedStartTime =
          DateTime(date.year, date.month, date.day, 9, 0); // Varsayılan 09:00
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
    // Eğer saatsiz (duration yok ve endTime yok) kaydedilmişse takvimde gözükmesi için 60 dk verelim
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
