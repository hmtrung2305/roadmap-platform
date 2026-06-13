import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useEffect, useRef } from "react";
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
import EditPortfolioPage from "./pages/EditPortfolioPage";
import { ToastContainer } from "react-toastify";
import VerifyEmailPage from "./pages/VerifyEmailPage";
import SettingsLayout from "./pages/settings/SettingsLayout";
import AccountSettingsPage from "./pages/settings/AccountSettingsPage";
import PrivacySettingsPage from "./pages/settings/PrivacySettingsPage";
import ProfileSettingsPage from "./pages/settings/ProfileSettingsPage";
import PointsSettingsPage from "./pages/settings/PointSettingsPage";
import ProfilePage from "./pages/ProfilePage";
import RoadmapSelectionPage from "./pages/RoadmapSelectionPage";
import RoadmapViewerPage from "./pages/RoadmapViewerPage";
import MarketPulsePage from "./pages/MarketPulsePage";

const publicPaths = ["/", "/login", "/register", "/verify-email", "/logout"];

function isPublicPortfolioPath(pathname) {
  const protectedPortfolioPaths = ["/portfolio/edit", "/portfolio/repositories"];

  if (protectedPortfolioPaths.some((path) => pathname.startsWith(path))) {
    return false;
  }

  return pathname.startsWith("/portfolio/") || pathname.startsWith("/portfolios/");
}

function isPublicPath(pathname) {
  return publicPaths.includes(pathname) || isPublicPortfolioPath(pathname);
}

function AuthBootstrap({ children }) {
  const location = useLocation();
  const bootstrappedRef = useRef(false);

  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);
  const user = useAuthStore((state) => state.user);
  const authInitialized = useAuthStore((state) => state.authInitialized);

  useEffect(() => {
    const pathname = location.pathname;

    if (isPublicPath(pathname)) {
      useAuthStore.setState({ authInitialized: true });
      return;
    }

    if (user && authInitialized) {
      bootstrappedRef.current = true;
      return;
    }

    if (bootstrappedRef.current) {
      return;
    }

    bootstrappedRef.current = true;
    loadCurrentUser();
  }, [authInitialized, loadCurrentUser, location.pathname, user]);

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

          <Route element={<MainLayout />}>
            <Route path="/portfolio/:username" element={<PortfolioPage />} />
            <Route path="/portfolios/:username" element={<PortfolioPage />} />
          </Route>

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
            <Route path="/portfolio/edit" element={<EditPortfolioPage />} />
            <Route
              path="/portfolio/repositories"
              element={<ManagePortfolioRepositoriesPage />}
            />
            <Route path="/resources" element={<ResourceManagementPage />} />
            <Route path="/study/:resourceId" element={<StudyRoomPage />} />

            <Route path="/roadmaps" element={<RoadmapSelectionPage />} />
            <Route path="/roadmaps/:slug" element={<RoadmapViewerPage />} />
            <Route path="/roadmap" element={<Navigate to="/roadmaps" replace />} />
            <Route path="/market-pulse" element={<MarketPulsePage />} />
          </Route>

          <Route
            path="/settings"
            element={
              <ProtectedRoute>
                <SettingsLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Navigate to="/settings/account" replace />} />
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
