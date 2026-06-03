import { CgListTree } from "react-icons/cg";

export default function AuthLogo({ compact = false }) {
  return (
    <div className="flex items-center gap-2">
      <div
        className={`flex items-center justify-center rounded-lg bg-blue-600 text-white shadow-sm ${
          compact ? "h-8 w-8" : "h-9 w-9"
        }`}
      >
        <CgListTree className={compact ? "text-base" : "text-lg"} />
      </div>

      <div className="leading-none">
        <h1
          className={`font-bold text-slate-900 ${
            compact ? "text-lg" : "text-xl"
          }`}
        >
          TechMap
        </h1>

        <p
          className={`mt-1 font-semibold tracking-[0.13em] text-slate-500 ${
            compact ? "text-[10px]" : "text-[11px]"
          }`}
        >
          ENGINEER YOUR FUTURE
        </p>
      </div>
    </div>
  );
}