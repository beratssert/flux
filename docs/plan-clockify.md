# Gereksinim Dokümanı (Final MVP)

## 1) Fonksiyonel Gereksinimler

### A. Kimlik Doğrulama, Yetkilendirme ve Roller
- Kullanıcılar güvenli şekilde oturum açabilir.
- Employee kullanıcılar kendileri kayıt olabilir.
- Manager hesapları Admin tarafından oluşturulur.
- Sistem, JWT tabanlı erişim modeli veya eşdeğer token tabanlı bir yaklaşım kullanır.
- Employee, Manager ve Admin için rol tabanlı erişim kontrolü uygulanır.
- Kullanıcılar yalnızca görme veya değiştirme yetkisine sahip oldukları verilere erişebilir.
- Manager, **kendi verileri üzerinde** Employee yetkilerini kullanabilir.
- Admin, MVP kapsamında **üst düzey kullanıcı/rol yönetimi ve denetim yetkilerine** sahiptir; tüm operasyonel CRUD yetkilerini varsayılan olarak miras almaz.
- Logout işlemi MVP kapsamında istemci tarafında token’ın bırakılmasıyla ele alınabilir; sunucu taraflı token iptal mekanizması sonraki sürümlere bırakılabilir.

### B. Zaman Takibi
- Employee kendi zaman kayıtlarını oluşturabilir, görüntüleyebilir, düzenleyebilir ve silebilir.
- Zaman kaydı alanları en az şunları içerir:
  - proje
  - tarih
  - başlangıç saati ve bitiş saati **veya**
  - toplam süre
  - açıklama
  - isteğe bağlı `billable` bayrağı
- `billable` alanı MVP’de yalnızca bilgilendirme amaçlıdır; faturalama veya invoice akışı içermez.
- Employee zamanlayıcı başlatabilir, durdurabilir ve oluşan süreyi kayıtlı girdiye dönüştürebilir.
- Aktif zamanlayıcı durumu, tamamlanmış `TimeEntry` kayıtlarından ayrı yönetilebilir.
- Employee yalnızca **atandığı projeler** için zaman girişi yapabilir.
- Sistem, çakışan zaman kayıtlarını ve geçersiz süreleri doğrular:
  - negatif süreye izin verilmez
  - sıfır süreye izin verilmez
  - bitiş zamanı başlangıç zamanından önce olamaz
  - aynı kullanıcı için politika gereği izin verilmeyen zaman çakışmaları engellenir
- Manager, yalnızca **yönettiği projelere ait** ekip zaman kayıtlarını ve zaman özetlerini görüntüleyebilir.
- Manager, bir çalışanın başka projelerdeki zaman kayıtlarını görüntüleyemez.

### C. Giderler
- Employee kendi giderlerini oluşturabilir, görüntüleyebilir, düzenleyebilir ve silebilir.
- Gider alanları en az şunları içerir:
  - proje
  - tarih
  - tutar
  - para birimi
  - kategori
  - notlar
  - isteğe bağlı ek referansı
- Employee yalnızca **atandığı projeler** için gider kaydı oluşturabilir.
- Manager, yalnızca **yönettiği projelere ait** ekip giderlerini görüntüleyebilir.
- Gider workflow’u MVP’de sade ama çıkmaz üretmeyecek şekilde tasarlanır.
- Gider kaydı en az aşağıdaki durumları destekler:
  - `Draft`
  - `Submitted`
  - `Rejected`
- Durum kuralları:
  - `Draft` durumundaki kayıt sahibi tarafından düzenlenebilir veya silinebilir
  - `Draft -> Submitted` geçişi kayıt sahibi tarafından yapılabilir
  - `Submitted` durumundaki kayıt sahibi tarafından düzenlenemez
  - Manager, yalnızca yönettiği projelerdeki submitted giderleri `Rejected` durumuna çekebilir
  - `Rejected` durumundaki kayıt sahibi tarafından yeniden düzenlenebilir ve tekrar submit edilebilir
