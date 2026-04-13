# Final Authorization Matrix (MVP)

**Notlar**
- `self` = yalnızca kendi verisi
- `assigned projects` = employee’in atandığı projeler kapsamındaki veriler
- `managed projects` = yalnızca manager’ın yönettiği projeler kapsamındaki veriler
- `all` = sistem/organizasyon scope’unda tüm veriler
- Hard delete yerine mümkün olan yerlerde **soft delete / deactivate / archive** tercih edilir
- Employee kayıt oluştururken yalnızca **atanmış olduğu projeler** için işlem yapabilir
- Employee self-registration, `auth/register` akışı üzerinden yapılır; bu yüzden `users` kaynağındaki create yetkisiyle karıştırılmamalıdır

| Resource | Role | Create | Read | Update | Delete | Access Scope |
|---|---|---:|---:|---:|---:|---|
| users | Employee | Hayır | Evet | Evet | Hayır | self |
| users | Manager | Hayır | Evet | Evet (yalnızca kendi profili) | Hayır | self + managed projects users (limited profile read) |
| users | Admin | Evet | Evet | Evet | Hayır (hard delete yok) | all |
| projects | Employee | Hayır | Evet | Hayır | Hayır | assigned projects only |
| projects | Manager | Evet | Evet | Evet | Hayır (archive/close only) | managed projects |
| projects | Admin | Hayır (MVP) | Evet | Hayır (MVP, sadece manager reassignment exception) | Hayır (MVP) | all |
| project assignments | Employee | Hayır | Evet | Hayır | Hayır | self |
| project assignments | Manager | Evet | Evet | Hayır | Evet | managed projects |
| project assignments | Admin | Hayır (MVP) | Evet | Hayır (MVP) | Hayır (MVP) | all |
| time entries | Employee | Evet | Evet | Evet | Evet | self only, only for assigned projects |
| time entries | Manager | Evet | Evet | Evet | Evet | self CRUD + read team entries in managed projects |
| time entries | Admin | Hayır (MVP) | Evet | Hayır (MVP) | Hayır (MVP) | all |
| running timers | Employee | Evet | Evet | Hayır | Evet (stop/cancel) | self only, only for assigned projects |
| running timers | Manager | Evet | Evet | Hayır | Evet (stop/cancel) | self only, only for assigned projects |
| running timers | Admin | Hayır (MVP) | Hayır (MVP) | Hayır (MVP) | Hayır (MVP) | none |
| expenses | Employee | Evet | Evet | Evet | Evet | self only, only for assigned projects |
| expenses | Manager | Evet | Evet | Evet (yalnızca kendi expense kayıtları) | Evet (yalnızca kendi expense kayıtları) | self CRUD + read team expenses in managed projects |
| expenses | Admin | Hayır (MVP) | Evet | Hayır (MVP) | Hayır (MVP) | all |
| expense review actions | Employee | Evet (`submit`) | Hayır | Hayır | Hayır | self only |
| expense review actions | Manager | Evet (`reject` in managed projects) | Hayır | Hayır | Hayır | managed projects only |
| expense review actions | Admin | Hayır (MVP) | Hayır | Hayır | Hayır | none |
| reports | Employee | Hayır | Evet | Hayır | Hayır | self |
| reports | Manager | Hayır | Evet | Hayır | Hayır | managed projects only |
| reports | Admin | Hayır | Evet | Hayır | Hayır | all |
| report exports | Employee | Evet | Hayır | Hayır | Hayır | self |
| report exports | Manager | Evet | Hayır | Hayır | Hayır | self + managed projects |
| report exports | Admin | Evet | Hayır | Hayır | Hayır | all |
| calendar events | Employee | Hayır | Evet | Hayır | Hayır | own visible events + assigned project events |
| calendar events | Manager | Evet | Evet | Evet | Evet | managed projects |
| calendar events | Admin | Hayır (MVP) | Evet | Hayır (MVP) | Hayır (MVP) | all |
| audit logs | Employee | Hayır | Hayır | Hayır | Hayır | none |
| audit logs | Manager | Hayır | Hayır | Hayır | Hayır | none |
| audit logs | Admin | Hayır | Evet | Hayır | Hayır | all |

# Business Rules / Authorization Notes

## 1. General
- Manager, Employee yetkilerini kendi verileri üzerinde miras alır.
- Admin operasyonel kullanıcı değildir; MVP’de esas görevi kullanıcı/rol yönetimi, denetim ve istisnai yönetimsel müdahalelerdir.
- Tüm erişimler organizasyon/tenant filtresiyle sınırlandırılmalıdır.
- Soft delete, archive ve deactivate tercih edilmelidir.
- Employee self-registration `auth/register` ile yapılır; admin ve manager oluşturma ayrı akışlardır.

