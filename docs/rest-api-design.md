# REST API Tasarımı

Bu API tasarımı aşağıdaki kararlarla uyumludur:
- tek organizasyon (`single-tenant`)
- rol tabanlı erişim: `Employee`, `Manager`, `Admin`
- Employee self-registration yapabilir
- Manager yalnızca **yönettiği projeler** kapsamında yetkilidir
- Admin, MVP’de **üst düzey kullanıcı/rol yönetimi ve denetim** odaklıdır
- Admin, istisnai olarak manager reassignment yapabilir
- raporlar çoğunlukla **read-only ve query tabanlıdır**
- raporlar en az CSV olarak export edilebilir
- hard delete yerine mümkün olan yerlerde **soft delete / archive / deactivate** yaklaşımı uygulanır

Temel prensipler:
1. API sürümleme: `/api/v1/...`
2. Kimlik doğrulama: JWT Bearer
3. Yetkilendirme: Role + Policy + Scope
4. Hata formatı: RFC 7807 `ProblemDetails`
5. Liste yanıtları: standart sayfalama zarfı
6. Tüm tarih/saat alanları ISO-8601 ve UTC olarak taşınır

---

## 1) Genel Sözleşmeler

### 1.1 Base Route
- Tüm uçlar `/api/v1` altında yayınlanır.

### 1.2 Authentication
- JWT Bearer token kullanılır.
- MVP’de auth uçları:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
- Logout işlemi MVP’de istemci tarafında token’ın bırakılmasıyla yönetilebilir.
- Refresh token akışı MVP kapsamı dışındadır.

### 1.3 Response Envelope (Liste Yanıtları)
Tüm liste uçlarında standart zarf önerilir:

~~~json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 245,
  "totalPages": 13,
  "hasNext": true,
  "hasPrevious": false
}
~~~

### 1.4 ProblemDetails Hata Formatı
Tüm hata yanıtları RFC 7807 uyumlu olmalıdır:

~~~json
{
  "type": "https://httpstatuses.com/403",
  "title": "Forbidden",
  "status": 403,
  "detail": "Bu kaynağa erişim yetkiniz yok.",
  "instance": "/api/v1/projects/..."
}
~~~

### 1.5 Ortak Query Parametreleri
Liste uçlarında aşağıdaki parametreler desteklenebilir:
- `page` (default: `1`)
- `pageSize` (default: `20`, max: `100`)
- `sortBy`
- `sortDir` (`asc` / `desc`)
- `q` (metin arama)
- `from`
- `to`

### 1.6 Scope Sözlüğü
- `self`: yalnızca kendi kaydı/verisi
- `assigned-projects`: employee için atandığı projeler kapsamı
- `managed-projects`: manager için yönettiği projeler kapsamı
- `all`: admin için sistem genel okuma veya yönetim kapsamı

### 1.7 Kimlik türleri (backend implementation)
Bu dokümandaki örnek JSON’larda bazı alanlar `uuid` olarak gösterilmiştir (özellikle kullanıcı kimlikleri ve soyut “id” örnekleri). **Mevcut Flux backend implementasyonunda:**
- **Kullanıcı kimlikleri** (`userId`, `managerUserId`, JWT `uid` vb.) string tabanlıdır (ASP.NET Identity ile uyumlu).
- **Proje kimlikleri** (`projectId`, proje `id`, path parametreleri `…/projects/{id}`) **`integer`** olarak taşınır: `Project` tablosu `int` identity birincil anahtar kullanır; `TimeEntry`, `Expense`, `RunningTimer`, `ProjectAssignment` ve rapor filtrelerindeki `ProjectId` alanları da **`int` foreign key**’dir.

OpenAPI / istemci tarafında proje ile ilgili alanlar **`integer` (int32)** olarak modellenmelidir. Aşağıdaki bölümlerde kalan `"uuid"` örnekleri, kullanıcı id’leri veya tarihsel doküman tutarlılığı içindir; **proje id’leri için geçerli değildir.**

