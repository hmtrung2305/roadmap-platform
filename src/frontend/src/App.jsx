import { Navigate, Route, Routes } from "react-router-dom";
import { useEffect } from "react";
import { AnimatePresence } from "framer-motion";

import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import HomePage from "./pages/HomePage";
import PortfolioPage from "./pages/PortfolioPage";

import MainLayout from "./layouts/MainLayout";
import ProtectedRoute from "./routes/ProtectedRoute";
import PublicRoute from "./routes/PublicRoute";
import { useAuthStore } from "./stores/useAuthStore";
import ResourceManagementPage from "./pages/ResourceManagementPage";
import PublicLayout from "./layouts/PublicLayout";
import StudyRoomPage from "./pages/StudyRoomPage";
import EditProfilePage from "./pages/EditProfilePage";
import ManagePortfolioRepositoriesPage from "./pages/ManagePortfolioRepositoryPage";
import { ToastContainer } from "react-toastify";
import VerifyEmailPage from "./pages/VerifyEmailPage";

function AuthBootstrap({ children }) {
  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);
  useEffect(() => {
    loadCurrentUser();
  }, [loadCurrentUser]);

  return children;
}
export default function App() {
  return (
    <AuthBootstrap>
      <ToastContainer
        position="top-right"
        autoClose={2500}
        hideProgressBar={false}
        newestOnTop
        closeOnClick
        pauseOnHover
        draggable
      />
      <AnimatePresence>
        
        <Routes >
          <Route path="/" element={<Navigate to="/login" replace />} />

          <Route
            path="/login"
            element={
              <PublicRoute>
                <LoginPage />
              </PublicRoute>
            }
          />
          <Route
            path="/verify-email"
            element={
              <PublicRoute>
                <VerifyEmailPage/>
              </PublicRoute>
            }
          />

          <Route
            path="/register"
            element={
              <PublicRoute>
                <RegisterPage />
              </PublicRoute>
            }
          />

          {/* PUBLIC DEMO ROUTES - không cần auth */}

          <Route element={<MainLayout />}>
            <Route path="/resources" element={<ResourceManagementPage />} />
           
            <Route path="/study/:resourceId" element={<StudyRoomPage />} />
          </Route>

          {/* Cần auth thì mới access được */}
          <Route
            element={
              <ProtectedRoute>
                <MainLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/home" element={<HomePage />} />
            <Route path="/profile/edit" element={<EditProfilePage />} />
             <Route path="/portfolio" element={<PortfolioPage />} />
             <Route path="/portfolio/repositories" element={<ManagePortfolioRepositoriesPage />} />
          </Route>
        </Routes>
      </AnimatePresence>
    </AuthBootstrap>
  );
}
