import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ToastProvider, useToast } from '../src/components/Toast';

/**
 * BİLDİRİM ŞERİDİ TIKLANABİLİR.
 *
 * <p>
 * Aynı bildirim iki farklı yerde çıkıyor: uygulama kapalıyken işletim
 * sisteminin bildirimi (tıklayınca kayda gidiyor), açıkken uygulama içi
 * şerit (tıklayınca hiçbir şey olmuyordu). Kullanıcının şikâyeti tam olarak
 * bu ayrımdı — hangi pencerede olduğuna göre bildirimin davranışı
 * değişiyordu.
 * </p>
 */

function Tetikleyici({ eylem }: { eylem?: () => void }) {
  const { bildir } = useToast();
  return (
    <button
      type="button"
      onClick={() =>
        bildir('bilgi', 'Yeni etkinlik', 'Makam toplantısı', eylem && { eylem, eylemEtiketi: 'Aç' })
      }
    >
      bildir
    </button>
  );
}

function kur(eylem?: () => void) {
  return render(
    <ToastProvider>
      <Tetikleyici eylem={eylem} />
    </ToastProvider>,
  );
}

describe('bildirim şeridi', () => {
  it('eylemi olmayan şeritte tıklanacak bir şey yok', () => {
    kur();
    fireEvent.click(screen.getByText('bildir'));

    expect(screen.getByText('Yeni etkinlik')).toBeInTheDocument();
    expect(screen.queryByTitle('Aç')).not.toBeInTheDocument();
  });

  it('eylemi olan şeride tıklayınca hedefe gidilir ve şerit kapanır', () => {
    const eylem = vi.fn();
    kur(eylem);
    fireEvent.click(screen.getByText('bildir'));

    const dugme = screen.getByTitle('Aç');
    fireEvent.pointerDown(dugme, { clientX: 40, clientY: 40 });
    fireEvent.click(dugme, { clientX: 41, clientY: 40 });

    expect(eylem).toHaveBeenCalledTimes(1);
    expect(screen.queryByText('Yeni etkinlik')).not.toBeInTheDocument();
  });

  it('KAYDIRARAK KAPATMA gezinme sayılmaz', () => {
    const eylem = vi.fn();
    kur(eylem);
    fireEvent.click(screen.getByText('bildir'));

    const dugme = screen.getByTitle('Aç');
    // Şeridi ekrandan dışarı atma hareketi de `click` üretiyor: kullanıcı
    // bildirimi REDDETMEK için yapıyor, hedefe gitmek için değil.
    fireEvent.pointerDown(dugme, { clientX: 40, clientY: 40 });
    fireEvent.click(dugme, { clientX: 160, clientY: 44 });

    expect(eylem).not.toHaveBeenCalled();
  });

  it('eylem düğmesi ekran okuyucuya başlığıyla duyurulur', () => {
    kur(vi.fn());
    fireEvent.click(screen.getByText('bildir'));

    expect(screen.getByLabelText('Yeni etkinlik — Aç')).toBeInTheDocument();
  });
});
