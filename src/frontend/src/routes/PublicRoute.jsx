import { Navigate } from "react-router-dom";
import AppLoading from "../components/common/AppLoading";
import { useAuthStore } from "../stores/useAuthStore";

export default function PublicRoute({ children }) {
  const user = useAuthStore((state) => state.user);
  const authInitialized = useAuthStore((state) => state.authInitialized);
  const authLoading = useAuthStore((state) => state.authLoading);

  const isAuthenticated = !!user;

  if (authLoading || !authInitialized) {
    return (
      <AppLoading
        title="Preparing TechMap"
        message="Please wait while we prepare your authentication state."
        fullScreen
      />
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}
