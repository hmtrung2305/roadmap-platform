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
    roadmapNodeId: ids.roadmapNodeId,
  };
}

export default function (data) {
  const roadmapEnrollmentId = requireEnv(
    config.roadmapEnrollmentId,
    "ROADMAP_ENROLLMENT_ID"
  );

  const params = authCookieParams(data.cookieHeader);

  group("update node progress to in_progress", () => {
    const res = http.patch(
      `${config.baseUrl}/api/roadmap-enrollments/${roadmapEnrollmentId}/nodes/${data.roadmapNodeId}/progress`,
      JSON.stringify({
        status: "in_progress",
      }),
      params
    );

    check(res, {
      "progress in_progress status is 200": (r) => r.status === 200,
    });
  });

  group("update node progress to completed", () => {
    const res = http.patch(
      `${config.baseUrl}/api/roadmap-enrollments/${roadmapEnrollmentId}/nodes/${data.roadmapNodeId}/progress`,
      JSON.stringify({
        status: "completed",
      }),
      params
    );

    check(res, {
      "progress completed status is 200": (r) => r.status === 200,
    });
  });

  sleep(1);
}
