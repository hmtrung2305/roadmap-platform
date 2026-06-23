import { Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { lazy, Suspense, useEffect, useRef } from "react";
import { AnimatePresence } from "framer-motion";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import LandingPage from "./pages/LandingPage";
import PortfolioPage from "./pages/PortfolioPage";
import MainLayout from "./layouts/MainLayout";
import AdminLayout from "./layouts/AdminLayout";
import ContentManagerLayout from "./layouts/ContentManagerLayout";
import RequirePermission from "./routes/RequirePermission";
import PublicRoute from "./routes/PublicRoute";
import { useAuthStore } from "./stores/useAuthStore";
import { subscribeToUnauthorizedEvent } from "./utils/authEventUtils";
import StudyRoomPage from "./pages/StudyRoomPage";
import EditPortfolioPage from "./pages/EditPortfolioPage";
import { ToastContainer } from "react-toastify";
import VerifyEmailPage from "./pages/VerifyEmailPage";
import SettingsLayout from "./pages/settings/SettingsLayout";
import AccountSettingsPage from "./pages/settings/AccountSettingsPage";
import PrivacySettingsPage from "./pages/settings/PrivacySettingsPage";
import ProfileSettingsPage from "./pages/settings/ProfileSettingsPage";
import PointsSettingsPage from "./pages/settings/PointSettingsPage";
import RoadmapSelectionPage from "./pages/RoadmapSelectionPage";
import RoadmapViewerPage from "./pages/RoadmapViewerPage";
import MarketPulsePage from "./pages/MarketPulsePage";
import SkillGapAnalysisPage from "./pages/SkillGapAnalysisPage";
import LearningModulesPage from "./pages/learning/LearningModulesPage";
import BrowseLearningModulesPage from "./pages/learning/BrowseLearningModulesPage";
import LearningModuleOverviewPage from "./pages/learning/LearningModuleOverviewPage";
import ContentManagerOverviewPage from "./pages/content/ContentManagerOverviewPage";
import ContentManagerLearningModulesPage from "./pages/content/learningModules/ContentManagerLearningModulesPage";
import ContentManagerLearningModuleCreatePage from "./pages/content/learningModules/ContentManagerLearningModuleCreatePage";
import ContentManagerLearningModuleEditorPage from "./pages/content/learningModules/ContentManagerLearningModuleEditorPage";
import ContentManagerLearningModulePreviewPage from "./pages/content/learningModules/ContentManagerLearningModulePreviewPage";
import ContentManagerSettingsPage from "./pages/content/ContentManagerSettingsPage";
import AdminHomePage from "./pages/admin/AdminHomePage";
import AdminSettingsPage from "./pages/admin/AdminSettingsPage";
import NotFoundPage from "./pages/NotFoundPage";
import {
  ADMIN_SURFACE_PERMISSIONS,
  CONTENT_MANAGER_SURFACE_PERMISSIONS,
  LEARNER_SURFACE_PERMISSIONS,
} from "./constants/permissions";

const ContentManagerRoadmapsPage = lazy(() => import("./pages/content/roadmaps/ContentManagerRoadmapsPage"));
const ContentManagerRoadmapEditorPage = lazy(() => import("./pages/content/roadmaps/ContentManagerRoadmapEditorPage"));

function ContentRouteLoadingState() {
  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8] px-4 py-7 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-[1520px] rounded-xl border border-[#B9D8CC]/80 bg-white/95 p-8 text-center text-sm font-bold text-slate-600 shadow-sm">
        Loading content workspace...
      </div>
    </main>
  );
}

function LazyContentPage({ children }) {
  return <Suspense fallback={<ContentRouteLoadingState />}>{children}</Suspense>;
}


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

function shouldLoadSessionOnPublicPath(pathname) {
  return publicPaths.includes(pathname) && localStorage.getItem("isLoggedIn") === "true";
}

function AuthBootstrap({ children }) {
  const location = useLocation();
  const navigate = useNavigate();
  const bootstrappedRef = useRef(false);

  const loadCurrentUser = useAuthStore((state) => state.loadCurrentUser);
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const user = useAuthStore((state) => state.user);
  const authInitialized = useAuthStore((state) => state.authInitialized);

  useEffect(() => {
    return subscribeToUnauthorizedEvent(() => {
      const pathname = window.location.pathname;

      clearAuth();

      if (!isPublicPath(pathname)) {
        navigate("/login", { replace: true });
      }
    });
  }, [clearAuth, navigate]);

  useEffect(() => {
    const pathname = location.pathname;

    if (isPublicPath(pathname)) {
      if (shouldLoadSessionOnPublicPath(pathname) && !user) {
        loadCurrentUser();
        return;
      }

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
        autoClose={3200}
        hideProgressBar={false}
        newestOnTop
        closeOnClick
        pauseOnHover
        draggable
        limit={4}
        theme="light"
        toastClassName="tm-toast"
        bodyClassName="tm-toast-body"
        progressClassName="tm-toast-progress"
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

            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route path="/portfolio/edit" element={<EditPortfolioPage />} />
            <Route path="/portfolio/repositories" element={<Navigate to="/portfolio/edit" replace />} />

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
              <RequirePermission anyPermissions={CONTENT_MANAGER_SURFACE_PERMISSIONS}>
                <ContentManagerLayout />
              </RequirePermission>
            }
          >
            <Route path="/content" element={<Navigate to="/content/overview" replace />} />
            <Route path="/content/overview" element={<ContentManagerOverviewPage />} />
            <Route path="/content/learning-modules" element={<ContentManagerLearningModulesPage />} />
            <Route path="/content/learning-modules/create" element={<ContentManagerLearningModuleCreatePage />} />
            <Route path="/content/learning-modules/:moduleId/edit" element={<ContentManagerLearningModuleEditorPage />} />
            <Route path="/content/learning-modules/:moduleId/preview" element={<ContentManagerLearningModulePreviewPage />} />
            <Route path="/content/roadmaps" element={<LazyContentPage><ContentManagerRoadmapsPage /></LazyContentPage>} />
            <Route path="/content/roadmaps/:roadmapId/edit" element={<LazyContentPage><ContentManagerRoadmapEditorPage /></LazyContentPage>} />
            <Route path="/content/settings" element={<ContentManagerSettingsPage />} />
          </Route>

          <Route
            element={
              <RequirePermission anyPermissions={ADMIN_SURFACE_PERMISSIONS}>
                <AdminLayout />
              </RequirePermission>
            }
          >
            <Route path="/admin" element={<AdminHomePage />} />
            <Route path="/admin/settings" element={<AdminSettingsPage />} />
          </Route>

          <Route
            path="/settings"
            element={
              <RequirePermission anyPermissions={LEARNER_SURFACE_PERMISSIONS}>
                <SettingsLayout />
              </RequirePermission>
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
