import { Outlet } from "react-router-dom";
import TopNavbar from "../../components/layout/TopNavBar";
import Footer from "../../components/layout/Footer";
import SettingsSidebar from "../../components/settings/SettingsSidebar";

export default function SettingsLayout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <TopNavbar />

      <main className="min-h-[calc(100vh-4rem)]">
        <div className="mx-auto flex max-w-7xl gap-6 px-6 py-8">
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