---

## 2) Önerilen Policy İsimleri

- `Users.Read.Self`
- `Users.Read.Team`
- `Users.Manage.All`
- `Projects.Read.Assigned`
- `Projects.Manage.Own`
- `Projects.Reassign.Manager`
- `Assignments.Read.Self`
- `Assignments.Manage.OwnProject`
- `TimeEntries.Manage.Self`
- `TimeEntries.Read.Team`
- `RunningTimers.Manage.Self`
- `Expenses.Manage.Self`
- `Expenses.Read.Team`
- `Expenses.Reject.Team`
- `Reports.Read.Self`
- `Reports.Read.Team`
- `Reports.Read.All`
- `Reports.Export.Self`
- `Reports.Export.Team`
- `Reports.Export.All`
- `Calendar.Read.Self`
- `Calendar.Manage.OwnProject`
- `Audit.Read.All`

Not:
- Policy kontrolü yalnızca controller seviyesinde değil, service/business layer seviyesinde de tekrar doğrulanmalıdır.

---

## 3) Endpoint Grupları

1. `Auth`
2. `Users`
3. `Projects`
4. `Project Assignments`
5. `Time Entries`
6. `Running Timers`
7. `Expenses`
8. `Expense Categories`
9. `Calendar Events`
10. `Reports`
11. `Admin / Audit`

---

## 4) Endpoint Tasarımı

## 4.1 Auth

### `POST /api/v1/auth/register`
Yeni employee hesabı oluşturur.

**Yetki:** Anonim

**Request**
~~~json
{
  "firstName": "Berat",
  "lastName": "Yilmaz",
  "email": "berat@company.com",
  "password": "StrongPass123!"
}
~~~

**Davranış**
- `Role = Employee`
- `IsActive = true`

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `409 Conflict`
- `422 Unprocessable Entity`

---

### `POST /api/v1/auth/login`
Kullanıcı giriş yapar ve access token alır.

**Yetki:** Anonim

**Request**
~~~json
{
  "email": "user@company.com",
  "password": "string"
}
~~~

**Response**
~~~json
{
  "accessToken": "jwt-token",
  "expiresAtUtc": "2026-03-15T10:00:00Z",
  "user": {
    "id": "uuid",
    "fullName": "Ad Soyad",
    "email": "user@company.com",
    "role": "Employee"
  }
}
~~~

**Status Codes**
- `200 OK`
- `401 Unauthorized`
- `423 Locked` (pasif / askıya alınmış kullanıcı)
- `429 Too Many Requests`

---

## 4.2 Users

### `GET /api/v1/users/me`
Giriş yapan kullanıcının kendi profilini döner.

**Yetki:** Authenticated (`self`)

**Response**
~~~json
{
  "id": "uuid",
  "firstName": "Berat",
  "lastName": "Yilmaz",
  "email": "berat@company.com",
  "role": "Employee",
  "isActive": true,
  "lastLoginAtUtc": "2026-03-15T09:15:00Z"
}
~~~

**Status Codes**
- `200 OK`
- `401 Unauthorized`

---

### `PATCH /api/v1/users/me`
Kullanıcı kendi profil alanlarının bir kısmını günceller.

**Yetki:** Authenticated (`self`)

**Request**
~~~json
{
  "firstName": "Berat",
  "lastName": "Yilmaz"
}
~~~

**Not**
- Email ve role bu uçtan değiştirilemez.

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `409 Conflict`

---

### `GET /api/v1/users`
Kullanıcı listesi döner.

**Yetki**
- Manager: yalnızca yönettiği projelere atanmış kullanıcıları görebilir
- Admin: tüm kullanıcıları görebilir

**Filtreler**
- `role`
- `isActive`
- `q`
- `projectId`
- `page`
- `pageSize`

**Kritik Kural**
- Manager, istemciden gelen filtre ne olursa olsun yalnızca **yönettiği projelere atanmış kullanıcıları** görebilir.

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `POST /api/v1/users/managers`
Yeni manager hesabı oluşturur.

