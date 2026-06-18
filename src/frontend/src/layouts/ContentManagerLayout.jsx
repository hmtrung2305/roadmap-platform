import { useEffect, useMemo, useRef, useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  ChevronUp,
  LibraryBig,
  LogOut,
  Settings,
  Shield,
} from "lucide-react";

import AuthLogo from "../components/auth/AuthLogo";
import StreakAnimation from "../components/streak/StreakAnimation";
import { useAuthStore } from "../stores/useAuthStore";
import { useAccountProfileStore } from "../stores/useAccountProfileStore";

const contentManagerNavGroups = [
  {
    label: "Content",
    items: [
      {
        label: "Learning Modules",
        path: "/content/learning-modules",
        icon: LibraryBig,
        match: (pathname) => pathname.startsWith("/content/learning-modules"),
      },
    ],
  },
];

function getContentManagerPageTitle(pathname) {
  if (pathname === "/content/learning-modules/create") {
    return "Create Module";
  }

  if (pathname.endsWith("/edit")) {
    return "Module Editor";
  }

  if (pathname.endsWith("/preview")) {
    return "Module Preview";
  }

  if (pathname.startsWith("/content/learning-modules")) {
    return "Learning Modules";
  }

  if (pathname === "/content/settings") {
    return "Settings";
  }

  return "Content Manager";
}

function getDisplayName(user, accountProfile) {
  return (
    accountProfile?.displayName
    || user?.fullName
    || user?.displayName
    || user?.name
    || user?.username
    || "Content Manager"
  );
}

function getEmail(user) {
  return user?.email || "No email available";
}

export default function ContentManagerLayout() {
  const navigate = useNavigate();
  const location = useLocation();

  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const accountProfile = useAccountProfileStore(
    (state) => state.accountProfile,
  );
  const loadAccountProfile = useAccountProfileStore(
    (state) => state.loadAccountProfile,
  );

  const userMenuRef = useRef(null);

  const [avatarFailed, setAvatarFailed] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);

  useEffect(() => {
    if (!user) {
      return;
    }

    loadAccountProfile().catch((error) => {
      console.error("Failed to load account profile:", error);
    });
  }, [user, loadAccountProfile]);

  useEffect(() => {
    setAvatarFailed(false);
  }, [accountProfile?.avatarUrl]);

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
    () => getContentManagerPageTitle(location.pathname),
    [location.pathname],
  );

  const displayName = getDisplayName(user, accountProfile);
  const email = getEmail(user);
  const avatarUrl = accountProfile?.avatarUrl?.trim();
  const showAvatar = Boolean(avatarUrl && !avatarFailed);

  const goToSettings = () => {
    setIsUserMenuOpen(false);
    navigate("/content/settings");
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
            onClick={() => navigate("/content/learning-modules")}
            className="block"
          >
            <AuthLogo compact showTagline={false} />
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-3 py-5">
          {contentManagerNavGroups.flatMap((group) => group.items).map((item) => {
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
              location.pathname === "/content/settings" || isUserMenuOpen
                ? "border-[#6FCF97] bg-[#6FCF97]/18"
                : "border-transparent hover:border-[#B9D8CC] hover:bg-[#F7F1E8]"
            }`}
          >
            <div className="grid h-10 w-10 shrink-0 place-items-center overflow-hidden rounded-lg bg-[#2FA084] text-white">
              {showAvatar ? (
                <img
                  src={avatarUrl}
                  alt={`${displayName} avatar`}
                  onError={() => setAvatarFailed(true)}
                  className="h-full w-full object-cover"
                />
              ) : (
                <Shield size={18} />
              )}
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
                  onClick={() => navigate("/content/learning-modules")}
                  className="shrink-0"
                >
                  <AuthLogo compact showTagline={false} />
                </button>

                <div>
                  <p className="text-[11px] font-extrabold uppercase tracking-[0.16em] text-[#1F6F5F]">
                    Content Manager
                  </p>
                  <h1 className="truncate text-sm font-extrabold text-[#18332D]">
                    {pageTitle}
                  </h1>
                </div>
              </div>

              <div className="hidden lg:block">
                <p className="text-[11px] font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Content Manager
                </p>
                <h1 className="truncate text-lg font-extrabold text-[#18332D]">
                  {pageTitle}
                </h1>
              </div>
            </div>

            <div className="hidden items-center gap-2 md:flex lg:hidden">
              {contentManagerNavGroups.flatMap((group) => group.items).map((item) => {
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

            <div className="relative hidden sm:block lg:hidden">
              <button
                type="button"
                onClick={() => setIsUserMenuOpen((current) => !current)}
                className="min-w-0 rounded-lg border border-transparent px-2 py-1 text-right transition hover:border-[#B9D8CC] hover:bg-[#F7F1E8]"
              >
                <div className="truncate text-xs font-extrabold text-[#18332D]">
                  {displayName}
                </div>
                <div className="truncate text-[11px] font-semibold text-slate-500">
                  {email}
                </div>
              </button>

              {isUserMenuOpen && (
                <div className="absolute right-0 top-12 z-50 w-44 overflow-hidden rounded-lg border border-[#B9D8CC] bg-white py-1 text-left shadow-xl">
                  <button
                    type="button"
                    onClick={goToSettings}
                    className="flex w-full items-center gap-2 px-3 py-2.5 text-xs font-extrabold text-[#18332D] hover:bg-[#F7F1E8]"
                  >
                    <Settings size={15} />
                    Settings
                  </button>
                  <div className="my-1 border-t border-[#B9D8CC]/70" />
                  <button
                    type="button"
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2 px-3 py-2.5 text-xs font-extrabold text-rose-700 hover:bg-rose-50"
                  >
                    <LogOut size={15} />
                    Sign out
                  </button>
                </div>
              )}
            </div>
          </div>

          <div className="flex gap-2 overflow-x-auto border-t border-[#B9D8CC]/70 px-4 py-2 md:hidden">
            {contentManagerNavGroups.flatMap((group) => group.items).map((item) => {
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
