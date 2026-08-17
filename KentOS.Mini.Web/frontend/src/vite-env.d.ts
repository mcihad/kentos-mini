/// <reference types="vite/client" />

/**
 * Yardım metinleri `?raw` ile düz metin olarak içe aktarılıyor
 * (bkz. `src/yardim/katalog.ts`). Vite bunu derleme anında dosyanın içeriğiyle
 * değiştirir; ayrı bir istek yapılmaz, çevrimdışıyken de açılır.
 */
declare module '*.md?raw' {
  const icerik: string;
  export default icerik;
}
