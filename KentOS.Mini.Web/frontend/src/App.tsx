import { Route, Routes } from 'react-router-dom';
import PublicDays from './screens/PublicDays';
import Applications from './screens/publicday/Applications';
import PublicDayDetail from './screens/publicday/PublicDayDetail';
import ComponentLibrary from './screens/ComponentLibrary';
import { PersonFileScreen } from './screens/publicday/PersonFile';
import HallMode from './screens/publicday/HallMode';
import { AppShell } from './shell/AppShell';
import { PERMISSION } from './components/permissions';
import { ProtectedRoute } from './auth/ProtectedRoute';
import Agenda from './screens/Agenda';
import Home from './screens/Home';
import Settings from './screens/Settings';
import InstitutionSettings from './screens/InstitutionSettings';
import OpenIdSettings from './screens/OpenIdSettings';
import PublicForm from './screens/form/PublicForm';
import Forms from './screens/Forms';
import FormDesigner from './screens/form/FormDesigner';
import FormResponses from './screens/form/FormResponses';
import NotFound from './screens/NotFound';
import Flowers from './screens/Flowers';
import FloristDetail from './screens/flower/FloristDetail';
import EventDetail from './screens/EventDetail';
import EventFormPage from './screens/EventFormPage';
import FileTransfer, { FileTransferDetail } from './screens/FileTransfer';
import NotificationsScreen from './screens/Notifications';
import HelpCenter from './help/HelpCenter';
import InvitationDetail from './screens/InvitationDetail';
import Invitations from './screens/Invitations';
import ResumePool from './screens/ResumePool';
import Protocol from './screens/Protocol';
import ProtocolDetail from './screens/protocol/ProtocolDetail';
import RequestFormPage from './screens/RequestForm';
import Login from './screens/Login';
import ReportPortal from './screens/citizen/ReportPortal';
import CitizenReports from './screens/CitizenReports';
import WorkMapScreen from './screens/WorkMapScreen';
import Inbox from './screens/Inbox';
import WorkDashboard from './screens/WorkDashboard';
import FieldHome from './screens/field/FieldHome';
import FieldReport from './screens/field/FieldReport';
import FieldTask from './screens/field/FieldTask';
import { PortalLayout } from './shell/PortalLayout';
import { SahaLayout } from './shell/SahaLayout';
import StatisticsHub from './screens/statistics/StatisticsHub';
import EventDashboard from './screens/statistics/EventDashboard';
import RequestScreen from './screens/statistics/RequestScreen';
import TopicDashboard from './screens/statistics/TopicDashboard';
import CalendarScreen from './screens/CalendarPage';
import RequestDetail from './screens/RequestDetail';
import Requests from './screens/Requests';
import Tasks from './screens/Tasks';
import TaskDetail from './screens/TaskDetail';
import TaskForm from './screens/task/TaskForm';
import TaskTypes from './screens/task/TaskTypes';
import Teams from './screens/Teams';
import Projects from './screens/Projects';
import ProjectDetail from './screens/ProjectDetail';
import ProjectForm from './screens/project/ProjectForm';
import Definitions from './screens/Definitions';
import SystemErrors, { SystemErrorDetail } from './screens/SystemErrors';
import Administration from './screens/Administration';
import UnitDetailScreen from './screens/admin/UnitDetail';
import RoleDetail from './screens/admin/RoleDetail';
import { FlowerDelivery } from './screens/flower/FlowerDelivery';

/**
 * Rota tablosu — design.md §6.
 *
 * `basename="/"` main.tsx'te veriliyor; buradaki yollar belgeyle birebir.
 * Modül seçme ekranı YOK: giriş sonrası doğrudan Ana Sayfa.
 *
 * Politika kontrolü rota düzeyinde: yetkisi olmayan bir kullanıcı ekranı
 * hiç görmez, 403 duvarına da çarpmaz. Yetkinin KAYNAĞI yine sunucudur.
 */
/**
 * Ajanda ekranları İKİ izinden biriyle açılır.
 *
 * Basın kullanıcısında `ajanda.goruntule` yok, `ajanda.basinGoruntule` var:
 * ekranı açar ama sunucu listeyi yalnızca "basın katılacak" kayıtlara indirir.
 * Tek izne bağlansaydı basın kullanıcısı kendi ajandasını hiç göremezdi.
 */
const AJANDA_GORME = [PERMISSION.ajandaGoruntule, PERMISSION.ajandaBasinGoruntule];

