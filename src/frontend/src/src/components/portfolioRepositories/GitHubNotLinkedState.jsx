import { ArrowLeft } from "lucide-react";
import { FaGithub } from "react-icons/fa";

export default function GitHubNotLinkedState({ error, onConnectGitHub, onBack }) {
  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#f6f8f4] px-4 py-8 sm:px-6">
      <section className="mx-auto max-w-4xl overflow-hidden rounded-[2rem] border border-emerald-100 bg-white p-8 text-center shadow-sm">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-3xl bg-slate-950 text-white shadow-lg">
          <Github size={30} />
        </div>

        <h1 className="mt-5 text-3xl font-black tracking-tight text-slate-950">Connect GitHub first</h1>

        <p className="mx-auto mt-3 max-w-xl text-sm font-medium leading-7 text-slate-500">
          To display repositories on your portfolio, connect your GitHub account first. After that, sync repositories and choose which projects are public.
        </p>

        {error && (
          <div className="mx-auto mt-5 max-w-xl rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
            {error}
          </div>
        )}

        <div className="mt-6 flex flex-col justify-center gap-3 sm:flex-row">
          <button
            type="button"
            onClick={onConnectGitHub}
            className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-950 px-5 py-3 text-sm font-black text-white shadow-sm transition hover:bg-emerald-700"
          >
            <FaGithub size={17} />
            Connect GitHub
          </button>

          <button
            type="button"
            onClick={onBack}
            className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 py-3 text-sm font-bold text-slate-700 transition hover:bg-slate-50"
          >
            <ArrowLeft size={17} />
            Back to Portfolio
          </button>
        </div>
      </section>
    </main>
  );
}
