# ═══════════════════════════════════════════════════════════════════════════
#  KentOS.Kalem — kapsayıcı imajı
#
#  Üç aşama:
#    1. onyuz   — Node ile SPA derlemesi (Vite)
#    2. sunucu  — .NET SDK ile yayın çıktısı
#    3. çalışma — yalnızca ASP.NET çalışma zamanı
#
#  Ön yüz AYRI bir aşamada derleniyor, `dotnet publish`in içindeki MSBuild
#  hedefiyle değil. Sebep katman önbelleği: `package-lock.json` değişmediği
#  sürece `npm ci` yeniden koşmuyor. Aynı işi tek aşamada yapmak, her C#
#  değişikliğinde bütün bağımlılıkları yeniden indirmek demekti.
# ═══════════════════════════════════════════════════════════════════════════


# ── 1. Ön yüz ──────────────────────────────────────────────────────────────
FROM node:22-slim AS onyuz
WORKDIR /kaynak/KentOS.Kalem.Web/frontend

# Önce yalnızca bağımlılık bildirimi: kaynak değişince bu katman korunur.
COPY KentOS.Kalem.Web/frontend/package.json KentOS.Kalem.Web/frontend/package-lock.json ./
RUN npm ci

COPY KentOS.Kalem.Web/frontend/ ./
# Vite çıktısı `../wwwroot` — yani /kaynak/KentOS.Kalem.Web/wwwroot.
# `emptyOutDir` kapalı olduğu için var olan dosyaları silmez.
RUN npm run build


# ── 2. Sunucu ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS sunucu
WORKDIR /kaynak

# Önce proje dosyaları: NuGet geri yüklemesi kaynak değişikliğinden bağımsız
# olarak önbellekte kalsın.
COPY KentOS.Kalem.sln ./
COPY KentOS.Kalem.Application/KentOS.Kalem.Application.csproj KentOS.Kalem.Application/
COPY KentOS.Kalem.Web/KentOS.Kalem.Web.csproj                 KentOS.Kalem.Web/
COPY KentOS.Kalem.Tests/KentOS.Kalem.Tests.csproj             KentOS.Kalem.Tests/
RUN dotnet restore KentOS.Kalem.Web/KentOS.Kalem.Web.csproj

COPY KentOS.Kalem.Application/ KentOS.Kalem.Application/
COPY KentOS.Kalem.Web/         KentOS.Kalem.Web/

# Derlenmiş SPA'yı kaynak ağacının `wwwroot`una bindir. Depoda izlenen
# varlıklar (amblem, PWA ikonları) yerinde kalır; üzerine `index.html` ve
# `uygulama/` paketleri gelir.
COPY --from=onyuz /kaynak/KentOS.Kalem.Web/wwwroot/ KentOS.Kalem.Web/wwwroot/

# SkipFrontend: ön yüz zaten derlendi. Bu bayrak olmadan MSBuild hedefi
# imajda olmayan `npm`i arar ve derleme anlaşılır bir hata bile vermeden
# durur.
RUN dotnet publish KentOS.Kalem.Web/KentOS.Kalem.Web.csproj \
      -c Release -o /yayin \
      -p:SkipFrontend=true \
      --no-restore


# ── 3. Çalışma ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS calisma
WORKDIR /uygulama

# ⚠ SAAT DİLİMİ KRİTİK.
#
# Veritabanındaki bütün damgalar `timestamp without time zone` ve uygulama
# baştan sona `DateTime.Now` (YEREL saat) kullanıyor. Kapsayıcı UTC'de
# çalışırsa Türkiye'de bütün etkinlikler 3 saat kayar — ve bu, sunucu
# hatası olarak değil "ajanda yanlış" olarak görünür.
#
# `TZ` tek başına yetmez: temel imajda tzdata yok, tanımsız bir bölge sessizce
# UTC'ye düşer.
RUN apt-get update \
 && apt-get install -y --no-install-recommends tzdata curl \
 && rm -rf /var/lib/apt/lists/*
ENV TZ=Europe/Istanbul

# Türkçe büyük/küçük harf (İ/ı) ICU ister. Değişmezliği (invariant globalization)
# AÇMAYIN: `ToUpper(tr-TR)` bozulur ve kurum adı çıktılarda yanlış basılır.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Kapsayıcıda varsayılan port 8080; Dokploy ters vekili buraya bağlanır.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Yüklenen dosyaların dizini — KALICI OLMALI.
# Bu klasör bir birime (volume) bağlanmazsa her dağıtımda etkinlik
# fotoğrafları, talep ekleri ve özgeçmişler kaybolur. Nesne deposu
# (`Storage__Provider=S3`) kullanılıyorsa bu uyarı geçersiz.
VOLUME ["/uygulama/wwwroot/uploads"]

# Kök olmayan kullanıcı KOPYADAN ÖNCE oluşturulur.
#
# Önce `COPY` sonra `chown -R` yapmak, 131 MB'lık uygulama katmanının
# TAMAMINI ikinci bir katman olarak yeniden yazıyordu — imaj bu yüzden iki
# kat büyüktü. `COPY --chown` sahipliği kopyalarken veriyor.
RUN useradd --create-home --shell /usr/sbin/nologin uygulama \
 && mkdir -p /uygulama/wwwroot/uploads \
 && chown uygulama:uygulama /uygulama /uygulama/wwwroot /uygulama/wwwroot/uploads

COPY --from=sunucu --chown=uygulama:uygulama /yayin ./
USER uygulama

# Sağlık denetimi: kurum ucu anonim ve veritabanına dokunuyor, yani
# "süreç ayakta" değil "gerçekten hizmet veriyor" ölçüyor.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/api/v2/institution || exit 1

ENTRYPOINT ["dotnet", "KentOS.Kalem.Web.dll"]