**Yetki:** Admin (`all`)

**Request**
~~~json
{
  "firstName": "Ayse",
  "lastName": "Kara",
  "email": "ayse@company.com",
  "temporaryPassword": "StrongPass123!"
}
~~~

**Response**
~~~json
{
  "id": "uuid",
  "firstName": "Ayse",
  "lastName": "Kara",
  "email": "ayse@company.com",
  "role": "Manager",
  "isActive": true
}
~~~

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `409 Conflict`

---

### `PATCH /api/v1/users/{userId}/role`
Kullanıcının rolünü günceller.

**Yetki:** Admin (`all`)

**Request**
~~~json
{
  "role": "Manager"
}
~~~

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`

---

### `PATCH /api/v1/users/{userId}/status`
Kullanıcının aktif/pasif durumunu günceller.

**Yetki:** Admin (`all`)

**Request**
~~~json
{
  "isActive": false
}
~~~

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`

---

## 4.3 Projects

### `POST /api/v1/projects`
Yeni proje oluşturur.

**Yetki:** Manager (`managed-projects` sahipliği oluşturur)

**Request**
~~~json
{
  "name": "Mobile Revamp",
  "code": "MOB-2026",
  "description": "Yeni mobil sürüm",
  "startDate": "2026-03-01",
  "endDate": "2026-06-30"
}
~~~

**Response**
~~~json
{
  "id": "uuid",
  "name": "Mobile Revamp",
  "code": "MOB-2026",
  "description": "Yeni mobil sürüm",
  "managerUserId": "uuid",
  "status": "Active",
  "startDate": "2026-03-01",
  "endDate": "2026-06-30"
}
~~~

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `409 Conflict`

---

### `GET /api/v1/projects`
Projeleri listeler.

**Yetki**
- Employee: yalnızca atandığı projeleri görebilir
- Manager: yalnızca yönettiği projeleri görebilir
- Admin: tüm projeleri görebilir

**Filtreler**
- `status`
- `managerUserId`
- `q`
- `page`
- `pageSize`

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/projects/{projectId}`
Tekil proje detayı döner.

**Yetki**
- Employee: yalnızca atandığı projelerde
- Manager: yalnızca yönettiği projelerde
- Admin: all

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

### `PATCH /api/v1/projects/{projectId}`
Projeyi günceller.

**Yetki:** Manager (`managed-projects`)

**Request**
~~~json
{
  "name": "Mobile Revamp v2",
  "description": "Güncellenmiş açıklama",
  "startDate": "2026-03-01",
  "endDate": "2026-07-15"
}
~~~

**Not**
- Admin bu uçta yazma yetkisine sahip değildir.
- Manager yalnızca kendi yönettiği projeleri güncelleyebilir.

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

### `PATCH /api/v1/projects/{projectId}/status`
Projeyi arşivler / kapatır / yeniden aktive eder.

**Yetki:** Manager (`managed-projects`)

**Request**
~~~json
{
  "status": "Archived"
}
~~~

**Allowed Values**
- `Active`
- `Archived`
- `Closed`

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`

---

### `PATCH /api/v1/projects/{projectId}/manager`
Projenin manager’ını değiştirir.

**Yetki:** Admin (`all`, governance exception)

**Request**
~~~json
{
  "managerUserId": "uuid"
}
~~~

**Not**
- Yeni manager kullanıcı rolü `Manager` olmalıdır.
- Bu uç, orphaned project riskini çözmek için istisnai olarak Admin’e açıktır.

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

## 4.4 Project Assignments

### `POST /api/v1/projects/{projectId}/assignments`
Projeye employee atar.

**Yetki:** Manager (`managed-projects`)

**Request**
~~~json
{
  "userId": "uuid"
}
~~~

**Kural**
- Aynı kullanıcı aynı projeye aktif olarak bir kez atanabilir.
- Atanacak kullanıcı Employee rolünde olmalıdır.

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

