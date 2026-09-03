# Otomatik Migration

Uygulama ayağa kalkarken bekleyen EF Core migration'ları **otomatik uygulanır**.
`Program.cs` içinde, `DataSeeder`'dan **önce** çalışır (seeder şemanın hazır olmasını varsayar).

## Davranış

| Durum | Sonuç |
|---|---|
| Bekleyen migration yok | `"Veritabanı güncel — uygulanacak migration yok."` loglanır |
| Bekleyen migration var | Liste loglanır, uygulanır, sonuç loglanır |
| DB henüz ayakta değil | 3, 6, 9, 12 sn aralıklarla **5 deneme** |
| 5 deneme de başarısız | **Uygulama başlatılmaz** (fail-fast) |

Sunucu yeniden başlatıldığında PostgreSQL servisi IIS'ten sonra hazır olabiliyor;
yeniden deneme tam olarak bu durum için var.

**Fail-fast tercihi bilinçli:** yarım şemayla çalışan bir uygulama, hatayı sessizce
üretime taşır. Başlatmayı durdurmak sorunu dağıtım anında görünür kılar. Log'lar
Windows Event Viewer / stdout log'larında görünür.

## Kapatma

```json
// appsettings.json
"Database": {
  "AutoMigrate": false
}
```

Kapatıldığında log: `"Database:AutoMigrate = false — migration atlandı."`
Migration'ları elle uygulamak isterseniz:

```bash
dotnet ef database update --project KentOS.Kalem.Web
```

## Bu sürümde migration gerekiyor mu?

**Hayır.** Yapılan değişiklikler yalnızca kod seviyesinde; model/şema değişmedi.
Doğrulandı:

```
$ dotnet ef migrations has-pending-model-changes --project KentOS.Kalem.Web
No changes have been made to the model since the last migration.
```

Yani ilk dağıtımda migration adımı hiçbir şey yapmayacak, sadece "güncel" loglayacak.
Mekanizma bundan sonraki şema değişiklikleri için hazır.
