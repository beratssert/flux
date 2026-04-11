# Domain Model ve Veritabanı Tasarımı

Amaç:
- MVP kapsamına uygun kalmak
- gereksiz karmaşıklığı azaltmak
- backend implementasyonunu kolaylaştırmak
- auth matrix ve gereksinim dokümanıyla tutarlı olmak

Bu sürümde:
- **tek organizasyon (single-tenant)** varsayılır
- **her projenin tek bir manager’ı vardır**
- raporlar **çoğunlukla sorgu tabanlı** üretilir
- **hard delete yerine** soft delete / archive / deactivate tercih edilir
- rol modeli sadeleştirilmiştir: **her kullanıcı için tek ana rol**

---

## 1) Temel Tasarım Kararları

### 1.1 Rol Modeli
Bu MVP’de her kullanıcı için tek ana rol bulunur:
- `Employee`
- `Manager`
- `Admin`

Bu nedenle ayrı `Role` ve `UserRole` tabloları yerine, `User` tablosunda doğrudan rol alanı tutulur.

### 1.2 Proje Sahipliği
- Her projenin tam olarak **bir manager’ı** vardır.
- Bu ilişki `Project.ManagerUserId` ile tutulur.
- Admin gerektiğinde proje manager’ını başka bir manager ile değiştirebilir.

### 1.3 Atamalar
- Employee’lerin projelerde çalışması `ProjectAssignment` tablosu ile yönetilir.
- Aynı employee aynı projede aynı anda yalnızca **bir aktif atamaya** sahip olabilir.

### 1.4 Raporlar
- Raporlar ayrı bir ana tablo olarak tutulmaz.
- `TimeEntry`, `Expense` ve `Project` üzerinden sorgu ile üretilir.
- Historical reports oluşturulurken yalnızca aktif assignment kayıtlarına bağımlı kalınmamalıdır.

### 1.5 Zamanlayıcı
- Aktif timer state’i için ayrı bir `RunningTimer` tablosu kullanılır.
- `TimeEntry` yalnızca tamamlanmış zaman kayıtlarını tutar.
- Timer stop edildiğinde `RunningTimer` kaydı bir `TimeEntry` kaydına dönüştürülür.

### 1.6 Takvim
- Takvim modülü proje odaklı ve sade tutulur.
- Manager, yönettiği projeler için etkinlik yönetebilir.
- Employee görünür etkinlikleri okuyabilir.
- Katılımcı yapısı desteklenir ancak gelişmiş davet/yanıt akışları MVP dışındadır.
- `Personal` görünürlük participant listesi ile kontrol edilir.

### 1.7 Expense Review
- Expense workflow’u sade tutulur:
  - `Draft`
  - `Submitted`
  - `Rejected`
- Manager, yönettiği projelerdeki submitted expense’leri reject edebilir.

### 1.8 Proje kimlikleri: `int` (implementation)
Aşağıdaki entity detaylarında `Project.Id` ve çeşitli `ProjectId` foreign key’ler **hedef tasarımda UUID** olarak yazılmış olabilir. **Flux MVP backend’inde bilinçli olarak `int` kullanılır:**
- `Project.Id`: `int` identity PK
- `ProjectAssignment.ProjectId`, `TimeEntry.ProjectId`, `Expense.ProjectId`, `RunningTimer.ProjectId` vb.: hepsi **`int` FK → Project.Id**

Gerekçe: zaman takibi ve gider modülleri baştan bu şemayla kuruldu; tutarlılık ve migration maliyeti açısından proje modülü de aynı modelde kaldı. İleride UUID veya ayrı bir `PublicId` (GUID) ihtiyacı çıkarsa ayrı bir evrim planı yazılabilir.

---

## 2) Entity Listesi

1. `User`
2. `Project`
3. `ProjectAssignment`
4. `RunningTimer`
5. `TimeEntry`
6. `ExpenseCategory`
7. `Expense`
8. `CalendarEvent`
9. `CalendarEventParticipant`
10. `AuditLog`

---

## 3) Entity Detayları

## 3.1 User
Sistem kullanıcılarını tutar.

