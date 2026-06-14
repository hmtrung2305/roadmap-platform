import { Outlet, useLocation } from "react-router-dom";
import Footer from "../components/layout/Footer";
import TopNavbar from "../components/layout/TopNavBar";
import StreakAnimation from "../components/streak/StreakAnimation";

export default function MainLayout() {
  const location = useLocation();

  const isStudyRoom = location.pathname.startsWith("/study");
  const isRoadmapCanvas = /^\/roadmaps\/[^/]+/.test(location.pathname);

  const hideChrome = isStudyRoom;
  const isRoadmapSelection = location.pathname === "/roadmaps" || location.pathname === "/roadmap";
  const hideFooter = isStudyRoom || isRoadmapCanvas || isRoadmapSelection;

  return (
    <div className="tm-page min-h-screen text-[#18332D]">
      {!hideChrome && <TopNavbar />}

      <main className={isRoadmapCanvas ? "" : "tm-page-animate"}>
        <Outlet />
      </main>

      {!hideFooter && <Footer />}

      <StreakAnimation />
    </div>
  );
}
