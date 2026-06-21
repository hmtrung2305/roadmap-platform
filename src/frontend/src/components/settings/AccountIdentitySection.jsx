/* eslint-disable react-hooks/set-state-in-effect, react-refresh/only-export-components */
import { useEffect, useMemo, useState } from "react";
import { Image, Phone, Save, UserRound } from "lucide-react";

import SettingsSection from "./SettingsSection";

function toFormValue(value) {
  return value == null ? "" : String(value);
}

function buildForm(accountProfile) {
  return {
    displayName: toFormValue(accountProfile?.displayName),
    avatarUrl: toFormValue(accountProfile?.avatarUrl),
    phoneNumber: toFormValue(accountProfile?.phoneNumber),
  };
}

function normalizeForm(form) {
  return {
    displayName: form.displayName.trim(),
    avatarUrl: form.avatarUrl.trim(),
    phoneNumber: form.phoneNumber.trim(),
  };
}

function isValidUrl(value) {
  if (!value) return true;

  try {
    const parsed = new URL(value);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

function validate(form) {
  const errors = {};
  const normalized = normalizeForm(form);

  if (normalized.displayName.length > 50) {
    errors.displayName = "Display name cannot exceed 50 characters.";
  }

  if (normalized.avatarUrl && !isValidUrl(normalized.avatarUrl)) {
    errors.avatarUrl = "Enter a valid image URL.";
  }

  if (normalized.phoneNumber.length > 32) {
    errors.phoneNumber = "Phone number cannot exceed 32 characters.";
  }

  return errors;
}

function Field({
  id,
  icon: Icon,
  label,
  value,
  placeholder,
  autoComplete,
  error,
  maxLength,
  onChange,
}) {
  return (
    <div>
      <label
        htmlFor={id}
        className="mb-2 block text-sm font-extrabold text-[#18332D]"
      >
        {label}
      </label>

      <div
        className={`flex items-center gap-3 rounded-lg border bg-white px-3 transition focus-within:ring-2 ${
          error
            ? "border-red-300 focus-within:border-red-400 focus-within:ring-red-100"
            : "border-[#B9D8CC] focus-within:border-[#2FA084] focus-within:ring-[#6FCF97]/20"
        }`}
      >
        <Icon size={17} className="shrink-0 text-[#1F6F5F]" />
        <input
          id={id}
          type="text"
          value={value}
          placeholder={placeholder}
          autoComplete={autoComplete}
          maxLength={maxLength}
          onChange={(event) => onChange(event.target.value)}
          className="min-w-0 flex-1 bg-transparent py-3 text-sm font-semibold text-[#18332D] outline-none placeholder:text-slate-400"
        />
      </div>

      {error && (
        <p className="mt-1.5 text-xs font-bold text-red-600" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}

export default function AccountIdentitySection({
  accountProfile,
  saving = false,
  onSave,
}) {
  const [form, setForm] = useState(() => buildForm(accountProfile));
  const [errors, setErrors] = useState({});
  const [avatarFailed, setAvatarFailed] = useState(false);

  useEffect(() => {
    setForm(buildForm(accountProfile));
    setErrors({});
  }, [accountProfile]);

  useEffect(() => {
    setAvatarFailed(false);
  }, [form.avatarUrl]);

  const normalizedForm = useMemo(() => normalizeForm(form), [form]);
  const normalizedInitial = useMemo(
    () => normalizeForm(buildForm(accountProfile)),
    [accountProfile],
  );

  const hasChanges =
    JSON.stringify(normalizedForm) !== JSON.stringify(normalizedInitial);

  const avatarUrl = normalizedForm.avatarUrl;
  const canShowAvatar = avatarUrl && isValidUrl(avatarUrl) && !avatarFailed;
  const avatarAlt = normalizedForm.displayName
    ? `${normalizedForm.displayName} avatar`
    : "Account avatar";

  const updateField = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    const nextErrors = validate(form);
    setErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    await onSave(normalizedForm);
  };

  return (
    <SettingsSection title="Profile">
      <form onSubmit={handleSubmit} className="space-y-6 px-6 py-6">
        <div className="grid gap-5 md:grid-cols-[80px_minmax(0,1fr)] md:items-start">
          <div className="grid h-20 w-20 shrink-0 place-items-center overflow-hidden rounded-xl border border-[#B9D8CC] bg-[#6FCF97]/18 text-[#1F6F5F]">
            {canShowAvatar ? (
              <img
                src={avatarUrl}
                alt={avatarAlt}
                onError={() => setAvatarFailed(true)}
                className="h-full w-full object-cover"
              />
            ) : (
              <UserRound size={30} aria-hidden="true" />
            )}
          </div>

          <div className="grid gap-5">
            <div className="grid gap-5 md:grid-cols-2">
              <Field
                id="content-manager-display-name"
                icon={UserRound}
                label="Display name"
                value={form.displayName}
                placeholder="Display name"
                autoComplete="name"
                maxLength={50}
                error={errors.displayName}
                onChange={(value) => updateField("displayName", value)}
              />

              <Field
                id="content-manager-phone-number"
                icon={Phone}
                label="Phone number"
                value={form.phoneNumber}
                placeholder="Phone number"
                autoComplete="tel"
                maxLength={32}
                error={errors.phoneNumber}
                onChange={(value) => updateField("phoneNumber", value)}
              />
            </div>

            <Field
              id="content-manager-avatar-url"
              icon={Image}
              label="Profile image URL"
              value={form.avatarUrl}
              placeholder="Image URL"
              autoComplete="url"
              error={errors.avatarUrl}
              onChange={(value) => updateField("avatarUrl", value)}
            />
          </div>
        </div>

        <div className="flex justify-end border-t border-slate-100 pt-5">
          <button
            type="submit"
            disabled={saving || !hasChanges}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#1F6F5F] px-4 py-2.5 text-sm font-extrabold text-white transition hover:bg-[#195C4F] disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Save size={16} aria-hidden="true" />
            {saving ? "Saving..." : "Save changes"}
          </button>
        </div>
      </form>
    </SettingsSection>
  );
}

export { buildForm, normalizeForm, validate };