### `GET /api/v1/projects/{projectId}/assignments`
Bir projenin aktif atamalarını döner.

**Yetki**
- Manager: yalnızca yönettiği projelerde
- Admin: all

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

### `DELETE /api/v1/projects/{projectId}/assignments/{userId}`
Projeden atamayı kaldırır.

**Yetki:** Manager (`managed-projects`)

**Davranış**
- Hard delete yerine `IsActive = false` ve `UnassignedAtUtc` set edilir.

**Status Codes**
- `204 No Content`
- `403 Forbidden`
- `404 Not Found`

---

### `GET /api/v1/users/me/assignments`
Giriş yapan kullanıcının aktif proje atamalarını döner.

**Yetki:** Authenticated (`self`)

**Status Codes**
- `200 OK`
- `401 Unauthorized`

---

## 4.5 Time Entries

### `POST /api/v1/time-entries`
Yeni manuel zaman kaydı oluşturur.

**Yetki:** Employee / Manager (`self`)

**Request (range tabanlı)**
~~~json
{
  "projectId": "uuid",
  "entryDate": "2026-03-15",
  "startTimeUtc": "2026-03-15T08:00:00Z",
  "endTimeUtc": "2026-03-15T10:30:00Z",
  "description": "API geliştirme",
  "isBillable": true,
  "sourceType": "Manual"
}
~~~

**Alternatif Request (duration tabanlı)**
~~~json
{
  "projectId": "uuid",
  "entryDate": "2026-03-15",
  "durationMinutes": 150,
  "description": "API geliştirme",
  "isBillable": true,
  "sourceType": "Manual"
}
~~~

**Kurallar**
- Kullanıcı yalnızca atandığı projelere time entry oluşturabilir.
- `startTimeUtc` + `endTimeUtc` verilirse `durationMinutes` backend tarafından hesaplanır.
- `durationMinutes` ayrı verilirse `start/end` zorunlu değildir.
- Aynı kullanıcı için çakışan zaman aralıkları engellenmelidir.

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `409 Conflict`
- `422 Unprocessable Entity`

---

### `GET /api/v1/time-entries`
Zaman kayıtlarını listeler.

**Yetki**
- Employee: yalnızca kendi kayıtları
- Manager: kendi kayıtları + yönettiği projelere ait ekip kayıtları
- Admin: all read

**Filtreler**
- `userId`
- `projectId`
- `from`
- `to`
- `isBillable`
- `page`
- `pageSize`
- `sortBy`

