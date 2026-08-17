#!/usr/bin/env bash
# Yerel geliştirme ve test veritabanlarını kurar.
#
# Postgres, /Users/cihad/Projects/database içindeki `postgis_db` konteynerinde
# çalışır ve BAŞKA PROJELERLE PAYLAŞILIR (kentos, turbopos, turbohesap).
# Bu betik yalnızca EKLER — var olan hiçbir veritabanına dokunmaz.
#
# Kullanım:  ./veritabani-kur.sh
set -euo pipefail

KONTEYNER="${WORKCOLLAB_PG_KONTEYNER:-postgis_db}"
ROL="workcollab"
PAROLA="workcollab"

if ! docker ps --format '{{.Names}}' | grep -qx "$KONTEYNER"; then
  echo "HATA: '$KONTEYNER' konteyneri çalışmıyor." >&2
  echo "      cd ~/Projects/database && docker compose up -d" >&2
  exit 1
fi

calistir() { docker exec -i "$KONTEYNER" psql -U postgres -v ON_ERROR_STOP=1 -c "$1"; }
var_mi_db() {
  docker exec -i "$KONTEYNER" psql -U postgres -tAc \
    "SELECT 1 FROM pg_database WHERE datname='$1'" | grep -q 1
}

# --- rol ---
if docker exec -i "$KONTEYNER" psql -U postgres -tAc \
     "SELECT 1 FROM pg_roles WHERE rolname='$ROL'" | grep -q 1; then
  echo "· rol '$ROL' zaten var"
else
  calistir "CREATE ROLE $ROL LOGIN PASSWORD '$PAROLA';"
  echo "+ rol '$ROL' oluşturuldu"
fi

# --- veritabanları ---
#   workcollab                → geliştirme (dotnet run)
#   workcollab_test           → PostgresTestFixture
#   workcollab_seri_test      → SunucuTestOrtami (gizli etkinlik + tekrar serisi)
#   workcollab_migrasyon_test → MigrasyonZinciriTests
#
# NOT: Rolde CREATEDB yetkisi YOK. Testler veritabanını düşürmek yerine
# `public` şemasını sıfırlıyor (bkz. SemayiSifirla). Yetki yükseltmemek
# bilinçli: paylaşılan bir konteynerde test rolünün veritabanı yaratabilmesi
# gereksiz bir risk.
for db in workcollab workcollab_test workcollab_seri_test workcollab_migrasyon_test; do
  if var_mi_db "$db"; then
    echo "· veritabanı '$db' zaten var"
  else
    calistir "CREATE DATABASE $db OWNER $ROL;"
    echo "+ veritabanı '$db' oluşturuldu"
  fi
done

echo
echo "Hazır. Şema ve tohum verisi için:"
echo "  cd KentOS.Mini.Web && ASPNETCORE_ENVIRONMENT=Development dotnet run"
