# Proje Detayı

Dört sekme: **Özet**, **Pano**, **Gantt** ve **Görevler**.

## Özet

**Kilometre taşları** ve **proje ekibi** burada.

Kilometre taşının yanındaki daireye basarak tamamlandı işaretleyebilir,
tekrar basarak geri açabilirsiniz. Tamamlanma **elle** işaretlenir: "bağlı
görevlerin hepsi bitince kendiliğinden" denseydi, hiç görev bağlanmamış bir
hedef açılır açılmaz tamamlanmış görünürdü.

**Proje ekibi** ayrı bir yetkiye bağlıdır (`proje.uyeYonet`). Sebebi: ekibi
düzenlemek ile projenin tarihini ve bütçesini değiştirmek farklı ağırlıkta
işlerdir; proje yöneticisine ekibini kurma yetkisi verirken bütçeyi de
açmak gerekmesin. **Proje yöneticisi ekibin üyesi olmalıdır.**

## Pano (Kanban)

Kartları sütunlar arasında sürükleyebilirsiniz. **Sürüklemek görevin
durumunu değiştirir** — sütunlar görev durumlarına eşlidir, ayrı bir durum
kaynağı değildir. Böylece "panoda Tamamlandı ama listede Devam Ediyor"
çelişkisi oluşamaz.

**Görev akışı panoda da geçerlidir.** Atanmamış bir görevi "Devam ediyor"a,
onaydan geçmemiş bir görevi "Tamamlandı"ya sürükleyemezsiniz; sunucu reddeder
ve size sebebini söyler. Onay kapısı panodan atlanamaz.

**Sütunsuz** başlıklı sütun, durumuna karşılık gelen bir sütun bulunmayan
görevleri gösterir. Buraya kart bırakılamaz; amacı hiçbir işin panodan
sessizce kaybolmamasıdır.

Sütun içinde elle sıralama yoktur: kartlar önce önceliğe, sonra en az vakti
kalana göre dizilir.

## Gantt

Kilometre taşları **elmas**, görevler **çubuk** olarak çizilir. Çubuğun
koyu kısmı tamamlanan aşama oranıdır. Kırmızı kesikli çizgi **bugünü**
gösterir — gecikmeyi çizgiye bakarak okumak, tarihleri tek tek
karşılaştırmaktan hızlıdır.

Alttaki kaydırıcı ile tarih aralığını daraltabilirsiniz.

**Tarihi olmayan satır çizilmez.** Başlangıcı ve bitişi belli olmayan bir
işi çizmek onu ya bugüne ya sonsuza yapıştırırdı; ikisi de yanlış bilgi
olurdu.

## Projeyi silme

**Görevler silinmez.** Kilometre taşları, pano ve ekip gider; görevlerin
proje bağı boşalır ve görev listesinde durmaya devam ederler. Proje bir
çatıdır, işin sahibi değil.
