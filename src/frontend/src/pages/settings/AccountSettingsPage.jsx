/* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
import { useEffect, useMemo, useState } from "react";
import { Trash2 } from "lucide-react";
import { MdEmail } from "react-icons/md";
import SettingsSection from "../../features/settings/components/SettingsSection";
import SettingsRow from "../../features/settings/components/SettingsRow";
import {
  redirectToGoogleLink,
  redirectToGitHubLink,
} from "../../api/authProviderApi";
import { FaGithub, FaKey, FaUser } from "react-icons/fa";
import { FcGoogle } from "react-icons/fc";
import { toast } from "react-toastify";
import ChangePasswordModal from "../../features/settings/components/ChangePasswordModal";
import AddLocalLoginModal from "../../features/settings/components/AddLocalLoginModal";
import VerifyLocalEmailModal from "../../features/settings/components/VerifyLocalEmailModal";
import EditUsernameModal from "../../features/settings/components/EditUsernameModal";
import ConfirmModal from "../../features/settings/components/ConfirmModal";
import ChangeEmailModal from "../../features/settings/components/ChangeEmailModal";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { useAuthStore } from "../../stores/useAuthStore";
import { useAuthProviderStore } from "../../stores/useAuthProviderStore";

export default function AccountSettingsPage() {
  const me = useAuthStore((state) => state.user);
  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);
  const providers = useAuthProviderStore((state) => state.providers);
  const providerActionLoading = useAuthProviderStore((state) => state.actionLoading);
  const connectingProvider = useAuthProviderStore((state) => state.connectingProvider);
  const providerError = useAuthProviderStore((state) => state.error);
  const loadProviders = useAuthProviderStore((state) => state.loadProviders);
  const unlinkProvider = useAuthProviderStore((state) => state.unlinkProvider);
  const startConnectingProvider = useAuthProviderStore((state) => state.startConnectingProvider);
  const [loading, setLoading] = useState(true);
  const [actionError, setActionError] = useState("");
  const [activeModal, setActiveModal] = useState(null);
  const [pendingLocalEmail, setPendingLocalEmail] = useState("");
  const [confirmAction, setConfirmAction] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  const localProvider = useMemo(() => {
    return providers.find((item) => item.provider?.toLowerCase() === "local");
  }, [providers]);

  const googleProvider = useMemo(() => {
    return providers.find((item) => item.provider?.toLowerCase() === "google");
  }, [providers]);

  const githubProvider = useMemo(() => {
    return providers.find((item) => item.provider?.toLowerCase() === "github");
  }, [providers]);

  const localRequiresVerification = Boolean(
    localProvider?.requiresVerification || localProvider?.RequiresVerification,
  );

  const getProviderDisplayName = (provider) => {
    if (provider === "google") return "Google";
    if (provider === "github") return "GitHub";
    return provider;
  };

  const getProviderActionLabel = (provider, providerInfo) => {
    if (providerInfo?.isLinked) return "Disconnect";
    if (connectingProvider === provider) return "Connecting...";
    return "Connect";
  };

  const isProviderActionDisabled = (provider, providerInfo) => {
    const isSocialProviderActionLocked = Boolean(connectingProvider);

    if (providerInfo?.isLinked) {
      return providerActionLoading || isSocialProviderActionLocked || !providerInfo?.canUnlink;
    }

    return providerActionLoading || isSocialProviderActionLocked;
  };
  const fetchSettings = async ({ force = false } = {}) => {
    try {
      setLoading(true);
      setActionError("");

      await Promise.all([
        loadCurrentUser({ force }),
        loadProviders({ force }),
      ]);
    } catch (error) {
      console.error("Failed to load settings:", error.response?.data || error);

      setActionError(
        useAuthProviderStore.getState().error ||
          getFriendlyApiErrorMessage(error, "Unable to load account settings."),
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSettings();
  }, []);


  const openDisconnectConfirm = (provider) => {
    const providerName = getProviderDisplayName(provider);

    setConfirmAction({
      type: "disconnect-provider",
      provider,
      title: "Disconnect account",
      message: `Are you sure you want to disconnect your ${providerName} account? You may not be able to use it to sign in after disconnecting.`,
      confirmLabel: "Disconnect",
    });
  };
  const handleConfirmAction = async () => {
    if (!confirmAction) return;

    if (confirmAction.type === "disconnect-provider") {
      const provider = confirmAction.provider;
      const providerName = getProviderDisplayName(provider);

      try {
        setConfirmLoading(true);
        setActionError("");

        await unlinkProvider(provider);
        await loadCurrentUser({ force: true });

        toast.success(`${providerName} disconnected successfully.`);
        setConfirmAction(null);
      } catch (error) {
        console.error(
          `Disconnect ${providerName} failed:`,
          error.response?.data || error,
        );

        setActionError(getFriendlyApiErrorMessage(error, `Unable to disconnect ${providerName}.`));
      } finally {
        setConfirmLoading(false);
      }
    }
  };

  const handleProviderRedirect = (provider) => {
    const providerInfo = provider === "google" ? googleProvider : githubProvider;

    if (isProviderActionDisabled(provider, providerInfo)) return;

    setActionError("");

    const startedProvider = startConnectingProvider(provider);

    if (!startedProvider) {
      const message =
        providerError ||
        "Another account connection is already in progress. Please try again shortly.";

      setActionError(message);
      toast.error(message);
      return;
    }

    if (provider === "google") {
      redirectToGoogleLink();
      return;
    }

    redirectToGitHubLink();
  };

  const handleDeleteAccount = () => {
    // Phase sau mình sẽ thay bằng modal đẹp hơn.
    // Tạm thời chưa gọi API delete ở phase này để tránh bấm nhầm.
    setActionError("Delete account sẽ làm ở phase sau bằng confirm modal.");
  };

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl">
        <div className="tm-surface p-6">
          <p className="text-sm text-slate-500">Loading settings...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
          Account settings
        </h1>

        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-500">
          Manage your username, local email login, password, and connected
          social accounts.
        </p>
      </div>

      {actionError && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {actionError}
        </div>
      )}

      <SettingsSection
        title="General"
        description="Manage your basic account information and local login method."
      >
        <SettingsRow
          icon={FaUser}
          title="Username"
          value={me?.username || "Not set"}
          actionLabel="Edit"
          onClick={() => {
            setActiveModal("edit-username");
          }}
        />

        <SettingsRow
          icon={MdEmail}
          title="Email"
          value={
            localRequiresVerification
              ? "Pending verification"
              : me?.email
                ? me.email
                : "Not connected"
          }
          actionLabel={
            localRequiresVerification
              ? "Verify"
              : localProvider?.isLinked
                ? "Change"
                : "Add"
          }
          onClick={() => {
            if (localRequiresVerification) {
              setPendingLocalEmail(localProvider?.email || me?.email || "");
              setActiveModal("verify-local-email");
              return;
            }

            if (localProvider?.isLinked) {
              setActiveModal("change-email");
              return;
            }

            setActiveModal("add-local-login");
          }}
        />

        <SettingsRow
          icon={FaKey}
          title="Password"
          value={
            localRequiresVerification
              ? "Pending verification"
              : localProvider?.isLinked
                ? "Set"
                : "Not set"
          }
          actionLabel={
            localRequiresVerification
              ? "Verify"
              : localProvider?.isLinked
                ? "Change"
                : "Add"
          }
          actionClassName={
            localProvider?.isLinked
              ? "bg-slate-100 text-slate-700"
              : "bg-[#5A9CB5]/12 text-[#2F7F98]"
          }
          onClick={() => {
            if (localRequiresVerification) {
              setPendingLocalEmail(localProvider?.email || me?.email || "");
              setActiveModal("verify-local-email");
              return;
            }

            if (localProvider?.isLinked) {
              setActiveModal("change-password");
              return;
            }

            setActiveModal("add-local-login");
          }}
        />
      </SettingsSection>

      <SettingsSection
        title="Social accounts"
        description="Connect Google or GitHub to sign in faster."
      >
        <SettingsRow
          icon={FcGoogle}
          title="Google"
          value={googleProvider?.isLinked ? "Connected" : "Not connected"}
          actionLabel={getProviderActionLabel("google", googleProvider)}
          actionClassName={
            googleProvider?.isLinked
              ? "bg-red-50 text-red-600"
              : "bg-[#5A9CB5]/12 text-[#2F7F98]"
          }
          isDelete={googleProvider?.isLinked}
          disabled={isProviderActionDisabled("google", googleProvider)}
          onClick={
            googleProvider?.isLinked
              ? () => openDisconnectConfirm("google")
              : () => handleProviderRedirect("google")
          }
        />

        <SettingsRow
          icon={FaGithub}
          title="GitHub"
          value={githubProvider?.isLinked ? "Connected" : "Not connected"}
          actionLabel={getProviderActionLabel("github", githubProvider)}
          actionClassName={
            githubProvider?.isLinked
              ? "bg-red-50 text-red-600"
              : "bg-[#5A9CB5]/12 text-[#2F7F98]"
          }
          isDelete={githubProvider?.isLinked}
          disabled={isProviderActionDisabled("github", githubProvider)}
          onClick={
            githubProvider?.isLinked
              ? () => openDisconnectConfirm("github")
              : () => handleProviderRedirect("github")
          }
        />
      </SettingsSection>

      <SettingsSection
        title="Danger zone"
        description="Permanent actions that affect your account."
      >
        <SettingsRow
          icon={Trash2}
          title="Delete account"
          actionLabel="Delete"
          danger
          isDelete
          onClick={handleDeleteAccount}
        />
      </SettingsSection>

      {activeModal === "add-local-login" && (
        <AddLocalLoginModal
          defaultEmail={me?.email}
          onClose={() => setActiveModal(null)}
          onSuccess={(email) => {
            setPendingLocalEmail(email);
            setActiveModal("verify-local-email");
          }}
        />
      )}

      {activeModal === "verify-local-email" && (
        <VerifyLocalEmailModal
          email={pendingLocalEmail}
          onClose={() => setActiveModal(null)}
          onSuccess={async () => {
            await fetchSettings({ force: true });
            setActiveModal(null);
            toast.success("Password login added successfully.");
          }}
        />
      )}

      {activeModal === "change-password" && (
        <ChangePasswordModal
          onClose={() => setActiveModal(null)}
          onSuccess={async () => {
            await fetchSettings({ force: true });
            toast.success("Password changed successfully.");
          }}
        />
      )}
      {activeModal === "edit-username" && (
        <EditUsernameModal
          currentUsername={me?.username}
          onClose={() => setActiveModal(null)}
          onSuccess={async () => {
            fetchSettings({ force: true });
            toast.success("Username changed successfully.");
          }}
        />
      )}

      {confirmAction && (
        <ConfirmModal
          title={confirmAction.title}
          message={confirmAction.message}
          confirmLabel={confirmAction.confirmLabel}
          cancelLabel="Cancel"
          loading={confirmLoading}
          danger
          onCancel={() => {
            if (confirmLoading) return;
            setConfirmAction(null);
          }}
          onConfirm={handleConfirmAction}
        />
      )}
      {activeModal === "change-email" && (
        <ChangeEmailModal
          currentEmail={me?.email}
          onClose={() => setActiveModal(null)}
          onSuccess={(newEmail) => {
            setPendingLocalEmail(newEmail);
            setActiveModal("verify-email-change");
          }}
        />
      )}
      {activeModal === "verify-email-change" && (
        <VerifyLocalEmailModal
          mode="change-email"
          email={pendingLocalEmail}
          onClose={() => setActiveModal(null)}
          onSuccess={async () => {
            await fetchSettings({ force: true });
            setActiveModal(null);
            toast.success("Email changed successfully.");
          }}
        />
      )}
    </div>
  );
}
