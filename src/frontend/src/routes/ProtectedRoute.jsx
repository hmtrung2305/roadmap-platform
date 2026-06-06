import { Navigate, useLocation } from "react-router-dom";
import AppLoading from "../components/common/AppLoading";
import { useAuthStore } from "../stores/useAuthStore";

function ProtectedRoute({ children }) {
  const location = useLocation();

  const user = useAuthStore((state) => state.user);
  const authLoading = useAuthStore((state) => state.authLoading);
  const authInitialized = useAuthStore((state) => state.authInitialized);

  const isAuthenticated = !!user;

  if (authLoading || !authInitialized) {
    return (
      <AppLoading
        title="Checking your session"
        message="TechMap is verifying your login before opening this page."
        fullScreen
      />
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return children;
}

export default ProtectedRoute;