### Alanlar
- `Id` (UUID, PK)
- `FirstName` (string, max 100, not null)
- `LastName` (string, max 100, not null)
- `Email` (string, max 255, not null, unique)
- `PasswordHash` (string, not null)
- `Role` (string / enum, not null)  
  Allowed values:
  - `Employee`
  - `Manager`
  - `Admin`
- `IsActive` (bool, not null, default true)
- `CreatedAtUtc` (datetime utc, not null)
- `UpdatedAtUtc` (datetime utc, nullable)
- `LastLoginAtUtc` (datetime utc, nullable)

### Notlar
- `Email` case-insensitive unique olmalıdır.
- `IsActive = false` kullanıcı sisteme giriş yapamamalıdır.
- Admin için hard delete yerine `IsActive = false` veya benzeri bir durum yönetimi tercih edilmelidir.

---

## 3.2 Project
Projeleri tutar.

### Alanlar
- `Id` (PK; hedef tasarımda UUID, **mevcut backend’de `int` identity**)
- `Name` (string, max 150, not null)
- `Code` (string, max 50, nullable, unique)
- `Description` (string, nullable)
- `ManagerUserId` (UUID, FK -> User, not null)
- `Status` (string / enum, not null)  
  Allowed values:
  - `Active`
  - `Archived`
  - `Closed`
- `StartDate` (date, nullable)
- `EndDate` (date, nullable)
- `CreatedAtUtc` (datetime utc, not null)
- `UpdatedAtUtc` (datetime utc, nullable)

### Notlar
- `ManagerUserId` yalnızca `Manager` rolündeki bir kullanıcıyı göstermelidir.
- `EndDate < StartDate` olamaz.
- Project hard delete uygulanmamalıdır.
- Silme yerine `Archived` veya `Closed` kullanılmalıdır.
- Manager pasif yapılmadan veya rolü düşürülmeden önce gerekli ise manager reassignment yapılmalıdır.
- Bkz. **§1.8** — `Project.Id` ve tüm `ProjectId` FK’leri backend’de **`int`**.

---

## 3.3 ProjectAssignment
Employee’lerin projelere atanmasını tutar.

### Alanlar
- `Id` (PK; hedef tasarımda UUID, **mevcut backend’de `int` identity**)
- `ProjectId` (FK -> Project, not null; **mevcut backend’de `int`**)
- `UserId` (UUID, FK -> User, not null)
- `AssignedAtUtc` (datetime utc, not null)
- `AssignedByUserId` (UUID, FK -> User, nullable)
- `IsActive` (bool, not null, default true)
- `UnassignedAtUtc` (datetime utc, nullable)

### Notlar
- Bu tablo esas olarak employee atamaları için kullanılır.
- Aynı employee aynı projede aynı anda yalnızca bir aktif atamaya sahip olabilir.
- Atama kaldırıldığında:
  - `IsActive = false`
  - `UnassignedAtUtc` dolu olmalıdır.
- Manager proje sahibi olarak `Project.ManagerUserId` üzerinden tutulur; assignment ile manager sahipliği yönetilmez.

---

## 3.4 RunningTimer
Aktif zamanlayıcı durumunu tutar.

### Alanlar
- `Id` (UUID, PK)
- `UserId` (UUID, FK -> User, not null)
- `ProjectId` (UUID, FK -> Project, not null)
- `StartedAtUtc` (datetime utc, not null)
- `Description` (string, max 1000, nullable)
- `IsBillable` (bool, not null, default false)
- `CreatedAtUtc` (datetime utc, not null)

### Notlar
- Bir kullanıcı aynı anda yalnızca **bir aktif running timer** kaydına sahip olabilir.
- Bu nedenle `UserId` için aktif timer tarafında unique kısıt önerilir.
- Timer yalnızca kullanıcının atandığı projeler için başlatılabilir.
- Timer stop edildiğinde bu kayıt tamamlanmış bir `TimeEntry` kaydına dönüştürülür.

---

## 3.5 TimeEntry
Tamamlanmış zaman kayıtlarını tutar.