export default function App() {
  return (
    <Routes>
      <Route path="/giris" element={<Login />} />

      {/*
        VATANDAŞ PORTALI — KABUKSUZ ve ANONİM.

        `ProtectedRoute` YOK: portalın tamamı oturum açmamış vatandaş için.
        `AppShell` de yok — kurumsal menüyü göstermek anlamsız, orada kimse
        oturum açmış değil.
      */}
      <Route element={<PortalLayout />}>
        <Route path="/bildir" element={<ReportPortal />} />
      </Route>

      {/*
        ÇİÇEK TESLİM FİŞİ — ANONİM ve KABUKSUZ.

        Çiçekçi kurumun kullanıcısı değil; bağlantı ona SMS ile gidiyor ve tek
        yetki belirteci adresteki tahmin edilemez kimlik. `ProtectedRoute` ve
        `AppShell` yok: giriş ekranına yönlendirmek ya da yirmi yedi menü
        öğesi göstermek, oturumu hiç olmayan birine anlamsız.

        Adres SMS'te geçiyor (`AjandaService`), yani DEĞİŞTİRİLEMEZ: eski
        bağlantılar hâlâ gelen kutularında duruyor.
      */}
      <Route path="/cicek-teslim/:kimlik" element={<FlowerDelivery />} />

      {/*
        VATANDAŞ FORMU — uygulama kabuğunun DIŞINDA.

        Bağlantıyı açan kişi kurumun personeli değil; ona menü, sekme çubuğu
        ve bildirim zili göstermek "yanlış yere geldim" hissi veriyor.
        Çiçek teslim ekranıyla aynı gerekçe.
      */}
      <Route path="/form/:anahtar" element={<PublicForm />} />

      {/*
        SAHA — KENDİ KABUĞUNDA ve KİMLİK DOĞRULAMALI.

        Panelin kabuğu yirmi yedi menü öğesi taşıyan kurumsal bir yüzey;
        sahada yapılan iş ise üç şeyden biri. `SahaLayout` renkli üst şerit
        ve alt sekme çubuğuyla telefonda yerli uygulama gibi duruyor,
        sekmeleri de kullanıcının izinlerine göre kuruyor.
      */}
      <Route
        element={
          <ProtectedRoute>
            <SahaLayout />
          </ProtectedRoute>
        }
      >
        <Route
          path="/saha"
          element={<ProtectedRoute permission={PERMISSION.gorevGoruntule}><FieldHome /></ProtectedRoute>}
        />
        <Route
          path="/saha/tespit"
          element={<ProtectedRoute permission={[PERMISSION.sahaTespit, PERMISSION.gorevEkle]}><FieldReport /></ProtectedRoute>}
        />
        {/* Panelin haritasıyla AYNI bileşen: iki harita, iki yerde
            düzeltilecek bir kümeleme mantığı demekti. */}
        <Route
          path="/saha/harita"
          element={<ProtectedRoute permission={PERMISSION.gorevGoruntule}><WorkMapScreen /></ProtectedRoute>}
        />
        <Route
          path="/saha/gorev/:id"
          element={<ProtectedRoute permission={PERMISSION.gorevGoruntule}><FieldTask /></ProtectedRoute>}
        />
      </Route>

      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route index element={<Home />} />

        <Route
          path="talepler"
          element={<ProtectedRoute permission={PERMISSION.talepGoruntule} policy="Ajanda"><Requests /></ProtectedRoute>}
        />
        {/* `yeni` ve `:id/duzenle` DETAYDAN ÖNCE; aksi hâlde "yeni" bir
            kimlik sanılır ve detay ekranı 404 gösterir. */}
        {/* Form MODAL: arkasına liste çizilir ki kapatınca boş ekran kalmasın
            ve kullanıcı listedeki yerini kaybetmesin. */}
        <Route
          path="talepler/yeni"
          element={
            <ProtectedRoute permission={PERMISSION.talepGoruntule} policy="Ajanda">
              <>
                <Requests />
                <RequestFormPage />
              </>
            </ProtectedRoute>
          }
        />
        <Route
          path="talepler/:id/duzenle"
          element={<ProtectedRoute permission={PERMISSION.talepGoruntule} policy="Ajanda"><RequestFormPage /></ProtectedRoute>}
        />
        <Route
          path="talepler/:id"
          element={<ProtectedRoute permission={PERMISSION.talepGoruntule} policy="Ajanda"><RequestDetail /></ProtectedRoute>}
        />

        {/*
          İŞ TAKİP.

          `politika` YOK ve bu bilinçli: `Ajanda` politikası makam rollerine
          (Admin/Sekreter/Yonetici/Baskan) açık, iş takip ise birimlerin
          işi — park bahçelerin şefi o politikada değil ama görevini
          görebilmeli. Kapı yalnızca izin.
        */}
        {/* `tipler` ve `yeni` DETAYDAN ÖNCE; aksi hâlde kimlik sanılır. */}
        <Route
          path="gorevler/tipler"
          element={<ProtectedRoute permission={PERMISSION.gorevTipYonet}><TaskTypes /></ProtectedRoute>}
        />
        {/* Form MODAL: arkasına liste çizilir ki kapatınca boş ekran kalmasın. */}
        <Route
          path="gorevler/yeni"
          element={
            <ProtectedRoute permission={PERMISSION.gorevEkle}>
              <>
                <Tasks />
                <TaskForm />
              </>
            </ProtectedRoute>
          }
        />
        <Route
          path="gorevler/:id/duzenle"
          element={<ProtectedRoute permission={PERMISSION.gorevDuzenle}><TaskForm /></ProtectedRoute>}
        />
        <Route
          path="gorevler/:id"
          element={<ProtectedRoute permission={PERMISSION.gorevGoruntule}><TaskDetail /></ProtectedRoute>}
        />
        <Route
          path="gorevler"
          element={<ProtectedRoute permission={PERMISSION.gorevGoruntule}><Tasks /></ProtectedRoute>}
        />
        {/* `yeni` DETAYDAN ÖNCE; aksi hâlde kimlik sanılır. */}
        <Route
          path="projeler/yeni"
          element={
            <ProtectedRoute permission={PERMISSION.projeYonet}>
              <>
                <Projects />
                <ProjectForm />
              </>
            </ProtectedRoute>
          }
        />
        <Route
          path="projeler/:id/duzenle"
          element={<ProtectedRoute permission={PERMISSION.projeYonet}><ProjectForm /></ProtectedRoute>}
        />
        <Route
          path="projeler/:id"
          element={<ProtectedRoute permission={PERMISSION.projeGoruntule}><ProjectDetail /></ProtectedRoute>}
        />
        <Route
          path="projeler"
          element={<ProtectedRoute permission={PERMISSION.projeGoruntule}><Projects /></ProtectedRoute>}
        />
        <Route
          path="vatandas-bildirimleri"
          element={<ProtectedRoute permission={PERMISSION.bildirimKarsila}><CitizenReports /></ProtectedRoute>}
        />
        <Route
          path="gelen-kutusu"
          element={<ProtectedRoute permission={PERMISSION.gelenKutusuGoruntule}><Inbox /></ProtectedRoute>}
        />
        <Route
          path="is-panosu"
          element={<ProtectedRoute permission={PERMISSION.isIstatistik}><WorkDashboard /></ProtectedRoute>}
        />
        <Route
          path="harita"
          element={<ProtectedRoute permission={PERMISSION.gorevGoruntule}><WorkMapScreen /></ProtectedRoute>}
        />
        <Route
          path="ekipler"
          element={<ProtectedRoute permission={[PERMISSION.ekipYonet, PERMISSION.gorevGoruntule]}><Teams /></ProtectedRoute>}
        />

        <Route
          path="ajanda"
          element={<ProtectedRoute permission={AJANDA_GORME} policy="Ajanda"><Agenda /></ProtectedRoute>}
        />
        {/* `yeni` ve `:id/duzenle` DETAYDAN ÖNCE; aksi hâlde "yeni" bir
            kimlik sanılır ve detay ekranı 404 gösterir. */}
        <Route
          path="ajanda/yeni"
          element={<ProtectedRoute permission={AJANDA_GORME} policy="Ajanda"><EventFormPage /></ProtectedRoute>}
        />
        <Route
          path="ajanda/:id/duzenle"
          element={<ProtectedRoute permission={AJANDA_GORME} policy="Ajanda"><EventFormPage /></ProtectedRoute>}
        />
        <Route
          path="ajanda/:id"
          element={<ProtectedRoute permission={AJANDA_GORME} policy="Ajanda"><EventDetail /></ProtectedRoute>}
        />

        <Route
          path="takvim"
          element={<ProtectedRoute permission={AJANDA_GORME} policy="Ajanda"><CalendarScreen /></ProtectedRoute>}
        />
        <Route
          path="protokol"
          element={<ProtectedRoute permission={PERMISSION.protokolGoruntule} policy="Ajanda"><Protocol /></ProtectedRoute>}
        />
        {/* Kişi dosyası: bilgiler + davet edildiği programlar. */}
        <Route
          path="protokol/:id"
          element={<ProtectedRoute permission={PERMISSION.protokolGoruntule} policy="Ajanda"><ProtocolDetail /></ProtectedRoute>}
        />
        {/*
          HALK GÜNÜ — rota SIRASI: `basvurular` mutlaka `:id`'den ÖNCE,
          yoksa "basvurular" bir kimlik sanılır ve ayrıntı 404 verir.
        */}
        <Route
          path="halk-gunu"
          element={<ProtectedRoute permission={PERMISSION.halkgunuGoruntule} policy="Ajanda"><PublicDays /></ProtectedRoute>}
        />
        <Route
          path="halk-gunu/basvurular"
          element={<ProtectedRoute permission={PERMISSION.halkgunuGoruntule} policy="Ajanda"><Applications /></ProtectedRoute>}
        />
        {/* `kisi` rotası `:id`den ÖNCE: aksi hâlde kimlik sanılır. */}
        <Route
          path="halk-gunu/kisi"
          element={<ProtectedRoute permission={PERMISSION.halkgunuGoruntule} policy="Ajanda"><PersonFileScreen /></ProtectedRoute>}
        />
        <Route
          path="halk-gunu/:id/salon"
          element={<ProtectedRoute permission={PERMISSION.halkgunuGorusme} policy="Ajanda"><HallMode /></ProtectedRoute>}
        />
        <Route
          path="halk-gunu/:id"
          element={<ProtectedRoute permission={PERMISSION.halkgunuGoruntule} policy="Ajanda"><PublicDayDetail /></ProtectedRoute>}
        />
        {/* Tasarım denetimi — izin kapısı yok: kimliği doğrulanmış
            herkes tema uyumunu görebilmeli. */}
        <Route path="bilesenler" element={<ComponentLibrary />} />
        <Route
          path="ozgecmisler"
          element={
            <ProtectedRoute permission={PERMISSION.ozgecmisGoruntule} policy="Ajanda">
              <ResumePool />
            </ProtectedRoute>
          }
        />
        <Route
          path="davetler"
          element={<ProtectedRoute permission={PERMISSION.davetGoruntule} policy="Ajanda"><Invitations /></ProtectedRoute>}
        />
        <Route
          path="davetler/:id"
          element={<ProtectedRoute permission={PERMISSION.davetGoruntule} policy="Ajanda"><InvitationDetail /></ProtectedRoute>}
        />

        {/* Dosya gönderimi POLİTİKA İSTEMEZ: kendisine dosya gönderilen
            herkes — rolü ne olursa olsun — kaydı açabilmeli. Gönderme
            yetkisi ekranın içinde ve sunucuda denetleniyor. */}
        <Route path="gonderim" element={<FileTransfer />} />
        <Route path="gonderim/:id" element={<FileTransferDetail />} />
        {/*
          İSTATİSTİK MERKEZİ — merkez + konu sayfaları.

          Merkezin kendisi İZİN İSTEMEZ: kartlar zaten tek tek süzülüyor ve
          hiçbiri kalmazsa ekran bunu söylüyor. Merkeze `istatistik.goruntule`
          konsaydı, halk günü sayılarını görmeye yetkili ama makam
          istatistiğine yetkisiz bir kullanıcı kapıda kalırdı.

          Konu sayfalarının kapısı KENDİ modül izni; asıl kapı sunucuda.
        */}
        <Route path="istatistikler" element={<StatisticsHub />} />
        <Route
          path="istatistikler/etkinlik"
          element={<ProtectedRoute permission={PERMISSION.istatistikGoruntule} role="Admin"><EventDashboard /></ProtectedRoute>}
        />
        <Route
          path="istatistikler/talep"
          element={<ProtectedRoute permission={PERMISSION.istatistikGoruntule} role="Admin"><RequestScreen /></ProtectedRoute>}
        />
        {/* Genel şekli kullanan konular TEK rotadan geçer; `konu` parametresi
            hem uç adresini hem katalogdaki kaydı seçiyor. */}
        <Route path="istatistikler/:konu" element={<TopicDashboard />} />
        <Route
          path="cicek"
          element={<ProtectedRoute permission={PERMISSION.cicekGoruntule} policy="Cicek"><Flowers /></ProtectedRoute>}
        />
        {/* Çiçekçi dosyası: talimatlar, bağlı programlar, dönem süzgeci, çıktı. */}
        <Route
          path="cicek/:id"
          element={<ProtectedRoute permission={PERMISSION.cicekGoruntule} policy="Cicek"><FloristDetail /></ProtectedRoute>}
        />
        <Route
          path="yonetim"
          element={<ProtectedRoute permission={PERMISSION.yonetimKullanici} role="Admin"><Administration /></ProtectedRoute>}
        />
        <Route
          path="yonetim/birimler/:id"
          element={<ProtectedRoute permission={PERMISSION.yonetimKullanici} role="Admin"><UnitDetailScreen /></ProtectedRoute>}
        />
        <Route
          path="yonetim/roller/:ad"
          element={<ProtectedRoute permission={PERMISSION.yonetimKullanici} role="Admin"><RoleDetail /></ProtectedRoute>}
        />
        {/* Hata kayıtları YALNIZCA Sistem rolüne açık — Admin bile göremez.
            Kayıtlarda istek gövdeleri, IP adresleri ve yığın izleri var; hem
            kişisel veri hem saldırı yüzeyini tarif eden bilgi. */}
        <Route
          path="hatalar"
          element={<ProtectedRoute permission={PERMISSION.sistemHata} role="Sistem"><SystemErrors /></ProtectedRoute>}
        />
        <Route
          path="hatalar/:id"
          element={<ProtectedRoute permission={PERMISSION.sistemHata} role="Sistem"><SystemErrorDetail /></ProtectedRoute>}
        />
        <Route
          path="tanimlar"
          element={<ProtectedRoute permission={PERMISSION.tanimYonet} role="Admin"><Definitions /></ProtectedRoute>}
        />
        {/*
          KURUM BİLGİLERİ. Değişiklik BÜTÜN kullanıcıların gördüğü yüzü
          etkiliyor — kurum adı, amblem ve kurumsal renk — bu yüzden kendi
          izniyle kapalı.
        */}
        <Route
          path="kurum"
          element={<ProtectedRoute permission={PERMISSION.sistemKurum} role="Admin"><InstitutionSettings /></ProtectedRoute>}
        />

        {/*
          FORM VE ANKET.

          Rota sırası: `yeni` ve `:id/yanitlar` DETAYDAN ÖNCE. Aksi hâlde
          `yeni` bir kimlik sanılır ve tasarımcı 404 gösterir — bu depoda
          adı konmuş bir tuzak.
        */}
        <Route
          path="formlar"
          element={<ProtectedRoute permission={PERMISSION.formGoruntule}><Forms /></ProtectedRoute>}
        />
        <Route
          path="formlar/yeni"
          element={<ProtectedRoute permission={PERMISSION.formYonet}><FormDesigner yeni /></ProtectedRoute>}
        />
        <Route
          path="formlar/:id/yanitlar"
          element={<ProtectedRoute permission={PERMISSION.formYanitGoruntule}><FormResponses /></ProtectedRoute>}
        />
        <Route
          path="formlar/:id"
          element={<ProtectedRoute permission={PERMISSION.formGoruntule}><FormDesigner yeni={false} /></ProtectedRoute>}
        />

        {/* Kimlik sağlayıcı ayrı bir izinle korunuyor (`sistem.openid`):
            yanlış girildiğinde giriş ekranındaki düğme çalışmaz, yani
            kurum bilgisi düzenlemekten daha dar bir yetki istiyor. */}
        <Route
          path="kimlik-saglayici"
          element={<ProtectedRoute permission={PERMISSION.sistemOpenid} role="Admin"><OpenIdSettings /></ProtectedRoute>}
        />

        <Route path="bildirimler" element={<NotificationsScreen />} />
        {/*
          YARDIM MERKEZİ izin kapısı TAŞIMAZ.

          Menüdeki her ekran izne bağlı ama yardım metni bir yetki değil bir
          açıklama; kapatmak, ekranı göremeyen kişinin "bu ekran ne işe
          yarıyordu?" sorusunu da cevapsız bırakırdı.
        */}
        <Route path="yardim" element={<HelpCenter />} />
        <Route path="ayarlar" element={<Settings />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}
