import { ArrowLeft } from "lucide-react";
import { FaGithub } from "react-icons/fa";

export default function GitHubNotLinkedState({ error, onConnectGitHub, onBack }) {
  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8] px-4 py-8 sm:px-6">
      <section className="mx-auto max-w-4xl overflow-hidden rounded-2xl border border-[#B9D8CC] bg-white p-8 text-center shadow-[0_18px_50px_rgba(31,111,95,0.10)]">
        <div className="mx-auto flex h-16 w-16 items-center justify-center  bg-[#2FA084] text-white border border-[#B9D8CC] shadow-sm shadow-lg">
          <FaGithub size={30} />
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
            className="inline-flex items-center justify-center gap-2 rounded-2xl bg-[#2FA084] px-5 py-3 text-sm font-black text-white shadow-sm transition hover:bg-[#1F6F5F]"
          >
            <FaGithub size={17} />
            Connect GitHub
          </button>

          <button
            type="button"
            onClick={onBack}
            className="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 py-3 text-sm font-bold text-slate-700 transition hover:bg-[#F7F1E8]"
          >
            <ArrowLeft size={17} />
            Back to Portfolio
          </button>
        </div>
      </section>
    </main>
  );
}
