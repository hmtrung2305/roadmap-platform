import { Outlet } from "react-router-dom";
import TopNavbar from "../../components/layout/TopNavBar";
import Footer from "../../components/layout/Footer";
import SettingsSidebar from "../../features/settings/components/SettingsSidebar";

export default function SettingsLayout() {
  return (
    <div className="tm-page min-h-screen text-[#18332D]">
      <TopNavbar />

      <main className="tm-page-animate min-h-[calc(100vh-4rem)]">
        <div className="tm-soft-enter mx-auto flex max-w-7xl gap-6 px-6 py-8">
          <SettingsSidebar />

          <section className="min-w-0 flex-1">
            <Outlet />
          </section>
        </div>
      </main>

      <Footer />
    </div>
  );
}