## 2. Users
- Employee yalnızca kendi profilini görebilir ve güncelleyebilir.
- Manager yalnızca kendi profilini güncelleyebilir.
- Manager, kendi yönettiği projelerdeki kullanıcıları yalnızca sınırlı profil alanlarıyla görüntüleyebilir.
- Admin kullanıcı oluşturabilir, rol atayabilir, kullanıcı durumunu değiştirebilir.
- Admin için hard delete önerilmez; deactivate/suspend kullanılmalıdır.

## 3. Projects
- Employee yalnızca atandığı projeleri görebilir.
- Manager yalnızca kendi yönettiği projeleri oluşturabilir, güncelleyebilir ve kapatabilir/arşivleyebilir.
- Project hard delete yerine `archived` / `closed` statüsü kullanılmalıdır.
- Admin normal proje CRUD yapmaz; ancak sahipsiz kalmış veya devredilmesi gereken projelerde **manager reassignment** yapabilir.

## 4. Project Assignments
- Employee yalnızca kendi proje atamalarını görebilir.
- Manager yalnızca kendi yönettiği projelere kullanıcı atayabilir veya çıkarabilir.
- Assignment tablosunda ek metadata yoksa `update` yetkisi verilmez.
- Aynı kullanıcı aynı projeye birden fazla kez aktif olarak atanamaz.

## 5. Time Entries
- Employee yalnızca kendi time entry kayıtlarında CRUD yapabilir.
- Employee yalnızca atandığı projeler için time entry oluşturabilir.
- Manager kendi kayıtlarında tam CRUD yapabilir.
- Manager ekip kayıtlarını yalnızca **read** edebilir ve bu erişim sadece yönettiği projelerle sınırlıdır.
- Manager, çalışanın başka projelerdeki kayıtlarını göremez.

## 6. Running Timers
- Aktif timer ayrı bir kaynak olarak değerlendirilir.
- Employee ve Manager aynı anda yalnızca bir aktif timer’a sahip olabilir.
- Timer yalnızca kullanıcının atandığı projeler için başlatılabilir.
- Timer stop edildiğinde tamamlanmış bir `TimeEntry` kaydına dönüştürülür.
- Admin running timer yönetmez.

## 7. Expenses
- Employee yalnızca kendi expense kayıtlarında CRUD yapabilir.
- Expense oluşturma yalnızca atandığı projeler için mümkündür.
- Manager kendi expense kayıtlarında tam CRUD yapabilir.
- Manager ekip expense kayıtlarını yalnızca **read** edebilir ve yalnızca yönettiği projeler kapsamında görebilir.
- Employee `Draft -> Submitted` geçişini yapabilir.
- Manager, yalnızca yönettiği projelerdeki `Submitted` expense kayıtlarını `Rejected` durumuna çekebilir.
- Manager ekip expense kaydının içerik alanlarını düzenlemez; yalnızca review action uygular.

## 8. Reports
- Reports ayrı bir tablo olmak zorunda değildir; time entries ve expenses üzerinden dinamik üretilebilir.
- Employee yalnızca kendi özetlerini görebilir.
- Manager yalnızca yönettiği projelere ait çalışan ve proje bazlı raporları görebilir.
- Admin tüm raporları görüntüleyebilir; backend’de ayrı `Reports.Read.All` / `Reports.Export.All` policy’leri yoktur — Admin, `Reports.Read.Team` / `Reports.Export.Team` ile tanımlı manager team uçlarını (filtrelerle) kullanır.
- Historical reports, yalnızca aktif assignment’a bağlı olmamalıdır; geçmiş time/expense kayıtları assignment sonlansa da raporlarda görünmeye devam etmelidir.
- Export işlemleri, ilgili report erişim yetkisini miras alır.

## 9. Calendar Events
- Employee yalnızca kendisine görünür olan etkinlikleri ve atandığı projelerle ilişkili etkinlikleri görebilir.
- Manager kendi yönettiği projeler için etkinlik oluşturabilir, güncelleyebilir, silebilir.
- `Personal` visibility desteklenir; ancak Employee MVP’de personal event oluşturmaz.
- `Personal` görünürlük, `CalendarEventParticipant` üzerinden sınırlandırılır.
- Calendar modülü MVP’de basit tutulur; gelişmiş davet/katılım yönetimi kapsam dışıdır.

## 10. Audit Logs
- Audit log erişimi yalnızca Admin’dedir.
- En az şu olaylar audit edilmelidir:
  - role değişiklikleri
  - manager oluşturma / pasifleştirme
  - project manager reassignment
  - project assignment ekleme / kaldırma
  - project archive / close
  - expense rejection