import {
  KeyRound,
  Activity,
  ShieldCheck,
  SlidersHorizontal,
  UsersRound,
} from "lucide-react";
import { Link } from "react-router-dom";

const adminAreas = [
  { title: "Users", icon: UsersRound },
  { title: "Roles", icon: ShieldCheck },
  { title: "Permissions", icon: KeyRound },
  { title: "Skills", icon: SlidersHorizontal },
  { title: "Market Pulse", icon: Activity, href: "/admin/market-pulse" },
];

export default function AdminHomePage() {
  return (
    <div className="mx-auto max-w-6xl space-y-6 px-6 py-8">
      <section className="rounded-xl border border-[#B9D8CC] bg-white p-6 shadow-sm">
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          Platform
        </p>
        <h1 className="mt-2 text-3xl font-black tracking-[-0.035em] text-[#18332D]">
          Admin Console
        </h1>
        <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
          Platform governance workspace.
        </p>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {adminAreas.map((area) => {
          const Icon = area.icon;

          return (
            <Link
              key={area.title}
              to={area.href || "/admin"}
              className="block rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm transition hover:border-[#6FCF97]"
            >
              <div className="grid h-11 w-11 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <Icon size={21} />
              </div>
              <h2 className="mt-4 text-base font-extrabold text-[#18332D]">
                {area.title}
              </h2>
            </Link>
          );
        })}
      </section>
    </div>
  );
}
