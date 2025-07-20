# ConfigurationSystem

`ConfigurationSystem`, .NET projelerinde merkezi ve dinamik konfigürasyon yönetimini kolaylaştırmak amacıyla geliştirilmiş bir sistemdir. MongoDB kullanılarak yapılandırılmış bu sistem, cache destekli çalışır ve her proje yalnızca kendi ayarlarını yönetebilir.

## 📦 ConfigurationReader Kütüphanesi

`ConfigurationReader` kütüphanesi uygulama başlatıldığında otomatik olarak şu işlemleri yapar:

- Projeye özel ayarları MongoDB ile kayıt eder.
- Eğer veri yoksa oluşturur, varsa günceller.
- `IsActive = true` olan verileri cache üzerinde tutar.
- Kullanıcı arayüzü üzerinden ekleme, düzenleme ve silme işlemleri yapılabilir.
- Her proje sadece kendi verilerini görür.

Veritabanı: **MongoDB**

---

## 🛠️ Kullanılabilir Metodlar

| Metot | Açıklama |
|-------|----------|
| `GetAllConfigurationsAsync` | Projeye ait tüm konfigürasyon verilerini getirir. |
| `GetConfigurationByIdAsync` | Belirtilen ID'ye ait veriyi getirir. |
| `GetConfigurationByNameAsync` | Belirtilen isme ait veriyi getirir. |
| `SearchConfigurationsByNameAsync` | İsim üzerinden arama ve filtreleme yapılmasını sağlar. |
| `CreateConfigurationAsync` | Yeni bir konfigürasyon kaydı oluşturur. |
| `UpdateConfigurationAsync` | Mevcut kaydı günceller. |
| `DeleteConfigurationAsync` | Kaydı silmez, pasif hale getirir (`IsActive = false`). |
| `GetValue` | Belirtilen key için value değerini döner. |
| `SyncConfigurationsFromAppSettings` | `appsettings.json` verilerini senkronize eder, eksik olanları ekler, varsa günceller. |
| `ExtractConfigurationsFromSection` | `appsettings` içeriğindeki tüm konfigürasyonları ayrıştırır. |
| `DetermineValueType` | Girilen değerin türünü belirler (örnek: string, int, bool). |
| `ConvertValue` | Veriyi string tipine çevirir. |
| `LoadConfigurations` | `IsActive = true` olan verileri cache'e yükler. |

---

## 🧩 Teknolojiler

- .NET 8
- MongoDB
- Memory Cache
- Docker

---

## 📁 Proje Yapısı

- `ConfigurationReader/` – Konfigürasyon kütüphanesi
- `ConfigurationSystem.Api/` – API projesi
- `docker-compose.yml` – Tüm sistemi kapsayan Docker konfigürasyonu

---

## 🐳 Docker ile Projeyi Çalıştırma

Projeyi Docker kullanarak kolayca başlatabilirsiniz.