### Alanlar
- `Id` (UUID, PK)
- `UserId` (UUID, FK -> User, not null)
- `ProjectId` (UUID, FK -> Project, not null)
- `EntryDate` (date, not null)
- `StartTimeUtc` (datetime utc, nullable)
- `EndTimeUtc` (datetime utc, nullable)
- `DurationMinutes` (int, not null)
- `Description` (string, max 1000, nullable)
- `IsBillable` (bool, not null, default false)
- `SourceType` (string / enum, not null)  
  Allowed values:
  - `Manual`
  - `Timer`
- `IsLocked` (bool, not null, default false)
- `CreatedAtUtc` (datetime utc, not null)
- `UpdatedAtUtc` (datetime utc, nullable)
- `DeletedAtUtc` (datetime utc, nullable)

### Notlar
- Employee yalnızca **atandığı projeler** için time entry oluşturabilir.
- `DurationMinutes > 0` olmalıdır.
- `StartTimeUtc` ve `EndTimeUtc` birlikte doluysa:
  - `EndTimeUtc > StartTimeUtc` olmalıdır.
- `StartTimeUtc` / `EndTimeUtc` varsa, `DurationMinutes` backend tarafından doğrulanmalı veya hesaplanmalıdır.
- `EntryDate`, kayıt için kullanılan iş gününü temsil eder.
- MVP’de bir `TimeEntry` için cross-day kullanım desteklenmeyecek şekilde sadeleştirilebilir:
  - öneri: bir kayıt aynı takvim günü içinde kalmalıdır
- Aynı kullanıcı için çakışan zaman aralıkları engellenmelidir.
- `IsLocked = true` ise update/delete engellenmelidir.
- Silme işlemi soft delete ile (`DeletedAtUtc`) yapılmalıdır.

---

## 3.6 ExpenseCategory
Gider kategorilerini tutar.

### Alanlar
- `Id` (UUID, PK)
- `Name` (string, max 100, not null, unique)
- `IsActive` (bool, not null, default true)
- `CreatedAtUtc` (datetime utc, not null)

### Notlar
- MVP’de kategoriler **global** kabul edilir.
- Proje bazlı veya kullanıcı bazlı kategori ayrımı yapılmaz.

---

## 3.7 Expense
Gider kayıtlarını tutar.

### Alanlar
- `Id` (UUID, PK)
- `UserId` (UUID, FK -> User, not null)
- `ProjectId` (UUID, FK -> Project, not null)
- `ExpenseDate` (date, not null)
- `Amount` (decimal(18,2), not null)
- `CurrencyCode` (char(3), not null)
- `CategoryId` (UUID, FK -> ExpenseCategory, not null)
- `Notes` (string, max 1000, nullable)
- `ReceiptUrl` (string, nullable)
- `Status` (string / enum, not null)  
  Allowed values:
  - `Draft`
  - `Submitted`
  - `Rejected`
- `RejectedReason` (string, max 1000, nullable)
- `ReviewedByUserId` (UUID, FK -> User, nullable)
- `ReviewedAtUtc` (datetime utc, nullable)
- `CreatedAtUtc` (datetime utc, not null)
- `UpdatedAtUtc` (datetime utc, nullable)
- `DeletedAtUtc` (datetime utc, nullable)

### Notlar
- Employee yalnızca **atandığı projeler** için expense oluşturabilir.
- `Amount > 0` olmalıdır.
- `CurrencyCode` ISO 4217 formatında 3 harf olmalıdır.
- `Draft` durumundaki kayıt sahibi tarafından düzenlenebilir veya silinebilir.
- `Draft -> Submitted` geçişi sahibi tarafından yapılabilir.
- `Submitted` durumundaki kayıt sahibi tarafından değiştirilemez.
- Manager, yalnızca yönettiği projelerdeki submitted expense kayıtlarını `Rejected` durumuna çekebilir.
- `Rejected` durumundaki kayıt sahibi tarafından yeniden düzenlenebilir ve tekrar submit edilebilir.
- Çok adımlı approval workflow MVP dışındadır.
- Silme işlemi soft delete ile (`DeletedAtUtc`) yapılmalıdır.

---

## 3.8 CalendarEvent
Takvim etkinliklerini tutar.