- Çok adımlı gider onay akışları MVP kapsamı dışındadır.

### D. Raporlar
- Employee kişisel raporlarını görüntüleyebilir:
  - gün / hafta / ay / proje bazında zaman toplamları
  - dönem / proje / kategori bazında gider toplamları
- Manager, yalnızca **yönettiği projelerdeki** employee raporlarını görüntüleyebilir.
- Manager ekip özetlerini görüntüleyebilir:
  - toplam takip edilen saat
  - toplam gider
  - proje bazında kırılım
  - dönem bazında kırılım
- Raporlar en az şu filtreleri destekler:
  - tarih aralığı
  - proje
  - employee (yalnızca yetkili roller ve kapsam dahilinde)
- Raporlar ayrı bir fiziksel tablo olmak zorunda değildir; zaman kayıtları ve giderler üzerinden dinamik olarak üretilebilir.
- Historical raporlar, yalnızca aktif proje atamalarına dayanmamalıdır.
- Geçmişte oluşturulmuş time entry ve expense kayıtları, kullanıcı artık projeye atanmış olmasa bile yetki kapsamında raporlarda görülebilmelidir.
- Raporlar en az CSV formatında export edilebilir olmalıdır.
- PDF export opsiyonel olabilir.

### E. Takvim
- Employee kendisine görünür olan takvim etkinliklerini görüntüleyebilir.
- Employee ayrıca atandığı projelerle ilişkili etkinlikleri görüntüleyebilir.
- Manager, yönettiği projeler için takvim etkinliği oluşturabilir, görüntüleyebilir, güncelleyebilir ve silebilir.
- Takvim API’si, istemcinin gün / hafta / ay görünümünü oluşturabilmesi için tarih aralığına göre sorgulamayı destekler.
- Takvim görünümünde en az aşağıdaki veriler desteklenebilir:
  - uygulama içi takvim etkinlikleri
  - isteğe bağlı olarak zaman kayıtlarının takvim temsili
- Harcamalar takvim görünümünün zorunlu bir parçası değildir.
- `Personal`, `Project` ve `Team` görünürlükleri desteklenebilir.
- Employee MVP’de personal event oluşturmaz.
- `Personal` görünürlük, katılımcı listesi üzerinden sınırlandırılır.
- Harici takvim sağlayıcılarıyla çift yönlü senkronizasyon MVP kapsamı dışındadır.

### F. Proje ve Atama Yönetimi
- Manager proje oluşturabilir.
- Manager yalnızca **kendi yönettiği projeleri** görüntüleyebilir ve güncelleyebilir.
- Manager, yönettiği projelere employee atayabilir veya atamayı kaldırabilir.
- Employee yalnızca kendi proje atamalarını görüntüleyebilir.
- Aynı employee aynı projeye birden fazla kez atanamaz.
- Proje silme işlemi MVP’de **hard delete** olarak uygulanmaz.
- Projeler için `archived` veya `closed` gibi durumlarla kapatma/arşivleme yaklaşımı tercih edilir.
- Her projenin aktif bir manager’ı bulunmalıdır.
- Admin, gerekli durumlarda proje manager’ını başka bir manager ile değiştirebilir.
- Admin, üst düzey rol atamalarını ve manager hesaplarını yönetebilir.

### G. Admin Yetenekleri
- Admin manager hesapları oluşturabilir ve yönetebilir.
- Admin kullanıcı rollerini politika kapsamında atayabilir veya yeniden atayabilir.
- Admin kullanıcı durumunu değiştirebilir:
  - aktif
  - pasif
  - askıya alınmış
- Admin, kritik yönetimsel değişiklikleri denetleyebilir:
  - kim değişiklik yaptı
  - hangi kayıt değişti
  - ne zaman değişti
- Admin için hard delete önerilmez; kullanıcı devre dışı bırakma veya durum değiştirme tercih edilir.
- Admin normal proje CRUD yapmaz; ancak yöneticisiz kalma riskini çözmek için manager reassignment yapabilir.

