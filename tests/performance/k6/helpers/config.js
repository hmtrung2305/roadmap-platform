export const config = {
  baseUrl: __ENV.BASE_URL || "https://localhost:7103",

  // Your seed uses this slug.
  roadmapSlug: __ENV.ROADMAP_SLUG || "frontend-developer",

  // Optional. If empty, roadmap-read.test.js discovers these from the graph response.
  roadmapVersionId: __ENV.ROADMAP_VERSION_ID || "",
  roadmapNodeId: __ENV.ROADMAP_NODE_ID || "",

  // Required for protected enrollment/progress tests.
  roadmapEnrollmentId: __ENV.ROADMAP_ENROLLMENT_ID || "",

  // Cookie auth login config.
  // Change LOGIN_PATH if your login route is different.
  loginPath: __ENV.LOGIN_PATH || "/api/auth/login",
  testUser: __ENV.TEST_USER || "",
  testPassword: __ENV.TEST_PASSWORD || "",

  // For local ASP.NET HTTPS dev certificates.
  insecureSkipTLSVerify:
    (__ENV.INSECURE_SKIP_TLS_VERIFY || "true").toLowerCase() === "true",

  // Set AUTHENTICATED_READ=true if graph/node detail should include user progress.
  authenticatedRead:
    (__ENV.AUTHENTICATED_READ || "false").toLowerCase() === "true",
};

export function requireEnv(value, name) {
  if (!value) {
    throw new Error(`${name} is required. Set it as an environment variable.`);
  }

  return value;
}

export function jsonParams(extraHeaders = {}) {
  return {
    headers: {
      "Content-Type": "application/json",
      ...extraHeaders,
    },
  };
}