**Kritik Kural**
- Manager yalnızca **yönettiği projelere ait** ekip kayıtlarını görebilir.
- Manager, çalışanın başka projelerdeki kayıtlarını göremez.

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/time-entries/{id}`
Tekil zaman kaydı döner.

**Yetki**
- Employee: kendi kaydı
- Manager: kendi kaydı veya yönettiği projelere ait ekip kaydı
- Admin: all

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

### `PATCH /api/v1/time-entries/{id}`
Zaman kaydını günceller.

**Yetki:** Sahip kullanıcı (`self`) ve kayıt kilitli değilse

**Not**
- Manager ekip kayıtlarını **güncelleyemez**
- Admin MVP’de time entry güncellemez

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

### `DELETE /api/v1/time-entries/{id}`
Zaman kaydını siler.

**Yetki:** Sahip kullanıcı (`self`) ve kayıt kilitli değilse

**Davranış**
- Soft delete uygulanır (`DeletedAtUtc` set edilir)

**Not**
- Manager ekip kayıtlarını silemez
- Admin MVP’de time entry silmez

**Status Codes**
- `204 No Content`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

## 4.6 Running Timers

### `POST /api/v1/time-entries/timer/start`
Aktif timer başlatır.

**Yetki:** Employee / Manager (`self`)

**Request**
~~~json
{
  "projectId": "uuid",
  "description": "API geliştirme",
  "isBillable": true
}
~~~

**Kurallar**
- Kullanıcı yalnızca atandığı projede timer başlatabilir.
- Aynı kullanıcı aynı anda yalnızca bir aktif timer’a sahip olabilir.

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `409 Conflict`

---

### `POST /api/v1/time-entries/timer/stop`
Aktif timer’ı durdurur ve tamamlanmış bir `TimeEntry` oluşturur.

**Yetki:** Employee / Manager (`self`)

**Request**
~~~json
{
  "entryDate": "2026-03-15"
}
~~~

**Davranış**
- Aktif `RunningTimer` okunur
- süre hesaplanır
- `TimeEntry` oluşturulur
- aktif timer kaldırılır

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

### `GET /api/v1/time-entries/timer/active`
Aktif timer bilgisini döner.

**Yetki:** Employee / Manager (`self`)

**Response**
~~~json
{
  "id": "uuid",
  "projectId": "uuid",
  "startedAtUtc": "2026-03-15T08:00:00Z",
  "description": "API geliştirme",
  "isBillable": true
}
~~~

**Status Codes**
- `200 OK`
- `404 Not Found`

---

## 4.7 Expenses

### `POST /api/v1/expenses`
Yeni gider kaydı oluşturur.

**Yetki:** Employee / Manager (`self`)

**Request**
~~~json
{
  "projectId": "uuid",
  "expenseDate": "2026-03-15",
  "amount": 250.75,
  "currencyCode": "TRY",
  "categoryId": "uuid",
  "notes": "Ulaşım",
  "receiptUrl": "https://example.com/receipt/123"
}
~~~

**Not**
- Backend varsayılan `status = Draft` atar.

**Kurallar**
- Kullanıcı yalnızca atandığı projelere expense oluşturabilir.
- `Amount > 0` olmalıdır.
- `CurrencyCode` 3 harfli ISO kod olmalıdır.

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `422 Unprocessable Entity`

---

### `GET /api/v1/expenses`
Gider kayıtlarını listeler.

**Yetki**
- Employee: yalnızca kendi giderleri
- Manager: kendi giderleri + yönettiği projelere ait ekip giderleri
- Admin: all read

**Filtreler**
- `userId`
- `projectId`
- `categoryId`
- `status`
- `from`
- `to`
- `page`
- `pageSize`

**Kritik Kural**
- Manager yalnızca yönettiği projelere ait ekip giderlerini görebilir.

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/expenses/{id}`
Tekil gider kaydını döner.

**Yetki**
- Employee: kendi gideri
- Manager: kendi gideri veya yönettiği projeye ait ekip gideri
- Admin: all

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

### `PATCH /api/v1/expenses/{id}`
Gider kaydını günceller.

**Yetki:** Sahip kullanıcı (`self`) ve kayıt `Draft` veya `Rejected` durumundaysa

**Not**
- Manager ekip giderlerini içerik olarak güncelleyemez
- Admin MVP’de expense güncellemez

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

### `DELETE /api/v1/expenses/{id}`
Gider kaydını siler.

**Yetki:** Sahip kullanıcı (`self`) ve kayıt `Draft` durumundaysa

**Davranış**
- Soft delete uygulanır (`DeletedAtUtc` set edilir)

**Not**
- Manager ekip giderlerini silemez
- Admin MVP’de expense silmez

**Status Codes**
- `204 No Content`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

### `POST /api/v1/expenses/{id}/submit`
Draft veya Rejected gider kaydını `Submitted` durumuna geçirir.

**Yetki:** Sahip kullanıcı (`self`)

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

### `POST /api/v1/expenses/{id}/reject`
Submitted gider kaydını `Rejected` durumuna geçirir.

**Yetki:** Manager (`managed-projects`)

**Request**
~~~json
{
  "reason": "Fiş okunamıyor"
}
~~~

