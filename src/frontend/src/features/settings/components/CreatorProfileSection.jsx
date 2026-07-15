/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import { FaGithub, FaLinkedin } from "react-icons/fa";
import { Globe2, Save } from "lucide-react";
import { toast } from "react-toastify";

import { getApiValidationErrors, getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";
import SettingsSection from "./SettingsSection";

const API_FIELD_MAP = {
  headline: "headline",
  bio: "bio",
  githuburl: "githubUrl",
  linkedinurl: "linkedinUrl",
  personalwebsiteurl: "personalWebsiteUrl",
};

function toFormValue(value) {
  return value == null ? "" : String(value);
}

function toNullableUrl(value) {
  const normalized = toFormValue(value).trim();
  return normalized || null;
}

function buildForm(profile) {
  return {
    headline: toFormValue(profile?.headline),
    bio: toFormValue(profile?.bio),
    githubUrl: toFormValue(profile?.githubUrl),
    linkedinUrl: toFormValue(profile?.linkedinUrl),
    personalWebsiteUrl: toFormValue(profile?.personalWebsiteUrl),
    isPublic: Boolean(profile?.isPublic),
  };
}

function normalizeForm(form) {
  return {
    headline: form.headline.trim(),
    bio: form.bio.trim(),
    githubUrl: toNullableUrl(form.githubUrl),
    linkedinUrl: toNullableUrl(form.linkedinUrl),
    personalWebsiteUrl: toNullableUrl(form.personalWebsiteUrl),
    isPublic: Boolean(form.isPublic),
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
  const normalized = normalizeForm(form);
  const errors = {};

  if (normalized.headline.length > 150) {
    errors.headline = "Headline cannot exceed 150 characters.";
  }

  if (normalized.bio.length > 500) {
    errors.bio = "Bio cannot exceed 500 characters.";
  }

  ["githubUrl", "linkedinUrl", "personalWebsiteUrl"].forEach((field) => {
    if (!isValidUrl(normalized[field])) {
      errors[field] = "Enter a complete URL beginning with http:// or https://.";
    }
  });

  return errors;
}

function mapApiValidationErrors(error) {
  const validationErrors = getApiValidationErrors(error);

  if (!validationErrors || typeof validationErrors !== "object") {
    return {};
  }

  return Object.entries(validationErrors).reduce((mappedErrors, [field, messages]) => {
    const normalizedField = String(field).replace(/[^a-z0-9]/gi, "").toLowerCase();
    const formField = API_FIELD_MAP[normalizedField];
    const firstMessage = Array.isArray(messages) ? messages.find(Boolean) : messages;

    if (formField && firstMessage) {
      mappedErrors[formField] = String(firstMessage);
    }

    return mappedErrors;
  }, {});
}

function getFirstErrorMessage(errors) {
  return Object.values(errors).find(Boolean) || "";
}

function UrlField({
  id,
  icon: Icon,
  label,
  value,
  error,
  placeholder,
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
            ? "border-red-400 focus-within:border-red-500 focus-within:ring-red-100"
            : "border-[#B9D8CC] focus-within:border-[#2FA084] focus-within:ring-[#6FCF97]/20"
        }`}
      >
        <Icon size={17} className="shrink-0 text-[#1F6F5F]" aria-hidden="true" />
        <input
          id={id}
          type="text"
          inputMode="url"
          autoCapitalize="none"
          autoCorrect="off"
          value={value}
          placeholder={placeholder}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${id}-error` : undefined}
          onChange={(event) => onChange(event.target.value)}
          className="min-w-0 flex-1 bg-transparent py-3 text-sm font-semibold text-[#18332D] outline-none placeholder:text-slate-400"
        />
      </div>
      {error && (
        <p id={`${id}-error`} className="mt-1.5 text-xs font-bold text-red-600">
          {error}
        </p>
      )}
    </div>
  );
}

