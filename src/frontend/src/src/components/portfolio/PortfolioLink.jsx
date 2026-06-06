import { Briefcase, ExternalLink, Globe, Mail, MapPin, Target } from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";

export default function PortfolioLinks({ portfolio }) {
  const hasLinks =
    portfolio?.publicEmail ||
    portfolio?.githubUrl ||
    portfolio?.linkedinUrl ||
    portfolio?.personalWebsiteUrl;

  return (
    <aside className="space-y-5 xl:sticky xl:top-24 xl:self-start">
      <section className="rounded-[2rem] border border-emerald-100 bg-emerald-50/70 p-6 shadow-sm">
        <h2 className="text-xl font-black tracking-tight text-slate-950">Current Focus</h2>

        <div className="mt-5 space-y-3">
          <FocusItem icon={<Briefcase size={17} />} label="Current role" value={portfolio?.currentRole || "Not updated"} />
          <FocusItem icon={<Target size={17} />} label="Career goal" value={portfolio?.careerGoal || "Not updated"} />
          <FocusItem icon={<MapPin size={17} />} label="Location" value={portfolio?.location || "Not updated"} />
        </div>
      </section>

      <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="text-xl font-black tracking-tight text-slate-950">Contact & Links</h2>

        <div className="mt-5 space-y-3">
          {portfolio?.publicEmail && <LinkItem icon={<Mail size={17} />} label={portfolio.publicEmail} href={`mailto:${portfolio.publicEmail}`} />}
          {portfolio?.githubUrl && <LinkItem icon={<FaGithub size={17} />} label="GitHub" href={portfolio.githubUrl} />}
          {portfolio?.linkedinUrl && <LinkItem icon={<FaLinkedin size={17} />} label="LinkedIn" href={portfolio.linkedinUrl} />}
          {portfolio?.personalWebsiteUrl && <LinkItem icon={<Globe size={17} />} label="Personal Website" href={portfolio.personalWebsiteUrl} />}

          {!hasLinks && (
            <p className="rounded-2xl bg-slate-50 p-4 text-sm font-medium text-slate-500">
              No public contact links have been added yet.
            </p>
          )}
        </div>
      </section>
    </aside>
  );
}

function FocusItem({ icon, label, value }) {
  return (
    <div className="rounded-3xl border border-white bg-white p-4 shadow-sm">
      <div className="flex items-center gap-2 text-xs font-black uppercase tracking-[0.16em] text-emerald-600">
        {icon}
        {label}
      </div>
      <p className="mt-2 text-sm font-bold leading-6 text-slate-900">{value}</p>
    </div>
  );
}

function LinkItem({ icon, label, href }) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      className="flex items-center justify-between rounded-2xl border border-slate-200 px-4 py-3 text-sm font-bold text-slate-700 transition hover:border-emerald-200 hover:bg-emerald-50 hover:text-emerald-700"
    >
      <span className="flex min-w-0 items-center gap-2">
        <span className="text-emerald-700">{icon}</span>
        <span className="truncate">{label}</span>
      </span>
      <ExternalLink size={15} />
    </a>
  );
}
