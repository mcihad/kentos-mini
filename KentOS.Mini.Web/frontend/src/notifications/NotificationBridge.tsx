import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useToast } from '../components/Toast';
import { useSession } from '../auth/SessionProvider';
import { queryKeys } from '../data/queryKeys';
import { notificationPath, jetonuTazele, onForegroundMessage } from './fcm';

/**
 * Web push ile uygulamayı birbirine bağlar.
 *
 * Üç iş yapar:
 * 1. Oturum açıldığında jetonu sessizce tazeler (izin ZATEN varsa).
 * 2. Uygulama açıkken gelen bildirimi toast'a çevirir.
 * 3. Arka plan bildirimine tıklanınca service worker'ın gönderdiği yolu
 *    uygulama içi gezinmeye çevirir.
 *
 * <p>
 * Görsel çıktısı yok; AppShell içine bir kez asılır. Router'ın İÇİNDE olmak
 * zorunda — `useNavigate` aksi hâlde çalışmaz.
 * </p>
 */
export function NotificationBridge() {
  const { me } = useSession();
  const { bildir } = useToast();
  const gezin = useNavigate();
  const qc = useQueryClient();

  // 1) Girişten sonra jetonu tazele.
  useEffect(() => {
    if (me) void jetonuTazele();
  }, [me]);

  // 2) Ön plan bildirimleri.
  useEffect(() => {
    if (!me) return;

    const birak = onForegroundMessage((baslik, govde, veri) => {
      /*
        Ön plan bildiriminden OTOMATİK YÖNLENDİRME YAPILMAZ: kullanıcının
        okuduğu ekrandan habersizce koparılması, bildirimin kendisinden daha
        rahatsız edici.

        Ama şerit ARTIK TIKLANABİLİR. Kural şuydu: "tarayıcı bildirimine
        tıklayınca etkinliğe gidiliyor, uygulama açıkken gelen şeritle
        gidilemiyor" — aynı bildirim, hangi pencerede olduğuna göre farklı
        davranıyordu. Karar kullanıcıda kalıyor, yalnızca yol açılıyor.
      */
      const yol = notificationPath(veri);
      bildir(
        'bilgi',
        baslik,
        govde,
        yol ? { eylem: () => gezin(yol), eylemEtiketi: 'Aç' } : undefined,
      );

      // Bildirim merkezi rozetini hemen güncelle.
      qc.invalidateQueries({ queryKey: ['bildirim'] });

      // Bildirim, açık bir ekranın verisini bayatlatmış olabilir:
      const varlik = veri?.entity?.toLowerCase();

      if (varlik === 'ajanda') {
        qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      } else if (varlik === 'talep') {
        qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      } else if (varlik === 'dosya') {
        qc.invalidateQueries({ queryKey: ['gonderim'] });
      } else if (varlik === 'ozgecmis') {
        qc.invalidateQueries({ queryKey: ['ozgecmis'] });
      } else if (varlik === 'gorev') {
        /*
          GÖREV BİLDİRİMİ ÜÇ ÖNBELLEĞİ BİRDEN DÜŞÜRÜYOR.

          Atama, durum değişimi ve süre aşımı; görev listesinde de, bağlı
          olduğu projenin panosunda da, gecikme panosunda da görünüyor.
          Yalnızca görev anahtarını düşürmek, açık duran bir proje panosunda
          eski durumu bırakırdı.
        */
        qc.invalidateQueries({ queryKey: ['gorev'] });
        qc.invalidateQueries({ queryKey: ['proje'] });
        qc.invalidateQueries({ queryKey: ['is-istatistik'] });
      } else if (varlik === 'proje') {
        qc.invalidateQueries({ queryKey: ['proje'] });
      } else if (varlik === 'gelenkutusu') {
        // Bekleyen sayısı menüdeki rozeti besliyor: devir bildirimi gelince
        // rozet hemen artmalı, bir dakikalık tazelik süresi beklenmemeli.
        qc.invalidateQueries({ queryKey: ['gelen-kutusu'] });
        qc.invalidateQueries({ queryKey: ['gorev'] });
      }
    });

    return () => birak();
  }, [me, bildir, gezin, qc]);

  // 3) Service worker'dan gelen yönlendirme.
  useEffect(() => {
    if (!('serviceWorker' in navigator)) return;

    const dinle = (olay: MessageEvent) => {
      if (olay.data?.tur !== 'bildirim-yolu' || typeof olay.data.yol !== 'string') return;

      // Service worker MUTLAK yol gönderiyor; router'ın
      // basename'i `/yeni` olduğu için öneki burada sıyırmak gerekiyor.
      // Uygulama köke taşındı ama KURULU eski service worker'lar bir süre
      // daha `/yeni/...` göndermeye devam edecek: kullanıcı sayfayı yenileyene
      // kadar eski worker etkin kalıyor. Önek varsa soyuluyor.
      const yol = (olay.data.yol as string).replace(/^\/yeni/, '') || '/';
      gezin(yol);
    };

    navigator.serviceWorker.addEventListener('message', dinle);
    return () => navigator.serviceWorker.removeEventListener('message', dinle);
  }, [gezin]);

  return null;
}
