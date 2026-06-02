
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../stores/useAuthStore';

function ProtectedRoute( { children }) {
    const user = useAuthStore((state) => state.user);
    const authLoading = useAuthStore((state) => state.authLoading);
    const isAuthenticated = !!user;
    const authInitialized = useAuthStore((state) => state.authInitialized)

    if(authLoading||!authInitialized){
      return <div>Loading...</div>
    }
    if(!isAuthenticated){
        return <Navigate to="/login" replace />;
    }
  return children;
}

export default ProtectedRoute