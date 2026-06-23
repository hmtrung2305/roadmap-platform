import { useNavigate } from "react-router-dom";
import AuthLogo from "../features/auth/components/AuthLogo";
import { useAuthStore } from "../stores/useAuthStore";
import { getDefaultAuthenticatedRoute } from "../utils/navigationUtils";

export default function NotFoundPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const homeRoute = user ? getDefaultAuthenticatedRoute(user) : "/";

  return (
    <main className="grid min-h-dvh place-items-center bg-[#F7F1E8] px-6 py-10 text-[#18332D]">
      <section className="w-full max-w-md rounded-lg border border-[#B9D8CC] bg-white/95 p-6 text-center shadow-[0_18px_44px_rgba(31,111,95,0.08)]">
        <div className="mb-6 flex justify-center">
          <AuthLogo compact showTagline={false} />
        </div>

        <p className="text-xs font-extrabold uppercase tracking-[0.22em] text-[#1F6F5F]">
          Page not found
        </p>

        <h1 className="mt-3 text-2xl font-extrabold text-[#18332D]">
          We could not find that page.
        </h1>

        <p className="mt-3 text-sm font-semibold leading-6 text-slate-500">
          The page may have moved, been removed, or is not available from this account.
        </p>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-center">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#1F6F5F] transition hover:bg-[#F7F1E8]"
          >
            Go back
          </button>

          <button
            type="button"
            onClick={() => navigate(homeRoute, { replace: true })}
            className="rounded-lg bg-[#2FA084] px-4 py-2.5 text-sm font-extrabold text-white transition hover:bg-[#1F6F5F]"
          >
            Go home
          </button>
        </div>
      </section>
    </main>
  );
}
