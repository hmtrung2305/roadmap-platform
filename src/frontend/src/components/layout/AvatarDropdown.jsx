import { useEffect, useRef, useState } from "react";
import {
  CheckCircle2,
  ChevronDown,
  LogOut,
  Settings,
  UserRound,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import {
  getAuthProvidersApi,
  redirectToGitHubLink,
} from "../../api/authProviderApi";
import { FaGithub } from "react-icons/fa";

export default function AvatarDropdown({ user, profile, onLogout }) {
  const navigate = useNavigate();
  const menuRef = useRef(null);

  const [open, setOpen] = useState(false);
  const [providers, setProviders] = useState([]);
  const [loadingProviders, setLoadingProviders] = useState(false);

  const displayName =
    profile?.displayName || user?.username || user?.email || "User";

  const email = user?.email || "";
  const avatarUrl = profile?.avatarUrl;

  const githubProvider = providers.find(
    (provider) => provider.provider === "github"
  );

  const isGitHubLinked = githubProvider?.isLinked ?? false;

  useEffect(() => {
    function handleClickOutside(event) {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  useEffect(() => {
    if (!open) return;

    async function fetchProviders() {
      try {
        setLoadingProviders(true);
        const data = await getAuthProvidersApi();
        setProviders(data);
      } catch (error) {
        console.error("Failed to load auth providers:", error);
      } finally {
        setLoadingProviders(false);
      }
    }

    fetchProviders();
  }, [open]);

  const handleConnectGitHub = () => {
    setOpen(false);
    redirectToGitHubLink();
  };

  const handleGoToProfile = () => {
    setOpen(false);
    navigate("/profile");
  };

  const handleGoToSettings = () => {
    setOpen(false);
    navigate("/settings");
  };

  const handleLogout = () => {
    setOpen(false);
    onLogout();
  };

  return (
    <div ref={menuRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className="flex h-10 items-center gap-2 border border-[#B9D8CC] bg-white px-2 shadow-sm transition hover:-translate-y-0.5"
      >
        <div className="flex h-7 w-7 items-center justify-center overflow-hidden border border-[#B9D8CC] bg-[#2FA084] text-xs font-extrabold text-white">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={displayName}
              className="h-full w-full object-cover"
            />
          ) : (
            displayName.charAt(0).toUpperCase()
          )}
        </div>

        <div className="hidden min-w-0 text-left lg:block">
          <p className="max-w-32 truncate text-sm font-extrabold leading-4 text-[#18332D]">
            {displayName}
          </p>
        </div>

        <ChevronDown
          size={15}
          className={`text-[#18332D] transition ${open ? "rotate-180" : ""}`}
        />
      </button>

      {open && (
        <div className="absolute right-0 top-12 z-[100] w-64 border border-[#B9D8CC] bg-white py-2 shadow-xl shadow-emerald-900/10">
          <div className="border-b-2 border-[#B9D8CC] px-4 pb-3 pt-2">
            <p className="truncate text-sm font-extrabold text-[#18332D]">{displayName}</p>
            <p className="mt-1 truncate text-xs font-semibold text-slate-500">
              {email || "Account"}
            </p>
          </div>

          <div className="p-2">
            <DropdownItem
              icon={<UserRound size={18} />}
              label="View Profile"
              onClick={handleGoToProfile}
            />

            <DropdownItem
              icon={<Settings size={18} />}
              label="Settings"
              onClick={handleGoToSettings}
            />

            {isGitHubLinked ? (
              <DropdownItem
                icon={<CheckCircle2 size={18} />}
                label="GitHub Connected"
                onClick={() => {
                  setOpen(false);
                  navigate("/portfolio/repositories");
                }}
                active
              />
            ) : (
              <DropdownItem
                icon={<FaGithub size={18} />}
                label={loadingProviders ? "Checking GitHub..." : "Connect GitHub"}
                onClick={handleConnectGitHub}
                disabled={loadingProviders}
              />
            )}
          </div>

          <div className="border-t-2 border-[#B9D8CC] p-2">
            <button
              type="button"
              onClick={handleLogout}
              className="flex w-full items-center gap-3 px-4 py-3 text-left text-sm font-extrabold text-red-600 hover:bg-red-50"
            >
              <LogOut size={18} />
              Sign out
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function DropdownItem({ icon, label, onClick, active = false, disabled = false }) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`flex w-full items-center gap-3 px-4 py-3 text-left text-sm font-extrabold transition disabled:cursor-not-allowed disabled:opacity-60 ${
        active
          ? "bg-[#6FCF97]/40 text-[#1F6F5F]"
          : "text-[#18332D] hover:bg-[#EEEEEE]"
      }`}
    >
      {icon}
      {label}
    </button>
  );
}
