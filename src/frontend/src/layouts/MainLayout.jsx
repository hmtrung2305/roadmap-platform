import { Outlet, useLocation } from "react-router-dom";
import Footer from "../components/layout/Footer";
import TopNavbar from "../components/layout/TopNavBar";
import StreakAnimation from "../components/streak/StreakAnimation";

function isLearningModuleStudyRoom(pathname) {
  const parts = pathname.split("/").filter(Boolean);

  if (parts[0] !== "learning-modules") {
    return false;
  }

  const slug = parts[1];

  if (!slug || slug === "browse") {
    return false;
  }

  return parts.length === 2 || (parts.length === 3 && parts[2] === "study");
}

export default function MainLayout() {
  const location = useLocation();

  const isStudyRoom =
    location.pathname.startsWith("/study")
    || isLearningModuleStudyRoom(location.pathname);

  const isRoadmapCanvas = /^\/roadmaps\/[^/]+/.test(location.pathname);

  const hideChrome = isStudyRoom;
  const hideFooter = isStudyRoom || isRoadmapCanvas;

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      {!hideChrome && <TopNavbar />}

      <main>
        <Outlet />
      </main>

      {!hideFooter && <Footer />}

      <StreakAnimation />
    </div>
  );
}
