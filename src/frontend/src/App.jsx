import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useEffect, useRef } from "react";
import { AnimatePresence } from "framer-motion";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import LandingPage from "./pages/LandingPage";
import PortfolioPage from "./pages/PortfolioPage";
import MainLayout from "./layouts/MainLayout";
import AdminLayout from "./layouts/AdminLayout";
import CounselorLayout from "./layouts/CounselorLayout";
import ProtectedRoute from "./routes/ProtectedRoute";
import RequirePermission from "./routes/RequirePermission";
import PublicRoute from "./routes/PublicRoute";
import { useAuthStore } from "./stores/useAuthStore";
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
import SkillGapAnalysisPage from "./pages/SkillGapAnalysisPage";
import LearningModulesPage from "./pages/learning/LearningModulesPage";
import BrowseLearningModulesPage from "./pages/learning/BrowseLearningModulesPage";
import LearningModuleOverviewPage from "./pages/learning/LearningModuleOverviewPage";
import CounselorLearningModulesPage from "./pages/counselor/learningModules/CounselorLearningModulesPage";
import CounselorLearningModuleCreatePage from "./pages/counselor/learningModules/CounselorLearningModuleCreatePage";
import CounselorLearningModuleEditorPage from "./pages/counselor/learningModules/CounselorLearningModuleEditorPage";
import CounselorLearningModulePreviewPage from "./pages/counselor/learningModules/CounselorLearningModulePreviewPage";
import CounselorSettingsPage from "./pages/counselor/CounselorSettingsPage";
import AdminHomePage from "./pages/admin/AdminHomePage";
import NotFoundPage from "./pages/NotFoundPage";
import {
  ADMIN_SURFACE_PERMISSIONS,
  COUNSELOR_SURFACE_PERMISSIONS,
  LEARNER_SURFACE_PERMISSIONS,
} from "./constants/permissions";

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
              <RequirePermission anyPermissions={LEARNER_SURFACE_PERMISSIONS}>
                <MainLayout />
              </RequirePermission>
            }
          >
            <Route path="/home" element={<Navigate to="/roadmaps" replace />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route path="/portfolio/edit" element={<EditPortfolioPage />} />
            <Route
              path="/portfolio/repositories"
              element={<ManagePortfolioRepositoriesPage />}
            />
            <Route path="/resources" element={<Navigate to="/learning-modules" replace />} />
            <Route path="/study/:resourceId" element={<Navigate to="/learning-modules" replace />} />

            <Route path="/learning-modules" element={<LearningModulesPage />} />
            <Route path="/learning-modules/browse" element={<BrowseLearningModulesPage />} />
            <Route path="/learning-modules/:slug/overview" element={<LearningModuleOverviewPage />} />
            <Route path="/learning-modules/:slug/study" element={<StudyRoomPage />} />
            <Route path="/learning-modules/:slug" element={<StudyRoomPage />} />

            <Route path="/roadmaps" element={<RoadmapSelectionPage />} />
            <Route path="/roadmaps/:slug" element={<RoadmapViewerPage />} />
            <Route path="/roadmap" element={<Navigate to="/roadmaps" replace />} />
            <Route path="/market-pulse" element={<MarketPulsePage />} />
            <Route path="/skill-gap" element={<SkillGapAnalysisPage />} />
            <Route path="/skill-gap-analysis" element={<SkillGapAnalysisPage />} />
          </Route>

          <Route
            element={
              <RequirePermission anyPermissions={COUNSELOR_SURFACE_PERMISSIONS}>
                <CounselorLayout />
              </RequirePermission>
            }
          >
            <Route path="/counselor" element={<Navigate to="/counselor/learning-modules" replace />} />
            <Route path="/counselor/learning-modules" element={<CounselorLearningModulesPage />} />
            <Route path="/counselor/learning-modules/create" element={<CounselorLearningModuleCreatePage />} />
            <Route path="/counselor/learning-modules/:moduleSlug/edit" element={<CounselorLearningModuleEditorPage />} />
            <Route path="/counselor/learning-modules/:moduleSlug/preview" element={<CounselorLearningModulePreviewPage />} />
            <Route path="/counselor/settings" element={<CounselorSettingsPage />} />
          </Route>

          <Route
            element={
              <RequirePermission anyPermissions={ADMIN_SURFACE_PERMISSIONS}>
                <AdminLayout />
              </RequirePermission>
            }
          >
            <Route path="/admin" element={<AdminHomePage />} />
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

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </AnimatePresence>
    </AuthBootstrap>
  );
}
