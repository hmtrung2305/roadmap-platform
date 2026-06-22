/* eslint-disable react-hooks/exhaustive-deps */
import { useEffect, useMemo, useRef, useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  ChevronUp,
  LayoutDashboard,
  LogOut,
  Activity,
  Settings,
  Shield,
} from "lucide-react";

import AuthLogo from "../components/auth/AuthLogo";
import StreakAnimation from "../components/streak/StreakAnimation";
import { useAuthStore } from "../stores/useAuthStore";
import { useProfileStore } from "../stores/useProfileStore";

const adminNavItems = [
  {
    label: "Overview",
    path: "/admin",
    icon: LayoutDashboard,
    match: (pathname) => pathname === "/admin",
  },
  {
    label: "Market Pulse",
    path: "/admin/market-pulse",
    icon: Activity,
    match: (pathname) => pathname === "/admin/market-pulse",
  },
  {
    label: "Settings",
    path: "/admin/settings",
    icon: Settings,
    match: (pathname) => pathname === "/admin/settings",
  },
];

function getAdminPageTitle(pathname) {
  if (pathname === "/admin") {
    return "Overview";
  }

  if (pathname === "/admin/settings") {
    return "Settings";
  }

  if (pathname === "/admin/market-pulse") {
    return "Market Pulse";
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
    || "Admin"
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
  const logout = useAuthStore((state) => state.logout);
  const profile = useProfileStore((state) => state.profile);
  const loadProfile = useProfileStore((state) => state.loadProfile);

  const userMenuRef = useRef(null);

  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);

  useEffect(() => {
    if (!user) return;

    loadProfile().catch((error) => {
      console.error("Failed to load profile:", error);
    });
  }, [loadProfile, user?.userId, user?.id, user?.email, user?.username]);

  useEffect(() => {
    function handleClickOutside(event) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target)) {
        setIsUserMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const pageTitle = useMemo(
    () => getAdminPageTitle(location.pathname),
    [location.pathname],
  );

  const displayName = getDisplayName(user, profile);
  const email = getEmail(user, profile);

  const goToSettings = () => {
    setIsUserMenuOpen(false);
    navigate("/admin/settings");
  };

  const handleLogout = async () => {
    setIsUserMenuOpen(false);
    await logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-[#F7F1E8] text-[#18332D]">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-60 border-r border-[#B9D8CC] bg-white/95 shadow-sm backdrop-blur-xl lg:flex lg:flex-col">
        <div className="border-b border-[#B9D8CC] px-4 py-4">
          <button
            type="button"
            onClick={() => navigate("/admin")}
            className="block"
          >
            <AuthLogo compact showTagline={false} />
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-3 py-5">
          {adminNavItems.map((item) => {
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

        <div ref={userMenuRef} className="relative border-t border-[#B9D8CC] p-3">
          {isUserMenuOpen && (
            <div className="absolute bottom-[72px] left-3 right-3 z-50 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 shadow-xl">
              <button
                type="button"
                onClick={goToSettings}
                className="flex w-full items-center gap-2 px-3 py-2.5 text-left text-xs font-extrabold text-[#18332D] transition hover:bg-[#F7F1E8]"
              >
                <Settings size={15} />
                Settings
              </button>

              <div className="my-1 border-t border-[#B9D8CC]/70" />

              <button
                type="button"
                onClick={handleLogout}
                className="flex w-full items-center gap-2 px-3 py-2.5 text-left text-xs font-extrabold text-rose-700 transition hover:bg-rose-50"
              >
                <LogOut size={15} />
                Sign out
              </button>
            </div>
          )}

          <button
            type="button"
            onClick={() => setIsUserMenuOpen((current) => !current)}
            className={`flex w-full min-w-0 items-center gap-3 rounded-lg border px-2 py-1.5 text-left transition ${
              location.pathname === "/admin/settings" || isUserMenuOpen
                ? "border-[#6FCF97] bg-[#6FCF97]/18"
                : "border-transparent hover:border-[#B9D8CC] hover:bg-[#F7F1E8]"
            }`}
          >
            <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#2FA084] text-white">
              <Shield size={18} />
            </div>

            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-extrabold text-[#18332D]">
                {displayName}
              </div>
              <div className="truncate text-xs font-semibold text-slate-500">
                {email}
              </div>
            </div>

            <ChevronUp
              size={15}
              className={`shrink-0 text-slate-500 transition ${isUserMenuOpen ? "rotate-180" : ""}`}
            />
          </button>
        </div>
      </aside>

      <div className="lg:pl-60">
        <header className="sticky top-0 z-30 border-b border-[#B9D8CC] bg-white/90 backdrop-blur-xl">
          <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
            <div className="min-w-0">
              <div className="flex items-center gap-3 lg:hidden">
                <button
                  type="button"
                  onClick={() => navigate("/admin")}
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
