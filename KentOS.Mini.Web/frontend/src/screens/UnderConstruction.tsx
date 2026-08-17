import { Hourglass } from 'lucide-react';
import { EmptyState } from '../components/EmptyState';

/** Henüz yazılmamış ekranlar için geçici yer tutucu. */
export default function UnderConstruction({ ad }: { ad: string }) {
  return (
    <EmptyState
      ikon={Hourglass}
      baslik={`${ad} hazırlanıyor`}
      aciklama="Bu ekran sıradaki aşamada tamamlanacak."
    />
  );
}
