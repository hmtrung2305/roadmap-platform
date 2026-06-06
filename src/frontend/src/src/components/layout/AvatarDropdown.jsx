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
        className="flex h-11 items-center gap-3 rounded-lg border border-slate-200 bg-white px-2.5 pr-3 shadow-sm transition hover:bg-slate-50"
      >
        <div className="h-8 w-8 overflow-hidden rounded-full bg-blue-100">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={displayName}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-sm font-bold text-blue-700">
              {displayName.charAt(0).toUpperCase()}
            </div>
          )}
        </div>

        <div className="hidden min-w-0 text-left lg:block">
          <p className="max-w-36 truncate text-sm font-semibold leading-4 text-slate-800">
            {displayName}
          </p>

          <p className="mt-0.5 max-w-44 truncate text-xs leading-4 text-slate-500">
            {email || "Account"}
          </p>
        </div>

        <ChevronDown
          size={15}
          className={`text-slate-500 transition ${open ? "rotate-180" : ""}`}
        />
      </button>

      {open && (
        <div className="absolute right-0 top-12 z-[100] w-64 overflow-hidden rounded-xl border border-slate-200 bg-white py-2 shadow-xl">
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

          <div className="mt-1 border-t border-slate-100 p-2">
            <button
              type="button"
              onClick={handleLogout}
              className="flex w-full items-center gap-3 rounded-lg px-4 py-3 text-left text-sm font-semibold text-red-600 hover:bg-red-50"
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
      className={`flex w-full items-center gap-3 rounded-lg px-4 py-3 text-left text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-60 ${
        active
          ? "bg-emerald-50 text-emerald-700 hover:bg-emerald-100"
          : "text-slate-700 hover:bg-slate-50 hover:text-blue-700"
      }`}
    >
      {icon}
      {label}
    </button>
  );
}