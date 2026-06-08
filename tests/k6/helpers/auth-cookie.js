import http from "k6/http";
import { check, fail } from "k6";
import { config, jsonParams } from "./config.js";

function toCookieHeader(cookies) {
  return Object.entries(cookies)
    .map(([name, values]) => {
      const first = Array.isArray(values) ? values[0] : values;
      return `${name}=${first.value}`;
    })
    .join("; ");
}

export function loginAndGetCookieHeader() {
  if (!config.testUser || !config.testPassword) {
    fail("TEST_USER and TEST_PASSWORD are required for cookie authentication.");
  }

  const res = http.post(
    `${config.baseUrl}${config.loginPath}`,
    JSON.stringify({
      emailOrUsername: config.testUser,
      password: config.testPassword,
    }),
    jsonParams()
  );

  check(res, {
    "login status is 200": (r) => r.status === 200,
    "login returned cookies": (r) => Object.keys(r.cookies || {}).length > 0,
  });

  if (res.status !== 200) {
    fail(`Login failed. Status: ${res.status}. Body: ${res.body}`);
  }

  const cookieHeader = toCookieHeader(res.cookies || {});

  if (!cookieHeader) {
    fail("Login succeeded, but no auth cookie was returned.");
  }

  return cookieHeader;
}

export function authCookieParams(cookieHeader) {
  return jsonParams({
    Cookie: cookieHeader,
  });
}

export function optionalAuthCookieParams(cookieHeader) {
  if (!cookieHeader) {
    return jsonParams();
  }

  return authCookieParams(cookieHeader);
}
