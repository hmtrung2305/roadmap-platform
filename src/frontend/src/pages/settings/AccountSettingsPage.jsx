import { useEffect, useMemo, useState } from "react";
import { Trash2 } from "lucide-react";
import { MdEmail } from "react-icons/md";
import SettingsSection from "../../components/settings/SettingsSection";
import SettingsRow from "../../components/settings/SettingsRow";
import { getCurrentUserApi } from "../../api/authApi";
import {
  getAuthProvidersApi,
  redirectToGoogleLink,
  redirectToGitHubLink,
  unlinkAuthProviderApi,
} from "../../api/authProviderApi";
import { FaGithub, FaKey, FaUser } from "react-icons/fa";
import { FcGoogle } from "react-icons/fc";
import { toast } from "react-toastify";
import ChangePasswordModal from "../../components/settings/ChangePasswordModal";
import AddLocalLoginModal from "../../components/settings/AddLocalLoginModal";
import VerifyLocalEmailModal from "../../components/settings/VerifyLocalEmailModal";
import EditUsernameModal from "../../components/settings/EditUsernameModal";
import ConfirmModal from "../../components/settings/ConfirmModal";
import ChangeEmailModal from "../../components/settings/ChangeEmailModal";

export default function AccountSettingsPage() {
  const [me, setMe] = useState(null);
  const [providers, setProviders] = useState([]);
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
  const fetchSettings = async () => {
    try {
      setLoading(true);
      setActionError("");

      const [meData, providersData] = await Promise.all([
        getCurrentUserApi(),
        getAuthProvidersApi(),
      ]);

      setMe(meData);
      setProviders(providersData);
    } catch (error) {
      console.error("Failed to load settings:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message || "Unable to load account settings.";

      setActionError(serverMessage);
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

        await unlinkAuthProviderApi(provider);
        await fetchSettings();

        toast.success(`${providerName} disconnected successfully.`);
        setConfirmAction(null);
      } catch (error) {
        console.error(
          `Disconnect ${providerName} failed:`,
          error.response?.data || error,
        );

        const serverMessage =
          error.response?.data?.message ||
          `Unable to disconnect ${providerName}.`;

        setActionError(serverMessage);
      } finally {
        setConfirmLoading(false);
      }
    }
  };

  const handleDeleteAccount = () => {
    // Phase sau mình sẽ thay bằng modal đẹp hơn.
    // Tạm thời chưa gọi API delete ở phase này để tránh bấm nhầm.
    setActionError("Delete account sẽ làm ở phase sau bằng confirm modal.");
  };

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl">
        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
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
        <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
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
              : "bg-indigo-50 text-indigo-700"
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
          actionLabel={googleProvider?.isLinked ? "Disconnect" : "Connect"}
          actionClassName={
            googleProvider?.isLinked
              ? "bg-red-50 text-red-600"
              : "bg-indigo-50 text-indigo-700"
          }
          isDelete={googleProvider?.isLinked}
          disabled={googleProvider?.isLinked && !googleProvider?.canUnlink}
          onClick={
            googleProvider?.isLinked
              ? () => openDisconnectConfirm("google")
              : redirectToGoogleLink
          }
        />

        <SettingsRow
          icon={FaGithub}
          title="GitHub"
          value={githubProvider?.isLinked ? "Connected" : "Not connected"}
          actionLabel={githubProvider?.isLinked ? "Disconnect" : "Connect"}
          actionClassName={
            githubProvider?.isLinked
              ? "bg-red-50 text-red-600"
              : "bg-indigo-50 text-indigo-700"
          }
          isDelete={githubProvider?.isLinked}
          disabled={githubProvider?.isLinked && !githubProvider?.canUnlink}
          onClick={
            githubProvider?.isLinked
              ? () => openDisconnectConfirm("github")
              : redirectToGitHubLink
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
            await fetchSettings();
            setActiveModal(null);
            toast.success("Password login added successfully.");
          }}
        />
      )}

      {activeModal === "change-password" && (
        <ChangePasswordModal
          onClose={() => setActiveModal(null)}
          onSuccess={async () => {
            await fetchSettings();
            toast.success("Password changed successfully.");
          }}
        />
      )}
      {activeModal === "edit-username" && (
        <EditUsernameModal
          currentUsername={me?.username}
          onClose={() => setActiveModal(null)}
          onSuccess={async () => {
            fetchSettings();
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
            await fetchSettings();
            setActiveModal(null);
            toast.success("Email changed successfully.");
          }}
        />
      )}
    </div>
  );
}