**Kurallar**
- Yalnızca yönettiği projelerdeki submitted expense kayıtları reject edilebilir.
- Reject işlemi denetlenmelidir.

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`

---

## 4.8 Expense Categories

### `GET /api/v1/expense-categories`
Aktif gider kategorilerini listeler.

**Yetki:** Authenticated

**Status Codes**
- `200 OK`

---

### `POST /api/v1/expense-categories`
Yeni gider kategorisi oluşturur.

**Yetki:** Admin (`all`)

**Request**
~~~json
{
  "name": "Transportation"
}
~~~

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `409 Conflict`

---

### `PATCH /api/v1/expense-categories/{id}`
Gider kategorisini günceller.

**Yetki:** Admin (`all`)

**Request**
~~~json
{
  "name": "Travel",
  "isActive": true
}
~~~

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`

---

## 4.9 Calendar Events

### `POST /api/v1/calendar-events`
Yeni takvim etkinliği oluşturur.

**Yetki:** Manager (`managed-projects`)

**Request**
~~~json
{
  "projectId": "uuid",
  "title": "Sprint Planlama",
  "description": "Haftalık planlama",
  "startAtUtc": "2026-03-18T09:00:00Z",
  "endAtUtc": "2026-03-18T10:00:00Z",
  "visibilityType": "Project",
  "isAllDay": false,
  "participantUserIds": ["uuid", "uuid"]
}
~~~

**Kurallar**
- Manager yalnızca yönettiği projeler için etkinlik oluşturabilir.
- Employee MVP’de calendar event oluşturamaz.
- Admin MVP’de calendar event oluşturmaz.

**Status Codes**
- `201 Created`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`

---

### `GET /api/v1/calendar-events`
Takvim etkinliklerini listeler.

**Yetki**
- Employee: kendisine görünür etkinlikler + atandığı projelerin etkinlikleri
- Manager: kendi yönettiği projelerin etkinlikleri
- Admin: all read

**Filtreler**
- `from`
- `to`
- `projectId`
- `visibilityType`
- `userId`
- `page`
- `pageSize`

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/calendar-events/{id}`
Tekil takvim etkinliği döner.

**Yetki**
- Employee: görünürse
- Manager: yönettiği projeyle ilişkiliyse
- Admin: all

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

### `PATCH /api/v1/calendar-events/{id}`
Takvim etkinliğini günceller.

**Yetki:** Manager (`managed-projects`)

**Not**
- Manager yalnızca yönettiği projelere ait etkinlikleri güncelleyebilir.
- Employee MVP’de calendar event güncellemez.
- Admin MVP’de calendar event güncellemez.

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`

---

### `DELETE /api/v1/calendar-events/{id}`
Takvim etkinliğini siler.

**Yetki:** Manager (`managed-projects`)

**Not**
- Manager yalnızca yönettiği projelere ait etkinlikleri silebilir.
- Employee MVP’de calendar event silemez.
- Admin MVP’de calendar event silmez.

**Status Codes**
- `204 No Content`
- `403 Forbidden`
- `404 Not Found`

---

## 4.10 Reports

Not:
- Bu uçlar read-only analitik uçlardır.
- Ayrı report tablosu yerine operasyonel tablolardan dinamik veri üretirler.
- Historical raporlar yalnızca aktif assignment kayıtlarına bağımlı olmamalıdır.

### `GET /api/v1/reports/me/time-summary`
Kullanıcının kendi zaman özetini döner.

**Yetki:** Employee / Manager (`self`)

**Filtreler**
- `from`
- `to`
- `groupBy=day|week|month|project`

**Response**
~~~json
{
  "totalMinutes": 3120,
  "groups": [
    { "key": "2026-W11", "minutes": 960 },
    { "key": "2026-W12", "minutes": 1020 }
  ]
}
~~~

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/reports/me/expense-summary`
Kullanıcının kendi gider özetini döner.

**Yetki:** Employee / Manager (`self`)

