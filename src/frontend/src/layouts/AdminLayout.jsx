import { useEffect, useMemo, useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  LibraryBig,
  Shield,
} from "lucide-react";

import AuthLogo from "../components/auth/AuthLogo";
import StreakAnimation from "../components/streak/StreakAnimation";
import { getMyProfileApi } from "../api/profileApi";
import { useAuthStore } from "../stores/useAuthStore";

const adminNavGroups = [
  {
    label: "Content",
    items: [
      {
        label: "Learning Modules",
        path: "/admin/learning-modules",
        icon: LibraryBig,
        match: (pathname) => pathname.startsWith("/admin/learning-modules"),
      },
    ],
  },
];

function getAdminPageTitle(pathname) {
  if (pathname === "/admin/learning-modules/create") {
    return "Create Module";
  }

  if (pathname.endsWith("/edit")) {
    return "Module Editor";
  }

  if (pathname.endsWith("/preview")) {
    return "Module Preview";
  }

  if (pathname.startsWith("/admin/learning-modules")) {
    return "Learning Modules";
  }

  return "Admin";
}

function getDisplayName(user, profile) {
  return (
    profile?.fullName
    || profile?.displayName
    || profile?.name
    || user?.fullName
    || user?.displayName
    || user?.name
    || user?.username
    || "Counselor"
  );
}

function getEmail(user, profile) {
  return (
    profile?.email
    || user?.email
    || "No email available"
  );
}

export default function AdminLayout() {
  const navigate = useNavigate();
  const location = useLocation();

  const user = useAuthStore((state) => state.user);

  const [profile, setProfile] = useState(null);

  useEffect(() => {
    if (!user) {
      return;
    }

    async function fetchProfile() {
      try {
        const data = await getMyProfileApi();
        setProfile(data);
      } catch (error) {
        console.error("Failed to load profile:", error);
      }
    }

    fetchProfile();
  }, [user]);

  const pageTitle = useMemo(
    () => getAdminPageTitle(location.pathname),
    [location.pathname],
  );

  const displayName = getDisplayName(user, profile);
  const email = getEmail(user, profile);

  return (
    <div className="min-h-screen bg-[#F7F1E8] text-[#18332D]">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-60 border-r border-[#B9D8CC] bg-white/95 shadow-sm backdrop-blur-xl lg:flex lg:flex-col">
        <div className="border-b border-[#B9D8CC] px-4 py-4">
          <button
            type="button"
            onClick={() => navigate("/admin/learning-modules")}
            className="block"
          >
            <AuthLogo compact showTagline={false} />
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-3 py-5">
          {adminNavGroups.flatMap((group) => group.items).map((item) => {
            const Icon = item.icon;
            const isActive = item.match(location.pathname);

            return (
              <button
                key={item.label}
                type="button"
                onClick={() => navigate(item.path)}
                className={`flex w-full items-center gap-3 rounded-lg border px-3 py-2.5 text-left text-sm font-extrabold transition ${
                  isActive
                    ? "border-[#6FCF97] bg-[#6FCF97]/24 text-[#1F6F5F]"
                    : "border-transparent text-slate-600 hover:border-[#B9D8CC] hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
                }`}
              >
                <Icon size={17} />
                {item.label}
              </button>
            );
          })}
        </nav>

        <div className="border-t border-[#B9D8CC] p-3">
          <div className="flex min-w-0 items-center gap-3 rounded-lg px-2 py-1.5">
            <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#2FA084] text-white">
              <Shield size={18} />
            </div>

            <div className="min-w-0">
              <div className="truncate text-sm font-extrabold text-[#18332D]">
                {displayName}
              </div>
              <div className="truncate text-xs font-semibold text-slate-500">
                {email}
              </div>
            </div>
          </div>
        </div>
      </aside>

      <div className="lg:pl-60">
        <header className="sticky top-0 z-30 border-b border-[#B9D8CC] bg-white/90 backdrop-blur-xl">
          <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
            <div className="min-w-0">
              <div className="flex items-center gap-3 lg:hidden">
                <button
                  type="button"
                  onClick={() => navigate("/admin/learning-modules")}
                  className="shrink-0"
                >
                  <AuthLogo compact showTagline={false} />
                </button>

                <div>
                  <p className="text-[11px] font-extrabold uppercase tracking-[0.16em] text-[#1F6F5F]">
                    Admin
                  </p>
                  <h1 className="truncate text-sm font-extrabold text-[#18332D]">
                    {pageTitle}
                  </h1>
                </div>
              </div>

              <div className="hidden lg:block">
                <p className="text-[11px] font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Admin
                </p>
                <h1 className="truncate text-lg font-extrabold text-[#18332D]">
                  {pageTitle}
                </h1>
              </div>
            </div>

            <div className="hidden items-center gap-2 md:flex lg:hidden">
              {adminNavGroups.flatMap((group) => group.items).map((item) => {
                const Icon = item.icon;
                const isActive = item.match(location.pathname);

                return (
                  <button
                    key={item.label}
                    type="button"
                    onClick={() => navigate(item.path)}
                    className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-extrabold transition ${
                      isActive
                        ? "border-[#6FCF97] bg-[#6FCF97]/24 text-[#1F6F5F]"
                        : "border-transparent text-slate-600 hover:border-[#B9D8CC] hover:bg-white hover:text-[#1F6F5F]"
                    }`}
                  >
                    <Icon size={15} />
                    {item.label}
                  </button>
                );
              })}
            </div>

            <div className="hidden min-w-0 text-right sm:block lg:hidden">
              <div className="truncate text-xs font-extrabold text-[#18332D]">
                {displayName}
              </div>
              <div className="truncate text-[11px] font-semibold text-slate-500">
                {email}
              </div>
            </div>
          </div>

          <div className="flex gap-2 overflow-x-auto border-t border-[#B9D8CC]/70 px-4 py-2 md:hidden">
            {adminNavGroups.flatMap((group) => group.items).map((item) => {
              const Icon = item.icon;
              const isActive = item.match(location.pathname);

              return (
                <button
                  key={item.label}
                  type="button"
                  onClick={() => navigate(item.path)}
                  className={`inline-flex shrink-0 items-center gap-2 rounded-lg border px-3 py-2 text-xs font-extrabold transition ${
                    isActive
                      ? "border-[#6FCF97] bg-[#6FCF97]/24 text-[#1F6F5F]"
                      : "border-[#B9D8CC] bg-white text-slate-600"
                  }`}
                >
                  <Icon size={15} />
                  {item.label}
                </button>
              );
            })}
          </div>
        </header>

        <main className="min-h-[calc(100vh-4rem)]">
          <Outlet />
        </main>
      </div>

      <StreakAnimation />
    </div>
  );
}
