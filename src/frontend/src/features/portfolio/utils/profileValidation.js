const OPTIONAL_URL_FIELDS = [
  "avatarUrl",
  "coverImageUrl",
  "githubUrl",
  "linkedinUrl",
  "personalWebsiteUrl",
];

const OPTIONAL_EMAIL_FIELDS = ["publicEmail"];

const optionalUrlMessages = {
  avatarUrl: "Please enter a valid avatar image URL, such as https://example.com/avatar.png.",
  coverImageUrl: "Please enter a valid cover image URL, such as https://example.com/cover.png.",
  githubUrl: "Please enter a valid GitHub URL, such as https://github.com/username.",
  linkedinUrl: "Please enter a valid LinkedIn URL, such as https://linkedin.com/in/username.",
  personalWebsiteUrl: "Please enter a valid website URL, such as https://example.com.",
};

export function normalizeOptionalProfileValue(value) {
  const normalizedValue = String(value ?? "").trim();
  return normalizedValue.length > 0 ? normalizedValue : "";
}

export function isValidOptionalEmail(value = "") {
  const normalizedValue = normalizeOptionalProfileValue(value);
  if (!normalizedValue) return true;

  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalizedValue);
}

export function isValidOptionalUrl(value = "") {
  const normalizedValue = normalizeOptionalProfileValue(value);
  if (!normalizedValue) return true;

  try {
    const url = new URL(normalizedValue);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

export function validatePortfolioProfileForm(form = {}) {
  const errors = {};

  if (!isValidOptionalEmail(form.publicEmail)) {
    errors.publicEmail = "Please enter a valid public email address.";
  }

  OPTIONAL_URL_FIELDS.forEach((fieldName) => {
    if (!isValidOptionalUrl(form[fieldName])) {
      errors[fieldName] = optionalUrlMessages[fieldName] || "Please enter a valid URL.";
    }
  });

  return errors;
}

export function hasPortfolioProfileErrors(errors = {}) {
  return Object.keys(errors).length > 0;
}

export function getPortfolioProfileErrorMessages(errors = {}) {
  return Object.values(errors).filter(Boolean);
}

export function getPortfolioProfileToastMessage(errors = {}) {
  const messages = getPortfolioProfileErrorMessages(errors);

  if (messages.length === 1) {
    return messages[0];
  }

  return "Please review the highlighted profile fields before saving.";
}

export function buildPortfolioProfileUpdatePayload(form = {}) {
  const payload = { ...form };

  [...OPTIONAL_EMAIL_FIELDS, ...OPTIONAL_URL_FIELDS].forEach((fieldName) => {
    const normalizedValue = normalizeOptionalProfileValue(form[fieldName]);

    if (normalizedValue) {
      payload[fieldName] = normalizedValue;
      return;
    }

    delete payload[fieldName];
  });

  return payload;
}
