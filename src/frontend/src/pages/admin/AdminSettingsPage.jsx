import { useEffect, useMemo, useState } from "react";
import { KeyRound, Mail } from "lucide-react";
import { toast } from "react-toastify";

import { getCurrentUserApi } from "../../api/authApi";
import { getAuthProvidersApi } from "../../api/authProviderApi";
import SettingsSection from "../../components/settings/SettingsSection";
import SettingsRow from "../../components/settings/SettingsRow";
import ChangePasswordModal from "../../components/settings/ChangePasswordModal";
import AddLocalLoginModal from "../../components/settings/AddLocalLoginModal";
import VerifyLocalEmailModal from "../../components/settings/VerifyLocalEmailModal";
import ChangeEmailModal from "../../components/settings/ChangeEmailModal";

export default function AdminSettingsPage() {
  const [me, setMe] = useState(null);
  const [providers, setProviders] = useState([]);
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

  const fetchSettings = async () => {
    try {
      setIsLoading(true);
      setActionError("");

      const [meData, providersData] = await Promise.all([
        getCurrentUserApi(),
        getAuthProvidersApi(),
      ]);

      setMe(meData);
      setProviders(providersData);
    } catch (error) {
      console.error("Failed to load admin settings:", error.response?.data || error);
      setActionError(error.response?.data?.message || "Unable to load admin settings.");
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
          Admin Settings
        </h1>
        <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
          Manage the sign-in details for this counselor account.
        </p>
      </div>

      {actionError && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
          {actionError}
        </div>
      )}

      <SettingsSection
        title="Login details"
        description="Only account security settings are shown in the admin area."
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
            await fetchSettings();
            setActiveModal(null);
            toast.success("Email changed successfully.");
          }}
        />
      )}
    </div>
  );
}
