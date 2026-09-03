import { cn } from './utils';

/** design.md §11 — yükleme iskeleti (1.2 sn nabız). */
export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      className={cn('animate-pulse rounded-sm bg-sunken', className)}
      style={{ animationDuration: '1.2s' }}
      aria-hidden
    />
  );
}

export function SkeletonRows({ adet = 5 }: { adet?: number }) {
  return (
    <div className="space-y-2" role="status" aria-label="Yükleniyor">
      {Array.from({ length: adet }).map((_, i) => (
        <Skeleton key={i} className="h-[46px] w-full" />
      ))}
    </div>
  );
}
