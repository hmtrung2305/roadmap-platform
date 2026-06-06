import { Outlet, useLocation } from "react-router-dom";
import Footer from "../components/layout/Footer";
import TopNavbar from "../components/layout/TopNavBar";
import StreakAnimation from "../components/streak/StreakAnimation";

export default function MainLayout() {
  const location = useLocation();

  const isStudyRoom = location.pathname.startsWith("/study");

  return (
    <div className="min-h-screen bg-transparent text-[#18332D]">
      {!isStudyRoom && <TopNavbar />}

      <main>
        <Outlet />
      </main>

      {!isStudyRoom && <Footer />}
      <StreakAnimation />
    </div>
  );
}
