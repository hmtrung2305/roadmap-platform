import http from "k6/http";
import { check, group, sleep } from "k6";
import { config, requireEnv } from "../helpers/config.js";
import { loginAndGetCookieHeader, authCookieParams } from "../helpers/auth-cookie.js";
import { resolveRoadmapIds } from "../helpers/roadmap.js";

export const options = {
  insecureSkipTLSVerify: config.insecureSkipTLSVerify,
  vus: Number(__ENV.VUS || 3),
  duration: __ENV.DURATION || "30s",
  thresholds: {
    http_req_failed: ["rate<0.02"],
    http_req_duration: ["p(95)<1000"],
  },
};

export function setup() {
  const cookieHeader = loginAndGetCookieHeader();
  const ids = resolveRoadmapIds(cookieHeader);

  return {
    cookieHeader,
    roadmapVersionId: ids.roadmapVersionId,
  };
}

export default function (data) {
  const params = authCookieParams(data.cookieHeader);

  group("get current enrollment", () => {
    const res = http.get(
      `${config.baseUrl}/api/roadmap-enrollments/current?roadmapVersionId=${data.roadmapVersionId}`,
      params
    );

    check(res, {
      "current enrollment status is 200 or 204": (r) => r.status === 200 || r.status === 204,
    });
  });

  group("enroll roadmap", () => {
    const res = http.post(
      `${config.baseUrl}/api/roadmap-enrollments`,
      JSON.stringify({
        roadmapVersionId: data.roadmapVersionId,
      }),
      params
    );

    // 400 is accepted because the user may already be enrolled.
    check(res, {
      "enroll status is 200 or already-enrolled 400": (r) => r.status === 200 || r.status === 400,
    });
  });

  sleep(1);
}
