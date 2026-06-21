import { Outlet } from "react-router-dom";
import Footer from "../components/layout/Footer";

export default function PublicLayout() {
  return (
    <div className="tm-page min-h-screen text-[#18332D]">
      {/* <TopNavbar /> */}

      <main>
        <Outlet />
      </main>

      <Footer />
    </div>
  );
}