import { useEffect, useRef, useState } from "react";
import {
  CheckCircle2,
  ChevronDown,
  Code2,
  LibraryBig,
  LogOut,
  Settings,
  Shield,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { redirectToGitHubLink } from "../../api/authProviderApi";
import { FaGithub } from "react-icons/fa";
import { toast } from "react-toastify";
import {
  ADMIN_SURFACE_PERMISSIONS,
  CONTENT_MANAGER_SURFACE_PERMISSIONS,
} from "../../constants/permissions";
import { hasAnyPermission } from "../../utils/authorizationUtils";
import { useAuthProviderStore } from "../../stores/useAuthProviderStore";

export default function AvatarDropdown({ user, profile, onLogout }) {
  const navigate = useNavigate();
  const menuRef = useRef(null);

  const [open, setOpen] = useState(false);
  const providers = useAuthProviderStore((state) => state.providers);
  const loadingProviders = useAuthProviderStore((state) => state.loading);
  const actionLoading = useAuthProviderStore((state) => state.actionLoading);
  const connectingProvider = useAuthProviderStore((state) => state.connectingProvider);
  const providerError = useAuthProviderStore((state) => state.error);
  const loadProviders = useAuthProviderStore((state) => state.loadProviders);
  const startConnectingProvider = useAuthProviderStore((state) => state.startConnectingProvider);

  const displayName =
    profile?.displayName || user?.username || user?.email || "User";

  const email = user?.email || "";
  const avatarUrl = profile?.avatarUrl;

  const githubProvider = providers.find(
    (provider) => provider.provider?.toLowerCase() === "github"
  );

  const isGitHubLinked = githubProvider?.isLinked ?? false;
  const canAccessContentManagerConsole = hasAnyPermission(user, CONTENT_MANAGER_SURFACE_PERMISSIONS);
  const canAccessAdminConsole = hasAnyPermission(user, ADMIN_SURFACE_PERMISSIONS);
  const isConnectingGitHub = connectingProvider === "github";
  const isSocialProviderActionLocked = Boolean(connectingProvider);
  const disableGitHubConnect =
    isGitHubLinked ||
    loadingProviders ||
    actionLoading ||
    isSocialProviderActionLocked;

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

    loadProviders().catch((error) => {
      console.error("Failed to load auth providers:", error);
    });
  }, [loadProviders, open]);


  const handleConnectGitHub = () => {
    if (disableGitHubConnect) return;

    const startedProvider = startConnectingProvider("github");

    if (!startedProvider) {
      toast.error(
        providerError ||
          "Another account connection is already in progress. Please try again shortly.",
      );
      return;
    }

    setOpen(false);
    redirectToGitHubLink();
  };

  const handleGoToEditPortfolio = () => {
    setOpen(false);
    navigate("/portfolio/edit");
  };

  const handleGoToSettings = () => {
    setOpen(false);
    navigate("/settings");
  };

  const handleGoToContentManagerConsole = () => {
    setOpen(false);
    navigate("/content");
  };

  const handleGoToAdminConsole = () => {
    setOpen(false);
    navigate("/admin");
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
        <div className="h-8 w-8 overflow-hidden rounded-full bg-[#6FCF97]/25">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={displayName}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center text-sm font-bold text-[#1F6F5F]">
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
        <div className="absolute right-0 top-12 z-[100] w-64 overflow-hidden rounded-lg border border-slate-200 bg-white py-2 shadow-xl">
          <div className="p-2">
            <DropdownItem
              icon={<Code2 size={18} />}
              label="Edit Portfolio"
              onClick={handleGoToEditPortfolio}
            />

            <DropdownItem
              icon={<Settings size={18} />}
              label="Settings"
              onClick={handleGoToSettings}
            />

            {canAccessContentManagerConsole && (
              <DropdownItem
                icon={<LibraryBig size={18} />}
                label="Content Manager Console"
                onClick={handleGoToContentManagerConsole}
              />
            )}

            {canAccessAdminConsole && (
              <DropdownItem
                icon={<Shield size={18} />}
                label="Admin Console"
                onClick={handleGoToAdminConsole}
              />
            )}

            {isGitHubLinked ? (
              <DropdownItem
                icon={<CheckCircle2 size={18} />}
                label="GitHub Connected"
                onClick={handleGoToEditPortfolio}
                active
              />
            ) : (
              <DropdownItem
                icon={<FaGithub size={18} />}
                label={
                  isConnectingGitHub
                    ? "Connecting GitHub..."
                    : loadingProviders
                      ? "Checking GitHub..."
                      : "Connect GitHub"
                }
                onClick={handleConnectGitHub}
                disabled={disableGitHubConnect}
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
              Exit
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
          : "text-slate-700 hover:bg-slate-50 hover:text-[#1F6F5F]"
      }`}
    >
      {icon}
      {label}
    </button>
  );
}