**Filtreler**
- `from`
- `to`
- `groupBy=category|project|month`
- `currencyCode`

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/reports/manager/team-time-summary`
Manager’ın yönettiği projelere ait ekip zaman özetini döner.

**Yetki**
- Manager (`managed-projects`)
- Admin (`all`)

**Filtreler**
- `projectId`
- `userId`
- `from`
- `to`
- `groupBy=user|project|week`

**Kural**
- Manager yalnızca yönettiği projelere ait ekip verisini görebilir.
- Historical veriler, aktif assignment sonlansa bile kaybolmamalıdır.

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/reports/manager/team-expense-summary`
Manager’ın yönettiği projelere ait ekip gider özetini döner.

**Yetki**
- Manager (`managed-projects`)
- Admin (`all`)

**Filtreler**
- `projectId`
- `userId`
- `categoryId`
- `from`
- `to`
- `groupBy=user|project|month`

**Kural**
- Manager yalnızca yönettiği projelere ait ekip verisini görebilir.
- Historical veriler, aktif assignment sonlansa bile kaybolmamalıdır.

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/reports/projects/{projectId}/summary`
Belirli bir proje için özet rapor döner.

**Yetki**
- Manager (`managed-projects`)
- Admin (`all`)

**İçerik**
- toplam süre
- toplam gider
- billable işaretli kayıt oranı

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

### `GET /api/v1/reports/me/time-summary/export`
Kullanıcının kendi zaman özetini dışa aktarır.

**Yetki:** Employee / Manager (`self`)

**Query**
- `format=csv`
- `from`
- `to`
- `groupBy=day|week|month|project`

**Response**
- `text/csv`

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`

---

### `GET /api/v1/reports/me/expense-summary/export`
Kullanıcının kendi gider özetini dışa aktarır.

**Yetki:** Employee / Manager (`self`)

**Query**
- `format=csv`
- `from`
- `to`
- `groupBy=category|project|month`

**Response**
- `text/csv`

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`

---

### `GET /api/v1/reports/manager/team-time-summary/export`
Manager ekip zaman özetini dışa aktarır.

**Yetki**
- Manager (`managed-projects`)
- Admin (`all`)

**Query**
- `format=csv`
- `projectId`
- `userId`
- `from`
- `to`
- `groupBy=user|project|week`

**Response**
- `text/csv`

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`

---

### `GET /api/v1/reports/manager/team-expense-summary/export`
Manager ekip gider özetini dışa aktarır.

**Yetki**
- Manager (`managed-projects`)
- Admin (`all`)

**Query**
- `format=csv`
- `projectId`
- `userId`
- `categoryId`
- `from`
- `to`
- `groupBy=user|project|month`

**Response**
- `text/csv`

**Status Codes**
- `200 OK`
- `400 Bad Request`
- `403 Forbidden`

---

## 4.11 Admin / Audit

### `GET /api/v1/admin/audit-logs`
Audit log kayıtlarını listeler.

**Yetki:** Admin (`all`)

**Filtreler**
- `entityName`
- `actionType`
- `actorUserId`
- `from`
- `to`
- `page`
- `pageSize`

**Status Codes**
- `200 OK`
- `403 Forbidden`

---

### `GET /api/v1/admin/audit-logs/{id}`
Tekil audit log kaydını döner.

**Yetki:** Admin (`all`)

**Status Codes**
- `200 OK`
- `403 Forbidden`
- `404 Not Found`

---

## 5) Filtreleme ve Sayfalama Kuralları

1. Tüm liste uçlarında aynı sözleşme kullanılmalıdır.
2. Tarih alanları için ISO-8601 kullanılmalıdır.
3. MVP için offset pagination yeterlidir.
4. Daha yüksek ölçek için ileride keyset pagination eklenebilir.
5. `pageSize` üst limiti olmalıdır.
6. Varsayılan sıralama deterministic olmalıdır.
7. `groupBy` alanları whitelist ile sınırlandırılmalıdır.

---

## 6) Status Code Rehberi

