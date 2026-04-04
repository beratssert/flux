import 'package:flutter/material.dart';
import 'package:table_calendar/table_calendar.dart';

import '../../data/models/time_entry_model.dart';
import '../../data/services/calendar_service.dart';

class CalendarPage extends StatefulWidget {
  @override
  _CalendarPageState createState() => _CalendarPageState();
}

class _CalendarPageState extends State<CalendarPage> {
  List<TimeEntry> _selectedDayEvents = [];
  bool _isLoading = false;
  final CalendarService _apiService = CalendarService();

  DateTime _focusedDay = DateTime.now();
  DateTime? _selectedDay;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Flux Calendar')),
      body: Column(
        children: [
          TableCalendar(
            firstDay: DateTime.utc(2020, 1, 1),
            lastDay: DateTime.utc(2030, 12, 31),
            focusedDay: _focusedDay,
            selectedDayPredicate: (day) => isSameDay(_selectedDay, day),
            onDaySelected: (selectedDay, focusedDay) {
              setState(() {
                _selectedDay = selectedDay;
                _focusedDay = focusedDay;
              });
              // Seçilen günün verilerini backend'den çek!
              _fetchEventsForDay(selectedDay);
            },
            calendarStyle: CalendarStyle(
              todayDecoration: BoxDecoration(
                  color: Colors.blueAccent, shape: BoxShape.circle),
              selectedDecoration: BoxDecoration(
                  color: Colors.deepPurple, shape: BoxShape.circle),
            ),
          ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : SingleChildScrollView(
                    // Saatler arasında kaydırma yapabilmek için
                    child: Stack(
                      children: [
                        // 1. Arka plandaki saat çizgileri (00:00 - 23:00)
                        Column(
                          children: List.generate(
                              24,
                              (index) => Container(
                                    height:
                                        60, // Her bir saat dilimi 60 piksel yüksekliğinde
                                    decoration: BoxDecoration(
                                      border: Border(
                                          top: BorderSide(
                                              color: Colors.grey.shade200)),
                                    ),
                                    child: Row(
                                      children: [
                                        SizedBox(
                                          width: 50,
                                          child: Text(
                                            "${index.toString().padLeft(2, '0')}:00",
                                            style: TextStyle(
                                                color: Colors.grey.shade500,
                                                fontSize: 12),
                                            textAlign: TextAlign.center,
                                          ),
                                        ),
                                        Expanded(child: Container()),
                                      ],
                                    ),
                                  )),
                        ),

                        // 2. Görev bloklarını saatlerin üzerine yerleştirme
                        ..._selectedDayEvents.map((event) {
                          // Görevin başlangıç saatine göre dikey pozisyonunu hesapla
                          // Örn: Saat 09:00 ise (9 * 60) = 540 piksel aşağıdan başlar
                          final double topPosition =
                              (event.startTime.hour * 60.0) +
                                  (event.startTime.minute * 1.0);
                          final double height = event.durationMinutes * 1.0;

                          return Positioned(
                            top: topPosition,
                            left: 60, // Saat sütununun yanından başla
                            right: 10,
                            height: height,
                            child: GestureDetector(
                              onTap: () => _showEditDeleteDialog(event),
                              child: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: Colors.deepPurple.withOpacity(0.2),
                                  border: const Border(
                                      left: BorderSide(
                                          color: Colors.deepPurple, width: 4)),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  event.description,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold,
                                      fontSize: 12),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ),
                          );
                        }).toList(),
                      ],
                    ),
                  ),
          )
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          _showAddTaskDialog(); // Yazdığın fonksiyonu burada çağırıyoruz
        },
        backgroundColor: Colors.deepPurple,
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }

  void _showAddTaskDialog() {
    // Seçili gün kontrolü
    if (_selectedDay == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Lütfen takvimden bir gün seçin')),
      );
      return;
    }

    final TextEditingController descriptionController = TextEditingController();
    final TextEditingController durationController = TextEditingController();
    final TextEditingController projectIdController = TextEditingController();
    TimeOfDay selectedTime = const TimeOfDay(hour: 9, minute: 0);

    showDialog(
      context: context,
      builder: (context) =>
          StatefulBuilder(builder: (context, setStateBuilder) {
        return AlertDialog(
          title: const Text("Yeni Görev Ekle"),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                  controller: descriptionController,
                  decoration: const InputDecoration(labelText: "Açıklama")),
              TextField(
                  controller: durationController,
                  decoration: const InputDecoration(labelText: "Süre (Dakika)"),
                  keyboardType: TextInputType.number),
              TextField(
                  controller: projectIdController,
                  decoration: const InputDecoration(labelText: "Proje ID"),
                  keyboardType: TextInputType.number),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text("Başlangıç: ${selectedTime.format(context)}"),
                  ElevatedButton(
                    onPressed: () async {
                      final TimeOfDay? time = await showTimePicker(
                        context: context,
                        initialTime: selectedTime,
                      );
                      if (time != null) {
                        setStateBuilder(() {
                          selectedTime = time;
                        });
                      }
                    },
                    child: const Text("Saat Seç"),
                  ),
                ],
              ),
            ],
          ),
          actions: [
            TextButton(
                onPressed: () {
                  descriptionController.dispose();
                  durationController.dispose();
                  projectIdController.dispose();
                  Navigator.pop(context);
                },
                child: const Text("İptal")),
            ElevatedButton(
              onPressed: () async {
                // Input doğrulaması
                if (descriptionController.text.isEmpty) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Lütfen açıklama girin')),
                  );
                  return;
                }

                int? projectId;
                int? duration;

                try {
                  projectId = int.parse(projectIdController.text);
                  duration = int.parse(durationController.text);
                } catch (e) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Lütfen geçerli sayı girin')),
                  );
                  return;
                }

                if (duration <= 0) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Süre 0 dan büyük olmalı')),
                  );
                  return;
                }

                final day = _selectedDay ?? DateTime.now();
                final entryDateTime = DateTime(
                  day.year,
                  day.month,
                  day.day,
                  selectedTime.hour,
                  selectedTime.minute,
                );

                final success = await _apiService.createTimeEntry(
                  projectId: projectId,
                  description: descriptionController.text,
                  duration: duration,
                  date: entryDateTime,
                );

                if (success) {
                  descriptionController.dispose();
                  durationController.dispose();
                  projectIdController.dispose();
                  if (context.mounted)
                    Navigator.pop(context); // 1. Pencereyi kapat

                  // 2. Başarı mesajı göster
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Görev başarıyla eklendi!'),
                        backgroundColor: Colors.green,
                      ),
                    );
                  }

                  // 3. Ekranı tazelemek için verileri tekrar çek
                  _fetchEventsForDay(day);
                } else {
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Görev eklenirken hata oluştu'),
                        backgroundColor: Colors.red,
                      ),
                    );
                  }
                }
              },
              child: const Text("Kaydet"),
            ),
          ],
        );
      }),
    );
  }

  Future<void> _fetchEventsForDay(DateTime day) async {
    setState(() => _isLoading = true);

    // API sadece o günü istediği için 'from' ve 'to' aralığını günün başı ve sonu yapıyoruz
    final startOfDay = DateTime(day.year, day.month, day.day, 0, 0, 0);
    final endOfDay = DateTime(day.year, day.month, day.day, 23, 59, 59);

    final events = await _apiService.getTimeEntries(startOfDay, endOfDay);

    setState(() {
      _selectedDayEvents = events;
      _isLoading = false;
    });
  }

  void _showEditDeleteDialog(TimeEntry event) {
    showDialog(
        context: context,
        builder: (context) {
          return AlertDialog(
            title: const Text("Görevi Düzenle / Sil"),
            content: Text(
                "Görev: ${event.description}\nSüre: ${event.durationMinutes} dakika"),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text("İptal"),
              ),
              TextButton(
                onPressed: () async {
                  final confirm = await showDialog<bool>(
                    context: context,
                    builder: (ctx) => AlertDialog(
                      title: const Text("Silmeyi Onayla"),
                      content: const Text(
                          "Bu görevi silmek istediğinize emin misiniz?"),
                      actions: [
                        TextButton(
                            onPressed: () => Navigator.pop(ctx, false),
                            child: const Text("Hayır")),
                        ElevatedButton(
                            onPressed: () => Navigator.pop(ctx, true),
                            style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.red),
                            child: const Text("Evet, Sil")),
                      ],
                    ),
                  );

                  if (confirm == true) {
                    final result = await _apiService.deleteTimeEntry(event.id);
                    if (result && mounted) {
                      Navigator.pop(context);
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                            content: Text("Görev silindi"),
                            backgroundColor: Colors.red),
                      );
                      if (_selectedDay != null)
                        _fetchEventsForDay(_selectedDay!);
                    }
                  }
                },
                child: const Text("Sil", style: TextStyle(color: Colors.red)),
              ),
            ],
          );
        });
  }
}
