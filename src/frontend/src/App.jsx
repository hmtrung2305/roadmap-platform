import { Navigate, Route, Routes } from "react-router-dom";
import { useEffect } from "react";
import { AnimatePresence } from "framer-motion";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import LandingPage from "./pages/LandingPage";
import DashboardPage from "./pages/DashboardPage";
import PortfolioPage from "./pages/PortfolioPage";
import MainLayout from "./layouts/MainLayout";
import ProtectedRoute from "./routes/ProtectedRoute";
import PublicRoute from "./routes/PublicRoute";
import { useAuthStore } from "./stores/useAuthStore";
import ResourceManagementPage from "./pages/ResourceManagementPage";
import StudyRoomPage from "./pages/StudyRoomPage";
import ManagePortfolioRepositoriesPage from "./pages/ManagePortfolioRepositoryPage";
import { ToastContainer } from "react-toastify";
import VerifyEmailPage from "./pages/VerifyEmailPage";
import SettingsLayout from "./pages/settings/SettingsLayout";
import AccountSettingsPage from "./pages/settings/AccountSettingsPage";
import PrivacySettingsPage from "./pages/settings/PrivacySettingsPage";
import ProfileSettingsPage from "./pages/settings/ProfileSettingsPage";
import PointsSettingsPage from "./pages/settings/PointSettingsPage";
import ProfilePage from "./pages/ProfilePage";

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
        <Routes>
          <Route path="/" element={<LandingPage />} />

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
                <VerifyEmailPage />
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

          {/* Public demo routes can be added here when they do not require auth. */}

          <Route element={<MainLayout />}></Route>

          {/* Authenticated workspace routes. */}
          <Route
            element={
              <ProtectedRoute>
                <MainLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/home" element={<Navigate to="/dashboard" replace />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route
              path="/portfolio/repositories"
              element={<ManagePortfolioRepositoriesPage />}
            />
            <Route path="/resources" element={<ResourceManagementPage />} />
            <Route path="/study/:resourceId" element={<StudyRoomPage />} />
          </Route>

          {/*Setting pages */}
          <Route path="/settings" element={<SettingsLayout />}>
            <Route
              index
              element={<Navigate to="/settings/account" replace />}
            />
            <Route path="account" element={<AccountSettingsPage />} />
            <Route path="privacy" element={<PrivacySettingsPage />} />
            <Route path="points" element={<PointsSettingsPage />} />
            <Route path="profile" element={<ProfileSettingsPage />} />
          </Route>
        </Routes>
      </AnimatePresence>
    </AuthBootstrap>
  );
}