- `200 OK`: başarılı okuma / güncelleme
- `201 Created`: başarılı oluşturma
- `204 No Content`: başarılı silme
- `400 Bad Request`: format veya validation hatası
- `401 Unauthorized`: token yok / geçersiz
- `403 Forbidden`: kimlik doğrulanmış ama yetki kapsamı dışında
- `404 Not Found`: kaynak yok veya güvenlik politikası gereği varlığı gizleniyor
- `409 Conflict`: duplicate kayıt, çakışan state, overlap, unique ihlali
- `422 Unprocessable Entity`: iş kuralı ihlali
- `423 Locked`: pasif / askıya alınmış kullanıcı veya kilitli kayıt durumu
- `429 Too Many Requests`: rate limit
- `500 Internal Server Error`: beklenmeyen hata

**Not**
- Güvenlik politikası gereği, bazı scope dışı erişimlerde `403` yerine `404` döndürülebilir.

---

## 7) DTO Tasarım Notları

### 7.1 Create / Update Ayrımı
Her kaynak için ayrı DTO önerilir:
- `RegisterRequest`
- `LoginRequest`
- `CreateProjectRequest`
- `UpdateProjectRequest`
- `ReassignProjectManagerRequest`
- `CreateTimeEntryRequest`
- `UpdateTimeEntryRequest`
- `StartTimerRequest`
- `StopTimerRequest`
- `CreateExpenseRequest`
- `UpdateExpenseRequest`
- `RejectExpenseRequest`

### 7.2 TimeEntry DTO Kuralı
Zaman kaydı oluştururken iki desteklenen model vardır:
1. aralık tabanlı giriş
2. süre tabanlı giriş

Aynı request içinde `start/end` ile `durationMinutes` arasında tutarsızlık olmamalıdır.

### 7.3 Read DTO Ayrımı
Entity’ler doğrudan dışarı açılmamalıdır.
Örnek:
- `UserProfileDto`
- `ProjectDto`
- `TimeEntryDto`
- `RunningTimerDto`
- `ExpenseDto`
- `CalendarEventDto`
- `AuditLogDto`

---

## 8) ASP.NET Core Uygulama Notları

1. Controller veya route group yaklaşımı kullanılabilir.
2. `Authorize(Policy = "...")` tercih edilmelidir.
3. Scope kontrolleri servis katmanında da tekrar edilmelidir.
4. Global exception middleware ile `ProblemDetails` standartlaştırılmalıdır.
5. Model doğrulama için `FluentValidation` önerilir.
6. Swagger / OpenAPI üzerinde her endpoint için rol ve scope açıklaması verilmelidir.
7. Soft delete kullanılan kaynaklarda global query filter uygulanabilir:
   - `TimeEntry`
   - `Expense`
8. Timer stop akışı transaction içinde çalıştırılmalıdır.

---

## 9) MVP Dışında Bırakılan API Özellikleri

Aşağıdaki uçlar veya akışlar bu sürümde özellikle **yoktur**:
- refresh token yönetimi
- server-side logout / token revocation
- password reset
- advanced approval workflow
- invoice / billing endpoints
- external calendar integration endpoints
- real-time notification endpoints
- multi-tenant administration endpoints
- fine-grained custom policy management endpoints
- PDF export zorunluluğu
- gelişmiş concurrency/versioning endpointleri

---

## 10) Sonuç

Bu API tasarımı aşağıdaki hedefler için uygundur:
- sade ama profesyonel bir backend
- employee self-registration
- role-based authorization
- manager-scoped project management
- active timer workflow
- employee-owned operational records
- expense rejection flow
- admin-governed user and audit management
- orphaned project riskini çözen manager reassignment
- query tabanlı reports
- CSV export
- MVP’ye uygun düşük karmaşıklık

Bu yapı doğrudan:
- controller tasarımına
- request/response DTO’larına
- authorization policy sabitlerine
- Swagger sözleşmesine
- service layer use case’lerine

dönüştürülebilir.