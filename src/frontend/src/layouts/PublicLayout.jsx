import { Outlet } from "react-router-dom";
import Footer from "../components/layout/Footer";
import TopNavbar from "../components/layout/TopNavBar";

export default function PublicLayout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      {/* <TopNavbar /> */}

      <main>
        <Outlet />
      </main>

      <Footer />
    </div>
  );
}