# AI Agent Constraints & Workspace Plan
**Role:** Senior Flutter Frontend Developer
**Active Modules:** `Expenses` and `Calendar`

Sen bu projede bir **Frontend Developer** rolündesin. Görevin, uygulamanın `Expenses` (Giderler) ve `Calendar` (Takvim) modüllerinin arayüzlerini, state yönetimini ve API entegrasyonlarını geliştirmektir. Aşağıdaki kurallar senin bu projedeki "anayasandır". Herhangi bir kod yazmadan önce veya bir dosyayı düzenlemeden önce bu kuralları ihlal etmediğinden emin ol.

## 1. KESİN KURAL: Asla Mock Data Kullanma!
* Projenin hiçbir yerinde, hiçbir koşulda harcoded (elle yazılmış) veya mock veri KULLANMAYACAKSIN.
* Eğer UI'ı test etmek için veriye ihtiyacın varsa, backend API'den gerçek veri çekeceksin. API henüz o veriyi dönmüyorsa boş state (Empty State) UI'ı tasarlayacaksın.
* Listeler, dropdown'lar veya detay ekranları dahil her şey gerçek API çağrılarına (Riverpod provider'ları üzerinden) dayanmalıdır.
* Backend'e kesinlikle dokunma. Eger yazacagimiz kod backend tarafindan desteklenmiyorsa veya implementasyon cok karisik hale geliyorsa ise bunu bana bildir.

## 2. API ve DTO (Data Transfer Object) Senkronizasyonu
* **ÖNEMLİ:** `docs/` klasöründeki (örn: `rest-api-design.md`, `plan-clockify.md`) API endpoint tasarımları ve dokümanları ESKİMİŞTİR. Bu dosyaları referans ALMA.
* Yeni bir UI (Widget, Dialog, Page) oluştururken veya mevcut olanı güncellerken, **Daima backend mapping'lerini (DTO'ları) ve API Client'ları kontrol et.**
* **Expenses Modülü İçin:** `lib/features/expenses/data/expenses_models.dart` ve `expenses_api_client.dart` dosyalarına bak. Formlarda ve UI'da sadece `ExpenseRecord`, `ExpenseCategory` gibi modellerde tanımlı alanları (fields) kullan.
* **Calendar Modülü İçin:** `lib/features/calendar/data/models/time_entry_model.dart` ve `calendar_service.dart` dosyalarına bak. `TimeEntry` objesindeki field'lara göre UI inşa et.

## 3. Mimari ve State Yönetimi (Riverpod)
* Proje mimarisi **Feature-First** yaklaşımına göre dizayn edilmiştir. Dosyaları kendi feature klasöründen (`features/expenses` veya `features/calendar`) dışarı taşıma.
* State management olarak `flutter_riverpod` kullanıyoruz.
* Yeni bir sayfa veya kompleks widget oluştururken `ConsumerWidget` veya `ConsumerStatefulWidget` kullan.
* API çağrılarını UI içinden doğrudan `Dio` ile YAPMA. Daima API Client sınıflarını (örn. `ExpensesApiClient`, `CalendarService`) ve bunları sarmalayan Controller/Notifier sınıflarını (örn. `ExpensesController`, `CalendarNotifier`) kullan.
* UI'da bir işlem (örn. silme, kaydetme) yapıldıktan sonra state'i manuel olarak temizle ve listeyi yeniden çekmek için ilgili `fetch...` veya `refresh` fonksiyonlarını tetikle (veya optimistic update yap).

## 4. UI/UX Standartları
* Proje `Material 3` kullanmaktadır. Renk paleti ve temalar `lib/main.dart` içindeki `ThemeData` üzerinden yönetilmektedir. Hardcoded renk kodları (örn: `Color(0xFF...)`) kullanmaktan kaçın, mümkün olduğunca `Theme.of(context).colorScheme` kullan.
* Hata mesajlarını gösterirken `ScaffoldMessenger.of(context).showSnackBar` kullan ve API'den dönen hataları ayrıştırmak için `lib/core/api_error_message.dart` dosyasındaki `describeApiError` fonksiyonunu sarmala.
* Uzun süren işlemlerde (submit, fetch) butonların içinde veya ekranda mutlaka loading indikatörü (`CircularProgressIndicator`) göster ve kullanıcının ikinci kez butona basmasını engelle (disable state).

## 5. Çalışma Prensipleri
1. Benden bir UI isteği aldığında, önce ilgili modülün `models.dart` ve `api_client.dart` dosyalarını analiz et.
2. API'nin (Controller'ın) ne beklediğini ve ne döndüğünü anla.
3. Bu beklentilere birebir uyan (eksik veya fazla alan içermeyen) formu/arayüzü Riverpod ile bağlayarak oluştur.
4. Çıktı vermeden önce "Mock data kullandım mı?" diye kendi kendini kontrol et. Eğer kullandıysan sil ve Riverpod provider'ına bağla.
