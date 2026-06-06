import { Navigate } from "react-router-dom";
import { useAuthStore } from "../stores/useAuthStore";

export default function PublicRoute({ children }) {
  const user = useAuthStore((state) => state.user);
  const authInitialized = useAuthStore((state) => state.authInitialized);

  const isAuthenticated = !!user;

  if (!authInitialized) {
    return <div>Loading...</div>;
  }

  if (isAuthenticated) {
    return <Navigate to="/home" replace />;
  }

  return children;
}