export default function RoadmapFullScreen({ children }) {
  return (
    <div className="min-h-[calc(100vh-64px)] bg-[#F7F1E8] text-[#18332D]">
      <main className="mx-auto flex min-h-[calc(100vh-64px)] max-w-7xl items-center justify-center px-6 py-8">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-8 text-center shadow-lg">
          <div className="mx-auto mb-4 h-7 w-7 animate-spin rounded-lg border border-[#B9D8CC] border-t-[#2FA084]" />
          <p className="text-sm font-extrabold tracking-tight text-slate-600">{children}</p>
        </div>
      </main>
    </div>
  );
}
