/* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
import { useEffect, useMemo, useState } from "react";
import { KeyRound, Mail } from "lucide-react";
import { toast } from "react-toastify";

import SettingsSection from "../../features/settings/components/SettingsSection";
import SettingsRow from "../../features/settings/components/SettingsRow";
import AccountIdentitySection from "../../features/settings/components/AccountIdentitySection";
import CreatorProfileSection from "../../features/settings/components/CreatorProfileSection";
import ChangePasswordModal from "../../features/settings/components/ChangePasswordModal";
import AddLocalLoginModal from "../../features/settings/components//AddLocalLoginModal";
import VerifyLocalEmailModal from "../../features/settings/components/VerifyLocalEmailModal";
import ChangeEmailModal from "../../features/settings/components/ChangeEmailModal";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { useAuthStore } from "../../stores/useAuthStore";
import { useAuthProviderStore } from "../../stores/useAuthProviderStore";
import { useAccountProfileStore } from "../../stores/useAccountProfileStore";
import { useProfileStore } from "../../stores/useProfileStore";
import { useRoadmapStore } from "../../stores/useRoadmapStore";
import { useLearningModuleStore } from "../../stores/useLearningModuleStore";

export default function ContentManagerSettingsPage() {
  const me = useAuthStore((state) => state.user);
  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);

  const providers = useAuthProviderStore((state) => state.providers);
  const loadProviders = useAuthProviderStore((state) => state.loadProviders);

  const accountProfile = useAccountProfileStore(
    (state) => state.accountProfile,
  );
  const accountProfileSaving = useAccountProfileStore(
    (state) => state.saving,
  );
  const loadAccountProfile = useAccountProfileStore(
    (state) => state.loadAccountProfile,
  );
  const updateAccountProfile = useAccountProfileStore(
    (state) => state.updateAccountProfile,
  );

  const creatorProfile = useProfileStore((state) => state.profile);
  const creatorProfileSaving = useProfileStore((state) => state.saving);
  const loadCreatorProfile = useProfileStore((state) => state.loadProfile);
  const updateCreatorProfile = useProfileStore((state) => state.updateProfile);

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
        loadAccountProfile({ force }),
        loadCreatorProfile({ force }),
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

  const handleIdentitySave = async (payload) => {
    try {
      setActionError("");
      await updateAccountProfile(payload);
      await loadCreatorProfile({ force: true });
      toast.success("Account identity updated successfully.");
    } catch (error) {
      console.error(
        "Failed to update content manager identity:",
        error.response?.data || error,
      );
      setActionError(
        getFriendlyApiErrorMessage(
          error,
          "Unable to update account identity.",
        ),
      );
    }
  };

  const handleCreatorProfileSave = async (payload) => {
    try {
      setActionError("");
      await updateCreatorProfile(payload);
      useRoadmapStore.getState().resetRoadmaps();
      useLearningModuleStore.getState().resetLearningModules();
      toast.success("Creator profile updated successfully.");
    } catch (error) {
      console.error(
        "Failed to update creator profile:",
        error.response?.data || error,
      );
      setActionError(
        getFriendlyApiErrorMessage(
          error,
          "Unable to update creator profile.",
        ),
      );
      throw error;
    }
  };

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
      </div>

      {actionError && (
        <div
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700"
          role="alert"
        >
          {actionError}
        </div>
      )}

      <AccountIdentitySection
        accountProfile={accountProfile}
        saving={accountProfileSaving}
        onSave={handleIdentitySave}
      />

      <CreatorProfileSection
        profile={creatorProfile}
        saving={creatorProfileSaving}
        onSave={handleCreatorProfileSave}
      />

      <SettingsSection title="Login details">
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
