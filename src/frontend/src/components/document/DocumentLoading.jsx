export default function DocumentLoading() {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <div className="h-8 w-2/3 animate-pulse rounded bg-slate-200" />
      <div className="mt-6 space-y-3">
        <div className="h-4 animate-pulse rounded bg-slate-200" />
        <div className="h-4 w-11/12 animate-pulse rounded bg-slate-200" />
        <div className="h-4 w-10/12 animate-pulse rounded bg-slate-200" />
      </div>
      <div className="mt-8 h-48 animate-pulse rounded-xl bg-slate-200" />
    </div>
  );
}