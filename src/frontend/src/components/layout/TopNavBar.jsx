import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { FaFireAlt } from "react-icons/fa";

import { useStreakStore } from "../../stores/useStreakStore";
import { useAuthStore } from "../../stores/useAuthStore";
import { getMyProfileApi } from "../../api/profileApi";
import AvatarDropdown from "./AvatarDropdown";

export default function TopNavbar() {
  const navItems = [
    { label: "Dashboard", path: "/dashboard" },
    { label: "Resources", path: "/resources" },
    { label: "Portfolio", path: "/portfolio" },
    { label: "Market Pulse", path: "/market-pulse" },
  ];

  const navigate = useNavigate();
  const location = useLocation();

  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const streak = useStreakStore((state) => state.streak);

  const [profile, setProfile] = useState(null);

  const currentStreak = streak?.currentStreak ?? 0;
  const isCompletedStreakToday = streak?.isCompletedStreakToday ?? false;

  useEffect(() => {
    if (!user) return;

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

  const handleLogout = async () => {
    await logout();
    navigate("/login");
  };

  return (
    <header className="sticky top-0 z-50 border-b border-slate-200 bg-white/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between">
        {/* LEFT SIDE - giữ nguyên */}
        <div className="flex items-center gap-8">
          <button
            type="button"
            onClick={() => navigate("/dashboard")}
            className="text-2xl font-bold text-blue-700"
          >
            TechMap
          </button>

          <nav className="flex items-center gap-6 text-sm font-medium text-slate-600">
            {navItems.map((item) => {
              const isActive =
                location.pathname === item.path ||
                location.pathname.startsWith(`${item.path}/`);

              return (
                <button
                  key={item.label}
                  type="button"
                  onClick={() => navigate(item.path)}
                  className={`group relative pb-1 transition-all duration-200 hover:-translate-y-0.5 hover:text-blue-700 ${
                    isActive ? "text-blue-700" : "text-slate-600"
                  }`}
                >
                  {item.label}

                  <span
                    className={`absolute bottom-0 left-1/2 h-0.5 -translate-x-1/2 rounded-full bg-blue-700 transition-all duration-300 ease-out ${
                      isActive ? "w-full" : "w-0 group-hover:w-full"
                    }`}
                  />
                </button>
              );
            })}
          </nav>
        </div>

        {/* RIGHT SIDE - cụm bên phải */}
        <div className="flex items-center gap-4">
          <div
            className={`flex h-10 items-center gap-2 rounded-lg border px-3 text-sm font-semibold shadow-sm transition ${
              isCompletedStreakToday
                ? "border-orange-200 bg-orange-50 text-orange-600"
                : "border-slate-200 bg-white text-slate-500"
            }`}
            title={
              isCompletedStreakToday
                ? "You completed your streak today"
                : "Open a learning resource to complete today's streak"
            }
          >
            <FaFireAlt
              className={`text-base ${
                isCompletedStreakToday ? "text-orange-500" : "text-slate-400"
              }`}
            />
            <span>{currentStreak}</span>
          </div>

          <AvatarDropdown
            user={user}
            profile={profile}
            onLogout={handleLogout}
          />
        </div>
      </div>
    </header>
  );
}
