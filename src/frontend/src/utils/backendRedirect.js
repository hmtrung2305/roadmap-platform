import { normalizeFetchErrorResponse } from "./apiErrorUtils";

export async function startBackendRedirect(url) {
  try {
    const response = await fetch(url, {
      method: "GET",
      credentials: "include",
      redirect: "manual",
      headers: {
        Accept: "application/json",
      },
    });

    if (
      response.type === "opaqueredirect" ||
      response.status === 0 ||
      (response.status >= 300 && response.status < 400)
    ) {
      window.location.assign(url);
      return;
    }

    if (!response.ok) {
      throw await normalizeFetchErrorResponse(response, url);
    }

    window.location.assign(url);
  } catch (error) {
    if (error?.isApiError || error?.code || error?.status) {
      throw error;
    }

    window.location.assign(url);
  }
}
