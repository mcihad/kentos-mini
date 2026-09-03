import { Compass } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';

/** Bilinmeyen rota. */
export default function NotFound() {
  const gezin = useNavigate();
  return (
    <EmptyState
      ikon={Compass}
      baslik="Sayfa bulunamadı"
      aciklama="Aradığınız sayfa taşınmış veya hiç var olmamış olabilir."
      eylem={<Button onClick={() => gezin('/')}>Ana sayfaya dön</Button>}
    />
  );
}