### Alanlar
- `Id` (UUID, PK)
- `ProjectId` (FK -> Project, nullable; veritabanında `Project.Id` ile uyumlu tamsayı — HTTP API’de de int `projectId`)
- `Title` (string, max 200, not null)
- `Description` (string, nullable)
- `StartAtUtc` (datetime utc, not null)
- `EndAtUtc` (datetime utc, not null)
- `CreatedByUserId` (UUID, FK -> User, not null)
- `VisibilityType` (string / enum, not null)  
  Allowed values:
  - `Personal`
  - `Project`
  - `Team`
- `IsAllDay` (bool, not null, default false)
- `CreatedAtUtc` (datetime utc, not null)
- `UpdatedAtUtc` (datetime utc, nullable)

### Notlar
- `EndAtUtc > StartAtUtc` olmalıdır.
- Manager, yalnızca yönettiği projeler için ilgili etkinlikleri oluşturabilmelidir.
- Project ilişkili bir event için `ProjectId` dolu olmalıdır.
- Personal event için `ProjectId` boş olabilir.
- Employee MVP’de event oluşturmaz; ancak görünür event’leri okuyabilir.

---

## 3.9 CalendarEventParticipant
Etkinlik katılımcılarını tutar.

### Alanlar
- `EventId` (UUID, FK -> CalendarEvent, PK bileşeni)
- `UserId` (UUID, FK -> User, PK bileşeni)
- `ParticipationType` (string / enum, nullable)  
  Allowed values:
  - `Required`
  - `Optional`

### Notlar
- PK: (`EventId`, `UserId`)
- Bu yapı görünürlük ve katılımcı listesi için yeterlidir.
- RSVP, invitation acceptance, decline gibi gelişmiş akışlar MVP dışındadır.
- Personal etkinliklerde en az bir katılımcı bulunmalıdır.
- `Personal` görünürlük, participant listesi üzerinden belirlenir.

---

## 3.10 AuditLog
Kritik sistem değişiklikleri için denetim izi tutar.

### Alanlar
- `Id` (UUID, PK)
- `ActorUserId` (UUID, FK -> User, nullable)
- `EntityName` (string, max 100, not null)
- `EntityId` (string, not null)
- `ActionType` (string / enum, not null)  
  Allowed values:
  - `Create`
  - `Update`
  - `Delete`
  - `RoleChange`
  - `AssignmentChange`
  - `StatusChange`
  - `ManagerReassignment`
  - `ExpenseRejected`
- `OldValuesJson` (json / text, nullable)
- `NewValuesJson` (json / text, nullable)
- `OccurredAtUtc` (datetime utc, not null)
- `IpAddress` (string, nullable)

### Notlar
- Aşağıdaki olaylar en az audit edilmelidir:
  - role değişiklikleri
  - manager oluşturma / pasifleştirme
  - project manager reassignment
  - project oluşturma / arşivleme / kapatma
  - project assignment ekleme / kaldırma
  - expense rejection
- Her update işlemini audit etmek zorunlu değildir; kritik işlemler yeterlidir.

---

## 4) İlişkiler (Relationships)

1. `User (Manager)` 1 - N `Project`
   - `Project.ManagerUserId -> User.Id`

2. `Project` 1 - N `ProjectAssignment`

3. `User (Employee)` 1 - N `ProjectAssignment`

4. `User` 1 - 0..1 `RunningTimer` (aktif timer bağlamında)

5. `Project` 1 - N `RunningTimer`

6. `User` 1 - N `TimeEntry`

7. `Project` 1 - N `TimeEntry`

8. `User` 1 - N `Expense`

9. `Project` 1 - N `Expense`

10. `ExpenseCategory` 1 - N `Expense`

11. `Project` 1 - N `CalendarEvent` (opsiyonel ilişki; event personal da olabilir)

12. `CalendarEvent` N - N `User`
   - `CalendarEventParticipant` üzerinden

13. `User` 1 - N `AuditLog`
   - `AuditLog.ActorUserId -> User.Id`

---

## 5) İş Kuralları ve Constraint’ler

## 5.1 User
- `Email` unique olmalıdır.
- `Email` case-insensitive unique olmalıdır.
- `Role` yalnızca izinli değerlerden biri olmalıdır.
- `IsActive = false` olan kullanıcı login olamaz.

