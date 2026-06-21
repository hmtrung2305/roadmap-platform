import { useEffect, useMemo } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { FaFireAlt } from "react-icons/fa";

import AuthLogo from "../auth/AuthLogo";
import { useStreakStore } from "../../stores/useStreakStore";
import { useAuthStore } from "../../stores/useAuthStore";
import { useProfileStore } from "../../stores/useProfileStore";
import AvatarDropdown from "./AvatarDropdown";

export default function TopNavbar() {
  const navigate = useNavigate();
  const location = useLocation();
  const params = useParams();

  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const streak = useStreakStore((state) => state.streak);
  const profile = useProfileStore((state) => state.profile);
  const loadProfile = useProfileStore((state) => state.loadProfile);

  const currentStreak = streak?.currentStreak ?? 0;
  const isCompletedStreakToday = streak?.isCompletedStreakToday ?? false;

  const portfolioPath = useMemo(() => {
    if (user) return "/portfolio";
    if (location.pathname.startsWith("/portfolio/")) return location.pathname;
    if (location.pathname.startsWith("/portfolios/")) return location.pathname;
    if (params?.username) return `/portfolio/${params.username}`;
    return "/login";
  }, [user, location.pathname, params]);

  const navItems = [
    { label: "Roadmaps", path: user ? "/roadmaps" : "/login" },
    { label: "Learning", path: user ? "/learning-modules" : "/login" },
    { label: "Skill Gap", path: user ? "/skill-gap" : "/login" },
    { label: "Market Pulse", path: user ? "/market-pulse" : "/login" },
    { label: "E-Portfolio", path: portfolioPath },
  ];

  const userProfileKey = user?.userId ?? user?.id ?? user?.email ?? user?.username ?? null;

useEffect(() => {
  if (!userProfileKey) return;

  loadProfile().catch((error) => {
    console.error("Failed to load profile:", error);
  });
}, [loadProfile, userProfileKey]);

  const handleLogout = async () => {
    await logout();
    navigate("/login");
  };

  return (
    <header className="sticky top-0 z-50 border-transparent shadow-sm bg-white/90 backdrop-blur-xl">
      <div className="mx-auto flex h-15 max-w-7xl items-center justify-between gap-5 px-4 py-1 sm:px-6 lg:px-8">
        <div className="flex min-w-0 items-center gap-8">
          <button
            type="button"
            onClick={() => navigate(user ? "/roadmaps" : "/")}
            className="shrink-0"
          >
            <AuthLogo compact showTagline={false} />
          </button>

          <nav className="hidden items-center gap-3 md:flex">
            {navItems.map((item) => {
              const isActive =
                location.pathname === item.path ||
                (item.path !== "/login" && location.pathname.startsWith(`${item.path}/`));

              return (
                <button
                  key={item.label}
                  type="button"
                  onClick={() => navigate(item.path)}
                  className={`rounded-lg border px-4 py-2 !text-[14px] font-bold transition ${
                    isActive
                      ? "border-[#6FCF97] bg-[#6FCF97]/24 text-[#1F6F5F]"
                      : "border-transparent text-slate-600 hover:border-[#B9D8CC] hover:bg-white hover:text-[#1F6F5F]"
                  }`}
                >
                  {item.label}
                </button>
              );
            })}
          </nav>
        </div>

        <div className="flex items-center gap-3">
          {user && (
            <div
              className={`hidden h-10 items-center gap-2 rounded-lg border px-3 text-xs font-extrabold shadow-sm sm:inline-flex ${
                isCompletedStreakToday
                  ? "border-[#6FCF97] bg-[#6FCF97]/30 text-[#1F6F5F]"
                  : "border-[#B9D8CC] bg-white text-[#18332D]"
              }`}
            >
              <FaFireAlt className={isCompletedStreakToday ? "text-orange-600" : "text-[#2FA084]"} />
              <span>{currentStreak} STREAK</span>
            </div>
          )}

          {user ? (
            <AvatarDropdown user={user} profile={user ? profile : null} onLogout={handleLogout} />
          ) : (
            <button
              type="button"
              onClick={() => navigate("/login")}
              className="rounded-lg bg-[#2FA084] px-4 py-2 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
            >
              Sign in
            </button>
          )}
        </div>
      </div>
    </header>
  );
}
