import { NavLink } from "react-router-dom";
import { Flame, Settings, Shield, UserRound } from "lucide-react";
import { FaFireAlt, FaUser } from "react-icons/fa";

const items = [
  {
    label: "Account",
    to: "/settings/account",
    icon: FaUser,
  },
  {
    label: "Privacy",
    to: "/settings/privacy",
    icon: Shield,
  },
  {
    label: "Points",
    to: "/settings/points",
    icon: FaFireAlt,
  },
  {
    label: "Profile",
    to: "/settings/profile",
    icon: Settings,
  },
];

export default function SettingsSidebar() {
  return (
    <aside className="sticky top-24 hidden h-fit w-64 shrink-0 rounded-3xl border border-slate-200 bg-white p-3 shadow-sm lg:block">
      <div className="px-3 py-3">
        <h1 className="text-xl font-bold tracking-tight text-slate-900">
          Settings
        </h1>

        <p className="mt-1 text-sm text-slate-500">
          Manage your account and profile.
        </p>
      </div>

      <nav className="mt-3 space-y-1">
        {items.map((item) => {
          const Icon = item.icon;

          return (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-2xl px-3 py-3 text-sm font-semibold transition ${
                  isActive
                    ? "bg-indigo-50 text-indigo-700"
                    : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                }`
              }
            >
              <Icon size={18} />
              {item.label}
            </NavLink>
          );
        })}
      </nav>

      <div className="mt-4 rounded-2xl border border-teal-100 bg-teal-50 px-3 py-3 text-xs leading-5 text-teal-700">
        Keep at least one login method connected to avoid losing access.
      </div>
    </aside>
  );
}
