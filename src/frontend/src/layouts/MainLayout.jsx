import { Outlet, useLocation } from "react-router-dom";
import Footer from "../components/layout/Footer";
import TopNavbar from "../components/layout/TopNavBar";
import StreakAnimation from "../components/streak/StreakAnimation";

export default function MainLayout() {
  const location = useLocation();

  const isStudyRoom = location.pathname.startsWith("/study");
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
