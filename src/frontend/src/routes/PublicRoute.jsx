import { Navigate } from "react-router-dom";
import { useAuthStore } from "../stores/useAuthStore";

export default function PublicRoute({ children }) {
  const user = useAuthStore((state) => state.user);
  const authLoading = useAuthStore((state) => state.authLoading);
  

  const isAuthenticated = !!user;

  if (authLoading) {
    return <div>Loading...</div>;
  }

  if (isAuthenticated) {
    return <Navigate to="/home" replace />;
  }

  return children;
}