export default function CreatorProfileSection({
  profile,
  saving = false,
  onSave,
}) {
  const [form, setForm] = useState(() => buildForm(profile));
  const [errors, setErrors] = useState({});

  useEffect(() => {
    setForm(buildForm(profile));
    setErrors({});
  }, [profile]);

  const normalizedForm = useMemo(() => normalizeForm(form), [form]);
  const normalizedInitial = useMemo(
    () => normalizeForm(buildForm(profile)),
    [profile],
  );
  const hasChanges =
    JSON.stringify(normalizedForm) !== JSON.stringify(normalizedInitial);

  const updateField = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    const clientErrors = validate(form);
    setErrors(clientErrors);

    if (Object.keys(clientErrors).length > 0) {
      toast.error(getFirstErrorMessage(clientErrors));
      return;
    }

    try {
      await onSave(normalizedForm);
    } catch (error) {
      const backendErrors = mapApiValidationErrors(error);

      if (Object.keys(backendErrors).length > 0) {
        setErrors((current) => ({ ...current, ...backendErrors }));
      }

      toast.error(
        getFirstErrorMessage(backendErrors) ||
          getFriendlyApiErrorMessage(error, "Unable to update creator profile."),
      );
    }
  };

  return (
    <SettingsSection
      title="Creator profile"
      description="Shown beside your published roadmaps and learning modules. Social links are optional."
    >
      <form noValidate onSubmit={handleSubmit} className="space-y-5 px-6 py-6">
        <div>
          <label
            htmlFor="creator-headline"
            className="mb-2 block text-sm font-extrabold text-[#18332D]"
          >
            Headline
          </label>
          <input
            id="creator-headline"
            value={form.headline}
            maxLength={150}
            placeholder="Frontend mentor, backend engineer, or subject expert"
            onChange={(event) => updateField("headline", event.target.value)}
            className="w-full rounded-lg border border-[#B9D8CC] bg-white px-3 py-3 text-sm font-semibold text-[#18332D] outline-none transition focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/20"
          />
          {errors.headline && (
            <p className="mt-1.5 text-xs font-bold text-red-600">
              {errors.headline}
            </p>
          )}
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between gap-3">
            <label
              htmlFor="creator-bio"
              className="text-sm font-extrabold text-[#18332D]"
            >
              Short bio
            </label>
            <span className="text-xs font-bold text-slate-400">
              {form.bio.length}/500
            </span>
          </div>
          <textarea
            id="creator-bio"
            value={form.bio}
            maxLength={500}
            rows={4}
            placeholder="Briefly describe your experience and the topics you create content for."
            onChange={(event) => updateField("bio", event.target.value)}
            className="w-full resize-none rounded-lg border border-[#B9D8CC] bg-white px-3 py-3 text-sm font-semibold leading-6 text-[#18332D] outline-none transition focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/20"
          />
          {errors.bio && (
            <p className="mt-1.5 text-xs font-bold text-red-600">
              {errors.bio}
            </p>
          )}
        </div>

        <div className="grid gap-5 md:grid-cols-2">
          <UrlField
            id="creator-github-url"
            icon={FaGithub}
            label="GitHub"
            value={form.githubUrl}
            error={errors.githubUrl}
            placeholder="https://github.com/username"
            onChange={(value) => updateField("githubUrl", value)}
          />
          <UrlField
            id="creator-linkedin-url"
            icon={FaLinkedin}
            label="LinkedIn"
            value={form.linkedinUrl}
            error={errors.linkedinUrl}
            placeholder="https://linkedin.com/in/username"
            onChange={(value) => updateField("linkedinUrl", value)}
          />
        </div>

        <UrlField
          id="creator-website-url"
          icon={Globe2}
          label="Personal website"
          value={form.personalWebsiteUrl}
          error={errors.personalWebsiteUrl}
          placeholder="https://your-site.com"
          onChange={(value) => updateField("personalWebsiteUrl", value)}
        />

        <label className="flex cursor-pointer items-start justify-between gap-4 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/55 px-4 py-3">
          <span>
            <span className="block text-sm font-extrabold text-[#18332D]">
              Show creator profile to learners
            </span>
            <span className="mt-1 block text-xs font-semibold leading-5 text-slate-500">
              Learners can see this profile on your published content.
            </span>
          </span>
          <input
            type="checkbox"
            checked={form.isPublic}
            onChange={(event) => updateField("isPublic", event.target.checked)}
            className="mt-1 h-5 w-5 rounded border-[#B9D8CC] text-[#1F6F5F] focus:ring-[#6FCF97]"
          />
        </label>

        <div className="flex justify-end border-t border-slate-100 pt-5">
          <button
            type="submit"
            disabled={saving || !hasChanges}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#1F6F5F] px-4 py-2.5 text-sm font-extrabold text-white transition hover:bg-[#195C4F] disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Save size={16} />
            {saving ? "Saving..." : "Save creator profile"}
          </button>
        </div>
      </form>
    </SettingsSection>
  );
}
