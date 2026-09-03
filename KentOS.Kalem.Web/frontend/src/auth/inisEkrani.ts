import type { Me } from './SessionProvider';

/**
 * GİRİŞTEN SONRA NEREYE İNİLECEK.
 *
 * <p>
 * Saha personeli günde onlarca kez giriş yapıyor ve her seferinde masaüstü
 * için tasarlanmış panelden geçip aradığını bulmak zorundaydı. İşaretliyse
 * doğrudan <code>/saha</code>'ya iniyor.
 * </p>
 *
 * <p>
 * <b>Derin bağlantı her şeyin önünde.</b> Kullanıcı bir görev bildirimine
 * dokunup giriş ekranına düştüyse gitmek istediği yer bellidir; onu saha ana
 * ekranına atmak, tıkladığı şeyi kaybettirmek olurdu. Saha varsayılanı ancak
 * gidilecek belirli bir yer YOKKEN devreye giriyor.
 * </p>
 *
 * <p>
 * <b>Bu bir kilit değil.</b> İzinleri varsa kullanıcı panele geçebilir;
 * burada belirlenen şey yalnızca varsayılan.
 * </p>
 */
export function inisYolu(me: Me | null, istenen?: string | null): string {
  if (istenen) return istenen;
  return me?.sahaPersoneli ? '/saha' : '/';
}
