import { useNavigate } from "react-router-dom";

export default function Footer() {
  const navigate = useNavigate();

  return (
    <footer className="border-t border-slate-200 bg-white">
      <div className="mx-auto flex max-w-7xl flex-col gap-3 py-5 text-sm text-slate-500 md:flex-row md:items-center md:justify-between">
        <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <button
            type="button"
            onClick={() => navigate("/dashboard")}
            className="font-bold text-blue-700 hover:text-blue-800 text-2xl"
          >
            TechMap
          </button>

          <span className="text-slate-300">·</span>

          <span>Learning roadmap and developer portfolio platform.</span>
        </div>

        <nav className="flex flex-wrap items-center gap-x-4 gap-y-2">
          <button
            type="button"
            onClick={() => navigate("/dashboard")}
            className="hover:text-blue-700"
          >
            Dashboard
          </button>

          <button
            type="button"
            onClick={() => navigate("/portfolio")}
            className="hover:text-blue-700"
          >
            Portfolio
          </button>

          <button
            type="button"
            onClick={() => navigate("/resources")}
            className="hover:text-blue-700"
          >
            Resources
          </button>

          <button
            type="button"
            className="hover:text-blue-700"
          >
            Privacy
          </button>

          <button
            type="button"
            className="hover:text-blue-700"
          >
            Terms
          </button>
        </nav>
      </div>

      <div className="border-t border-slate-100 py-3 text-center text-xs text-slate-400">
        © 2026 TechMap. All rights reserved.
      </div>
    </footer>
  );
}