---

## 2) Fonksiyonel Olmayan Gereksinimler

### Güvenlik
- Kimlik doğrulama için JWT veya eşdeğer güvenli token yaklaşımı kullanılır.
- Parolalar modern ve güvenli algoritmalarla hash’lenir:
  - PBKDF2
  - bcrypt
  - Argon2
- Yetkilendirme kontrolleri hem API katmanında hem iş mantığı katmanında uygulanır.
- Girdi doğrulaması zorunludur.
- Yaygın API risklerine karşı koruma sağlanır:
  - injection
  - over-posting
  - yetkisiz erişim
- Hassas veriler aktarım sırasında HTTPS ile korunur.

### Performans
- Beklenen MVP yükü altında standart CRUD işlemlerinde tipik API yanıt süresi 500 ms altında hedeflenir.
- Rapor uç noktaları küçük ve orta ölçekli veri setleri için optimize edilir.
- Sayfalama ve filtreleme raporlama ve listeleme uç noktalarında desteklenir.
- Export işlemleri küçük ve orta ölçekli veri setlerinde kabul edilebilir sürede tamamlanmalıdır.
- MVP, küçük ve orta ölçekli ekiplerin eşzamanlı kullanımını desteklemelidir.

### Güvenilirlik ve Veri Bütünlüğü
- Kritik işlemler için ACID uyumlu kalıcılık sağlanır.
- Tarih/saat, sahiplik ve yetki kısıtları güçlü şekilde doğrulanır.
- Kritik kayıtlar için soft delete, archive veya audit stratejileri uygulanır.
- Veri bütünlüğü için benzersizlik ve ilişki kısıtları tanımlanır.
- Aktif timer ve tamamlanmış time entry kayıtları birbiriyle çelişmeyecek şekilde tasarlanmalıdır.

### Ölçeklenebilirlik
- Mimari; gelecekte eklenecek aşağıdaki özellikleri destekleyebilecek modülerlikte olmalıdır:
  - onay akışları
  - faturalama
  - entegrasyonlar
  - gelişmiş raporlama
- Veritabanı şeması, kayıt ve rapor sorgularının büyümesini destekleyecek şekilde tasarlanmalıdır.

### Bakım Kolaylığı
- Clean Architecture sınırları korunur:
  - Web API
  - Application
  - Infrastructure
  - Domain
- Tutarlı hata yönetimi uygulanır.
- Yapılandırılmış loglama kullanılmalıdır.
- İş kuralları için birim testleri yazılmalıdır.
- Temel uç noktalar için entegrasyon testleri sağlanmalıdır.

### Kullanılabilirlik
- API sözleşmeleri öngörülebilir ve tutarlı olmalıdır.
- RESTful kaynak adlandırma yaklaşımı izlenmelidir.
- Uygun HTTP durum kodları kullanılmalıdır.
- Yetki ve doğrulama hataları için açık ve anlaşılır hata mesajları sunulmalıdır.

### Uyum ve Denetlenebilirlik
- Admin rol değişiklikleri için temel bir denetim izi tutulmalıdır.
- Kritik proje atamaları için temel bir denetim izi tutulmalıdır.
- Project manager reassignment işlemleri denetlenebilir olmalıdır.
- Expense reject işlemleri denetlenebilir olmalıdır.
- Proje oluşturma, kapatma/arşivleme ve rol değişiklikleri denetlenebilir olmalıdır.
- Zaman ve gider geçmişi için saklama kuralları dokümante edilmelidir.

---

## 3) Role Göre Kullanım Senaryoları

### Employee
1. Sisteme kendi employee hesabını oluşturarak kayıt olur.
2. Atandığı projede zaman girişi yapar.
3. Çalışma sırasında zamanlayıcıyı başlatır ve durdurur.
4. Hatalı kendi zaman kaydını düzenler veya siler.
5. Kendi giderini oluşturur, submit eder ve reject edildiyse düzenleyip tekrar submit eder.
6. Kendi haftalık saat ve gider raporunu görüntüler.
7. Kendi raporlarını CSV olarak dışa aktarır.
8. Kendisine görünür etkinlikleri ve atandığı projelere ait takvim verisini görüntüler.

