import { Navigate, Route, Routes, useLocation } from "react-router-dom";
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

const publicPaths = ["/login", "/register", "/logout"];
function AuthBootstrap({ children }) {
  const location = useLocation();

  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);
  const user = useAuthStore((state) => state.user);
  useEffect(() => {
    const isPublicPath = publicPaths.includes(location.pathname);
    if (isPublicPath) {
      useAuthStore.setState({ authInitialized: true });
      return;
    }
    if (user) {
      useAuthStore.setState({ authInitialized: true });
      return;
    }
    loadCurrentUser();
  }, [loadCurrentUser, user, location.pathname]);

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

          {/* PUBLIC DEMO ROUTES - không cần auth */}

          <Route element={<MainLayout />}>
            <Route path="/portfolio/:username" element={<PortfolioPage />} />
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
