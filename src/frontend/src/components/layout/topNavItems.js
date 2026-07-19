import { PERMISSIONS } from "../../constants/permissions";
import { hasPermission } from "../../utils/authorizationUtils";

export function buildTopNavItems(user, portfolioPath) {
  return [
    { label: "Roadmaps", path: user ? "/roadmaps" : "/login" },
    { label: "Learning", path: user ? "/learning-modules" : "/login" },
    { label: "Skill Gap", path: user ? "/skill-gap" : "/login" },
    ...(hasPermission(user, PERMISSIONS.MARKET_PULSE_VIEW_CATALOG)
      ? [{ label: "Market Pulse", path: "/market-pulse" }]
      : []),
    { label: "E-Portfolio", path: portfolioPath },
  ];
}
