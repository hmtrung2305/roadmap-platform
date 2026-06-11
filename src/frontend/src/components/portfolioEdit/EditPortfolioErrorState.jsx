export default function EditPortfolioErrorState({ message }) {
  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8]/60 px-4 py-8 sm:px-6">
      <section className="mx-auto max-w-3xl rounded-lg border border-[#B9D8CC] bg-white p-8 text-center shadow-[0_18px_45px_rgba(31,111,95,0.08)]">
        <p className="text-xl font-bold text-[#18332D]">Portfolio editor unavailable</p>
        <p className="mt-2 text-sm font-semibold text-red-600">{message}</p>
      </section>
    </main>
  );
}