## 5.2 Project
- `Name` boş olamaz.
- `ManagerUserId` yalnızca `Manager` rolündeki kullanıcıya ait olabilir.
- `EndDate`, `StartDate`’ten küçük olamaz.
- Project hard delete edilmez.
- Manager reassignment işlemi audit edilmelidir.

## 5.3 ProjectAssignment
- Aynı employee aynı projede aynı anda yalnızca bir aktif atamaya sahip olabilir.
- Aktif atama için filtered unique index önerilir:
  - `(ProjectId, UserId) WHERE IsActive = true`
- Atama kaldırıldığında `UnassignedAtUtc` dolmalıdır.

## 5.4 RunningTimer
- Bir kullanıcı aynı anda yalnızca bir aktif running timer’a sahip olabilir.
- Timer yalnızca atanmış projelerde başlatılabilir.
- Stop işlemi sonrasında karşılık gelen `TimeEntry` oluşturulmalı ve timer kaydı kapatılmalı/silinmelidir.

## 5.5 TimeEntry
- `DurationMinutes > 0` olmalıdır.
- `StartTimeUtc` ve `EndTimeUtc` doluysa:
  - `EndTimeUtc > StartTimeUtc`
- Employee yalnızca atandığı projelere kayıt açabilir.
- Aynı kullanıcı için zaman çakışması engellenmelidir.
- `IsLocked = true` ise update/delete yapılamaz.
- `DeletedAtUtc IS NULL` olmayan kayıtlar aktif listelerde görünmez.

## 5.6 Expense
- `Amount > 0` olmalıdır.
- `CurrencyCode` 3 karakterli ISO kod olmalıdır.
- Employee yalnızca atandığı projelere expense girebilir.
- `Status` yalnızca izinli değerlerden biri olmalıdır.
- Geçişler:
  - `Draft -> Submitted`
  - `Submitted -> Rejected`
  - `Rejected -> Submitted`
- `RejectedReason`, `Rejected` durumunda doldurulabilir.
- Reject işlemi yalnızca ilgili manager tarafından yapılabilir.

## 5.7 CalendarEvent
- `EndAtUtc > StartAtUtc` olmalıdır.
- `VisibilityType` yalnızca izinli değerlerden biri olmalıdır.
- Personal etkinliklerde en az bir participant bulunmalıdır.
- Project bağlantılı event’lerde ilgili proje görünürlük kuralları uygulanmalıdır.

## 5.8 AuditLog
- Kritik yönetimsel işlemler audit edilmelidir.
- `OccurredAtUtc` zorunludur.
- `EntityName`, `EntityId`, `ActionType` boş olamaz.

---

## 6) Önerilen İndeksler

## 6.1 User
- Unique index: `Email`
- Index: `IsActive`
- Index: `Role`

## 6.2 Project
- Unique index: `Code` (nullable unique davranışı DB’ye göre ayarlanır)
- Index: `(ManagerUserId, Status)`
- Index: `Status`

## 6.3 ProjectAssignment
- Unique filtered index: `(ProjectId, UserId) WHERE IsActive = true`
- Index: `(UserId, IsActive)`
- Index: `(ProjectId, IsActive)`

## 6.4 RunningTimer
- Unique index: `UserId`
- Index: `(ProjectId, UserId)`
- Index: `StartedAtUtc`

## 6.5 TimeEntry
- Index: `(UserId, EntryDate)`
- Index: `(ProjectId, EntryDate)`
- Index: `(UserId, ProjectId, EntryDate)`
- Index: `(UserId, StartTimeUtc, EndTimeUtc)`

## 6.6 Expense
- Index: `(UserId, ExpenseDate)`
- Index: `(ProjectId, ExpenseDate)`
- Index: `(CategoryId, ExpenseDate)`
- Index: `(Status, ExpenseDate)`
- Index: `(UserId, ProjectId, ExpenseDate)`

## 6.7 ExpenseCategory
- Unique index: `Name`
- Index: `IsActive`

## 6.8 CalendarEvent
- Index: `(StartAtUtc, EndAtUtc)`
- Index: `(ProjectId, StartAtUtc)`
- Index: `(CreatedByUserId, StartAtUtc)`

## 6.9 CalendarEventParticipant
- PK: `(EventId, UserId)`
- Index: `(UserId, EventId)`

