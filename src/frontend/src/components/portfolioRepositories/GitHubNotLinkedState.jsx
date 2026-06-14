export default function GitHubNotLinkedState({
  error,
  onConnectGitHub,
  onBack,
}) {
  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8] px-6 py-12">
      <section className="mx-auto max-w-4xl rounded-lg border border-slate-200 bg-white p-8 text-center shadow-sm">
        <h1 className="text-2xl font-bold text-slate-900">
          Connect GitHub first
        </h1>

        <p className="mx-auto mt-3 max-w-xl text-slate-500">
          To display repositories on your portfolio, connect your GitHub account
          first. After that, you can sync repositories and choose which projects
          are public.
        </p>

        {error && (
          <div className="mt-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}

        <div className="mt-6 flex justify-center gap-3">
          <button
            type="button"
            onClick={onConnectGitHub}
            className="rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-semibold text-white hover:bg-[#1F6F5F]"
          >
            Connect GitHub
          </button>

          <button
            type="button"
            onClick={onBack}
            className="rounded-lg border border-slate-200 bg-white px-5 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            Back to Portfolio
          </button>
        </div>
      </section>
    </main>
  );
}