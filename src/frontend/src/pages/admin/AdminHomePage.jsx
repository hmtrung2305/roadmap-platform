import {
  Activity,
  ShieldCheck,
  SlidersHorizontal,
  UsersRound,
} from "lucide-react";
import { Link } from "react-router-dom";
import { PERMISSIONS } from "../../constants/permissions";
import { useAuthStore } from "../../stores/useAuthStore";
import { hasAllPermissions, hasAnyPermission } from "../../utils/authorizationUtils";

const adminAreas = [
  {
    title: "Users",
    icon: UsersRound,
    href: "/admin/users",
    requiredAllPermissions: [
      PERMISSIONS.USER_VIEW_ANY,
      PERMISSIONS.USER_ROLE_VIEW_ANY,
    ],
  },
  {
    title: "Roles & Permissions",
    icon: ShieldCheck,
    href: "/admin/roles",
    requiredPermissions: [
      PERMISSIONS.ROLE_VIEW_ANY,
      PERMISSIONS.PERMISSION_VIEW_ANY,
      PERMISSIONS.ROLE_PERMISSION_VIEW_ANY,
    ],
  },
  { title: "Skills", icon: SlidersHorizontal },
  {
    title: "Market Pulse",
    icon: Activity,
    href: "/admin/market-pulse",
    requiredPermissions: [PERMISSIONS.MARKET_PULSE_MANAGE_ANY],
  },
];

export default function AdminHomePage() {
  const user = useAuthStore((state) => state.user);
  const visibleAdminAreas = adminAreas.filter((area) => (
    hasAnyPermission(user, area.requiredPermissions || [])
    && hasAllPermissions(user, area.requiredAllPermissions || [])
  ));

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
        {visibleAdminAreas.map((area) => {
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