## 6.10 AuditLog
- Index: `(EntityName, EntityId, OccurredAtUtc)`
- Index: `(ActorUserId, OccurredAtUtc)`
- Index: `(ActionType, OccurredAtUtc)`

---

## 7) Core Tablolar

Aşağıdaki tablolar sistemin operasyonel çekirdeğini oluşturur:

1. `User`
2. `Project`
3. `ProjectAssignment`
4. `RunningTimer`
5. `TimeEntry`
6. `ExpenseCategory`
7. `Expense`
8. `CalendarEvent`
9. `CalendarEventParticipant`
10. `AuditLog`

Bu tablolar doğrudan iş akışlarını ve API CRUD operasyonlarını destekler.

---

## 8) Dinamik Üretilmesi Önerilen Raporlar

Aşağıdaki raporlar ayrı tablo yerine sorgu ile üretilmelidir:

### 8.1 Employee Personal Time Summary
Kaynak:
- `TimeEntry`

Örnek kırılımlar:
- günlük toplam saat
- haftalık toplam saat
- aylık toplam saat
- proje bazlı toplam saat

### 8.2 Employee Personal Expense Summary
Kaynak:
- `Expense`
- `ExpenseCategory`

Örnek kırılımlar:
- dönem bazlı toplam gider
- kategori bazlı toplam gider
- proje bazlı toplam gider

### 8.3 Manager Team Time Summary
Kaynak:
- `Project`
- `TimeEntry`

Örnek kırılımlar:
- employee bazlı saat
- proje bazlı saat
- tarih aralığı bazlı toplamlar

**Not**
- Bu raporlar historical veriyi kaybetmemek için yalnızca aktif assignment’a bağlı kurgulanmamalıdır.

### 8.4 Manager Team Expense Summary
Kaynak:
- `Project`
- `Expense`

Örnek kırılımlar:
- employee bazlı gider
- proje bazlı gider
- tarih aralığı bazlı gider toplamları

**Not**
- Bu raporlar historical veriyi kaybetmemek için yalnızca aktif assignment’a bağlı kurgulanmamalıdır.

### 8.5 Project Summary
Kaynak:
- `TimeEntry`
- `Expense`

Örnek kırılımlar:
- toplam süre
- toplam gider
- billable işaretli kayıt oranı

### 8.6 Calendar Density / Activity Summary
Kaynak:
- `CalendarEvent`
- `CalendarEventParticipant`

Örnek kırılımlar:
- kullanıcı bazlı etkinlik yoğunluğu
- proje bazlı etkinlik yoğunluğu

---

## 9) EF Core ve Implementasyon Notları

- `Role`, `Status`, `SourceType`, `VisibilityType`, `ParticipationType` gibi alanlar için enum + string conversion kullanılabilir.
- Soft delete kullanılan tablolarda global query filter uygulanabilir:
  - `TimeEntry`
  - `Expense`
- `Project` için soft delete yerine status tabanlı yaşam döngüsü daha uygundur.
- `AuditLog` generic tutulduğu için `EntityId` string olarak saklanabilir.
- `CreatedAtUtc` alanları insert sırasında,
- `UpdatedAtUtc` alanları update sırasında merkezi şekilde set edilmelidir.
- Authorization yalnızca controller düzeyinde değil, service/business layer düzeyinde de uygulanmalıdır.
- Timer stop akışı transaction içinde çalıştırılmalıdır:
  - `RunningTimer` okunur
  - `TimeEntry` oluşturulur
  - `RunningTimer` kaldırılır / kapatılır

---

## 10) Bu Tasarımın Sınırı

Bu model özellikle aşağıdaki kapsam için uygundur:
- tek organizasyon
- sade rol yapısı
- temel time tracking
- aktif timer desteği
- temel expense tracking
- manager scoped reporting
- proje bazlı takvim
- temel audit
- CSV export üreten raporlar

Aşağıdakiler bu sürümde **özellikle yoktur**:
- multi-tenant yapı
- gelişmiş approval workflow
- invoice / billing sistemi
- external calendar sync
- real-time collaboration
- fine-grained policy engine
- materialized reporting infrastructure
- gelişmiş concurrency/versioning altyapısı