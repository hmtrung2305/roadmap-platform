import { Navigate, useLocation } from "react-router-dom";
import AppLoading from "../components/common/AppLoading";
import NotFoundPage from "../pages/NotFoundPage";
import { useAuthStore } from "../stores/useAuthStore";
import { canAccessRoute } from "../utils/authorizationUtils";

export default function RequirePermission({
  children,
  anyPermissions = [],
  allPermissions = [],
  anyRoles = [],
  allRoles = [],
}) {
  const location = useLocation();

  const user = useAuthStore((state) => state.user);
  const authLoading = useAuthStore((state) => state.authLoading);
  const authInitialized = useAuthStore((state) => state.authInitialized);

  if (authLoading || !authInitialized) {
    return (
      <AppLoading
        title="Checking your access"
        message="TechMap is verifying your permissions before opening this page."
        fullScreen
      />
    );
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (!canAccessRoute(user, { anyPermissions, allPermissions, anyRoles, allRoles })) {
    return <NotFoundPage />;
  }

  return children;
}
