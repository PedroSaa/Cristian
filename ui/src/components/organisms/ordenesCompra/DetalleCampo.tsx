export default function DetalleCampo({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-[11px] font-semibold uppercase tracking-wide text-text-base/45">{label}</span>
      <span className="text-sm text-text-base">{value || '—'}</span>
    </div>
  );
}
