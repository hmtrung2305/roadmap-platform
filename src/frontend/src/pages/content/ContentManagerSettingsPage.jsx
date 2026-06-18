import { useEffect, useMemo, useState } from "react";
import { KeyRound, Mail } from "lucide-react";
import { toast } from "react-toastify";

import SettingsSection from "../../components/settings/SettingsSection";
import SettingsRow from "../../components/settings/SettingsRow";
import ChangePasswordModal from "../../components/settings/ChangePasswordModal";
import AddLocalLoginModal from "../../components/settings/AddLocalLoginModal";
import VerifyLocalEmailModal from "../../components/settings/VerifyLocalEmailModal";
import ChangeEmailModal from "../../components/settings/ChangeEmailModal";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { useAuthStore } from "../../stores/useAuthStore";
import { useAuthProviderStore } from "../../stores/useAuthProviderStore";

export default function ContentManagerSettingsPage() {
  const me = useAuthStore((state) => state.user);
  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);

  const providers = useAuthProviderStore((state) => state.providers);
  const loadProviders = useAuthProviderStore((state) => state.loadProviders);

  const [isLoading, setIsLoading] = useState(true);
  const [actionError, setActionError] = useState("");
  const [activeModal, setActiveModal] = useState(null);
  const [pendingLocalEmail, setPendingLocalEmail] = useState("");

  const localProvider = useMemo(() => {
    return providers.find((item) => item.provider?.toLowerCase() === "local");
  }, [providers]);

  const localRequiresVerification = Boolean(
    localProvider?.requiresVerification || localProvider?.RequiresVerification,
  );

  const fetchSettings = async ({ force = false } = {}) => {
    try {
      setIsLoading(true);
      setActionError("");

      await Promise.all([
        loadCurrentUser({ force }),
        loadProviders({ force }),
      ]);
    } catch (error) {
      console.error(
        "Failed to load content manager settings:",
        error.response?.data || error,
      );
      setActionError(
        getFriendlyApiErrorMessage(
          error,
          "Unable to load content manager settings.",
        ),
      );
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchSettings();
  }, []);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl px-6 py-8">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-sm font-semibold text-slate-600 shadow-sm">
          Loading settings...
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6 px-6 py-8">
      <div>
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          Account
        </p>
        <h1 className="mt-1 text-3xl font-black tracking-[-0.035em] text-[#18332D]">
          Content Manager Settings
        </h1>
        <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
          Keep your content manager login details up to date.
        </p>
      </div>

      {actionError && (
        <div
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700"
          role="alert"
        >
          {actionError}
        </div>
      )}

      <SettingsSection
        title="Login details"
        description="Update the email and password used to sign in to this content manager account."
      >
        <SettingsRow
          icon={Mail}
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
          iconClassName="bg-[#6FCF97]/18 text-[#1F6F5F]"
          actionClassName="bg-[#6FCF97]/18 text-[#1F6F5F]"
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
          icon={KeyRound}
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
          iconClassName="bg-[#6FCF97]/18 text-[#1F6F5F]"
          actionClassName="bg-slate-100 text-slate-700"
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
            setActiveModal(null);
            toast.success("Password changed successfully.");
          }}
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
