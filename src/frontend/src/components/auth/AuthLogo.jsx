import { CgListTree } from "react-icons/cg";

export default function AuthLogo() {
  return (
    <div className="flex items-center gap-2.5">
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-600 text-white shadow-sm">
        <CgListTree className="text-lg" />
      </div>

      <div className="leading-none">
        <h1 className="text-xl font-bold text-slate-900">TechMap</h1>
        <p className="mt-1 text-[12px] font-semibold tracking-[0.14em] text-slate-500">
          ENGINEER YOUR FUTURE
        </p>
      </div>
    </div>
  );
}