import http from "k6/http";
import { check, group, sleep } from "k6";
import { config } from "../helpers/config.js";
import {
  loginAndGetCookieHeader,
  optionalAuthCookieParams,
} from "../helpers/auth-cookie.js";
import { resolveRoadmapIds, graphSizeChecks } from "../helpers/roadmap.js";

export const options = {
  insecureSkipTLSVerify: config.insecureSkipTLSVerify,
  vus: Number(__ENV.VUS || 5),
  duration: __ENV.DURATION || "30s",
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<800"],
  },
};

export function setup() {
  const cookieHeader = config.authenticatedRead ? loginAndGetCookieHeader() : "";
  const ids = resolveRoadmapIds(cookieHeader);

  return {
    cookieHeader,
    roadmapVersionId: ids.roadmapVersionId,
    roadmapNodeId: ids.roadmapNodeId,
  };
}

export default function (data) {
  const params = optionalAuthCookieParams(data.cookieHeader);

  group("roadmap list", () => {
    const res = http.get(`${config.baseUrl}/api/roadmaps`, params);

    check(res, {
      "list status is 200": (r) => r.status === 200,
      "list returns body": (r) => r.body && r.body.length > 0,
    });
  });

  group("roadmap graph by slug", () => {
    const res = http.get(
      `${config.baseUrl}/api/roadmaps/${config.roadmapSlug}/graph`,
      params
    );

    graphSizeChecks(res);
  });

  group("roadmap node detail", () => {
    const res = http.get(
      `${config.baseUrl}/api/roadmaps/${data.roadmapVersionId}/nodes/${data.roadmapNodeId}`,
      params
    );

    check(res, {
      "node detail status is 200": (r) => r.status === 200,
      "node detail returns body": (r) => r.body && r.body.length > 0,
    });
  });

  sleep(1);
}
