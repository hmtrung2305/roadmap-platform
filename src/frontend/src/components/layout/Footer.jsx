import { useNavigate } from "react-router-dom";
import AuthLogo from "../auth/AuthLogo";

export default function Footer() {
  const navigate = useNavigate();

  return (
    <footer className="border-t border-[#B9D8CC] bg-white/85 backdrop-blur-xl">
      <div className="mx-auto grid max-w-7xl gap-8 px-3 py-3 text-sm sm:px-6 md:grid-cols-[1.2fr_1fr_1fr_1fr] lg:px-8">
        <div>
          <button type="button" onClick={() => navigate("/dashboard")}>
            <AuthLogo compact showTagline={false} />
          </button>
          <p className="mt-4 max-w-sm leading-6 text-slate-600">
            Build a role-based learning path, track repositories, and publish a portfolio that proves your work.
          </p>
        </div>

        <FooterGroup
          title="Resources"
          items={[
            ["Roadmaps", "/roadmap"],
            ["Learning resources", "/resources"],
            ["AI Mentor", "/resources"],
          ]}
          navigate={navigate}
        />

        <FooterGroup
          title="Platform"
          items={[
            ["Dashboard", "/dashboard"],
            ["Public Portfolio", "/portfolio"],
            ["Market Pulse", "/market-pulse"],
          ]}
          navigate={navigate}
        />

        <FooterGroup
          title="Support"
          items={[
            ["Settings", "/settings"],
            ["Profile", "/profile"],
            ["Terms of Service", ""],
          ]}
          navigate={navigate}
        />
      </div>

      <div className="border-t border-[#B9D8CC] py-2 text-center text-xs font-semibold text-slate-500">
        © 2026 TechMap. All rights reserved.
      </div>
    </footer>
  );
}

function FooterGroup({ title, items, navigate }) {
  return (
    <div>
      <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
        {title}
      </p>
      <div className="mt-3 space-y-2">
        {items.map(([label, path]) => (
          <button
            key={label}
            type="button"
            onClick={() => path && navigate(path)}
            className="block font-semibold text-slate-600 transition hover:text-[#1F6F5F]"
          >
            {label}
          </button>
        ))}
      </div>
    </div>
  );
}
