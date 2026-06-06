import { Mail, MapPin, Target, UserRound } from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";

export default function PortfolioFocusCard({ portfolio }) {
  const links = [
    portfolio?.publicEmail && {
      label: portfolio.publicEmail,
      href: `mailto:${portfolio.publicEmail}`,
      icon: <Mail size={15} />,
    },
    portfolio?.githubUrl && {
      label: "GitHub",
      href: portfolio.githubUrl,
      icon: <FaGithub size={15} />,
    },
    portfolio?.linkedinUrl && {
      label: "LinkedIn",
      href: portfolio.linkedinUrl,
      icon: <FaLinkedin size={15} />,
    },
  ].filter(Boolean);

  return (
    <aside className="space-y-4">
      <section className="rounded-2xl border border-[#B9D8CC] bg-[#f1f4ff] p-5 shadow-sm">
        <div className="flex items-center gap-2">
          <Target size={19} className="text-emerald-700" />
          <h2 className="text-xl font-extrabold tracking-tight text-slate-950">Current Focus</h2>
        </div>

        <div className="mt-5 space-y-3">
          <FocusItem
            label="Current Role"
            value={portfolio?.currentRole || "Not updated"}
            icon={<UserRound size={15} />}
          />
          <FocusItem
            label="Career Goal"
            value={portfolio?.careerGoal || "Not updated"}
            icon={<Target size={15} />}
          />
          <FocusItem
            label="Location"
            value={portfolio?.location || "Not updated"}
            icon={<MapPin size={15} />}
          />
        </div>
      </section>

      {links.length > 0 && (
        <section className="rounded-2xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <h2 className="text-xl font-extrabold tracking-tight text-slate-950">Contact</h2>
          <div className="mt-4 space-y-2">
            {links.map((link) => (
              <a
                key={link.label}
                href={link.href}
                target={link.href.startsWith("mailto:") ? undefined : "_blank"}
                rel="noreferrer"
                className="flex items-center gap-2 rounded-xl border border-slate-100 bg-[#F7F1E8] px-3 py-2.5 text-sm font-bold text-slate-700 transition hover:border-emerald-200 hover:bg-emerald-50 hover:text-emerald-700"
              >
                <span className="text-emerald-700">{link.icon}</span>
                <span className="truncate">{link.label}</span>
              </a>
            ))}
          </div>
        </section>
      )}
    </aside>
  );
}

function FocusItem({ label, value, icon }) {
  return (
    <div className="rounded-xl border border-[#B9D8CC] bg-white p-4">
      <p className="flex items-center gap-2 text-xs font-extrabold uppercase tracking-[0.12em] text-emerald-700">
        {icon}
        {label}
      </p>
      <p className="mt-2 text-sm font-bold leading-6 text-slate-900">{value}</p>
    </div>
  );
}
