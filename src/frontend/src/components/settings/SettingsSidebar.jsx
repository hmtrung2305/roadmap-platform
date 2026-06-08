import { NavLink } from "react-router-dom";
import { Settings, Shield } from "lucide-react";
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
    <aside className="sticky top-24 hidden h-fit w-64 shrink-0 rounded-lg border border-[#B9D8CC] bg-white p-3 shadow-[0_14px_34px_rgba(31,111,95,0.08)] lg:block">
      <div className="px-3 py-3">
        <h1 className="text-xl font-extrabold tracking-tight text-[#18332D]">
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
                `flex items-center gap-3 rounded-lg px-3 py-3 text-sm font-bold transition ${
                  isActive
                    ? "bg-[#6FCF97]/20 text-[#1F6F5F] shadow-sm shadow-[#6FCF97]/20"
                    : "text-slate-600 hover:bg-[#6FCF97]/10 hover:text-[#1F6F5F]"
                }`
              }
            >
              <Icon size={18} className="shrink-0" />
              {item.label}
            </NavLink>
          );
        })}
      </nav>

      <div className="mt-4 rounded-lg border border-[#B9D8CC] bg-[#6FCF97]/12 px-3 py-3 text-xs leading-5 text-[#1F6F5F]">
        Keep at least one login method connected to avoid losing access.
      </div>
    </aside>
  );
}