### Manager
1. Yeni bir proje oluşturur.
2. Yönettiği projeye employee atar veya atamayı kaldırır.
3. Yalnızca yönettiği projelere ait ekip zaman kayıtlarını görüntüler.
4. Yalnızca yönettiği projelere ait ekip gider toplamlarını proje ve tarih aralığına göre görüntüler.
5. Submitted durumundaki ekip giderini reject eder.
6. Ekip verimlilik ve maliyet özet raporu üretir.
7. Yalnızca yetkili olduğu kapsamda employee kırılımına iner.
8. Raporları CSV olarak dışa aktarır.
9. Yönettiği projeler için takvim etkinliği oluşturur, günceller veya siler.

### Admin
1. Manager hesabı oluşturur.
2. Manager rol atamalarını günceller.
3. Kullanıcıları politika kapsamında roller arasında yükseltir veya düşürür.
4. Kullanıcıların aktif/pasif durumunu değiştirir.
5. Gerekirse bir projenin manager’ını başka bir manager ile değiştirir.
6. Yönetimsel değişiklikleri denetim logları üzerinden inceler.
7. Modüller genelinde rol sınırlarının doğru uygulandığını denetler.

---

## 4) Varsayımlar ve MVP Kapsamı

### Varsayımlar
- Sürüm 1 için tek organizasyon (`single-tenant`) varsayılır.
- Employee kullanıcılar self-registration ile oluşturulabilir.
- Manager kullanıcılar admin akışı ile oluşturulur.
- MVP’de kur dönüşümü gerekmez; tek para birimi veya girildiği gibi saklama yaklaşımı yeterlidir.
- Onay akışları minimaldir veya ertelenmiştir.
- Takvim etkinlikleri uygulama içi etkinliklerdir; tam dış takvim senkronizasyonu yoktur.
- Şifre sıfırlama ve gelişmiş oturum yönetimi sonraki sürümlere bırakılabilir.

### MVP Kapsamı (Sürüm 1)
- Employee, Manager ve Admin için rol tabanlı erişim kontrolü
- Employee self-registration
- Atama kontrolleri ile zaman kaydı CRUD
- Aktif timer state yönetimi ve timer start/stop akışı
- Sahiplik ve proje uygunluğu kontrolleri ile gider CRUD
- Draft / Submitted / Rejected expense workflow
- Employee ve Manager kapsamları için temel rapor uç noktaları
- CSV export desteği
- Manager tarafından proje oluşturma ve employee atama
- Manager tarafından proje bazlı takvim etkinliği yönetimi
- Admin tarafından manager hesap yönetimi ve rol atama
- Admin tarafından manager reassignment
- Temel takvim görünümü için veri sağlama
- Admin ve kritik atama/rol değişiklikleri için çekirdek denetim loglama

---

## 5) Sürüm 1 Kapsam Dışı Kalemler
- Bordro işlemleri
- Faturalama ve invoice üretimi
- Gelişmiş onay akışları
- Çok adımlı gider veya zaman onay mekanizmaları
- Harici entegrasyonlar:
  - Google Calendar
  - Outlook
  - Slack
  - muhasebe araçları
- Gerçek zamanlı iş birliği ve bildirimler
- Offline-first mobil senkronizasyon karmaşıklığı
- Multi-tenant mimari
- Gelişmiş analitik panoları:
  - tahminleme
  - anomali tespiti
  - yapay zeka içgörüleri
- Özel rol oluşturucu veya ince taneli politika motoru
- Dosya yaşam döngüsü yönetimi
- Uluslararası vergi / KDV uyumluluk otomasyonu
- Tam kapsamlı şifre sıfırlama, token yenileme ve gelişmiş oturum güvenliği özellikleri
- PDF export zorunlu değildir; CSV önceliklidir