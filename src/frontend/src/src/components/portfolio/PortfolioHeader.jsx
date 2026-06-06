import {
  Briefcase,
  Edit3,
  Globe2,
  Link2,
  Mail,
  MapPin,
  ShieldCheck,
  Target,
  UserRound,
} from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";
import { useNavigate } from "react-router-dom";

export default function PortfolioHeader({ portfolio, editable = false }) {
  const navigate = useNavigate();

  const displayName = portfolio?.displayName || "Unnamed User";
  const headline = portfolio?.headline || "Learning builder & developer";
  const repoCount = portfolio?.repositories?.length || 0;

  return (
    <section className="relative overflow-hidden rounded-[1.7rem] border border-emerald-100 bg-gradient-to-br from-[#ecfdf5] via-white to-[#f8fafc] p-4 shadow-sm sm:p-5">
      <DecorativeBlob className="-right-16 -top-20 h-56 w-56 bg-emerald-200/50" />
      <DecorativeBlob className="bottom-0 left-1/2 h-52 w-52 -translate-x-1/2 bg-cyan-100/70" />

      <div className="relative mb-4 flex items-center justify-between gap-3">
        <span className="inline-flex items-center gap-2 rounded-full border border-emerald-100 bg-white/80 px-3 py-1.5 text-xs font-bold uppercase tracking-[0.16em] text-emerald-700">
          <ShieldCheck size={13} />
          Public Portfolio
        </span>

        {editable && (
          <button
            type="button"
            onClick={() => navigate("/settings/profile")}
            className="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-950 px-4 py-2.5 text-sm font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-emerald-700"
          >
            <Edit3 size={15} />
            Edit My Portfolio
          </button>
        )}
      </div>

      <div className="relative grid grid-cols-1 gap-4 lg:grid-cols-[1.05fr_0.95fr]">
        <div className="rounded-[1.4rem] border border-white/90 bg-white/80 p-5 shadow-sm backdrop-blur">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-center">
            <div className="h-28 w-28 shrink-0 overflow-hidden rounded-[1.45rem] border-[5px] border-white bg-emerald-50 shadow-lg shadow-emerald-100/80 sm:h-32 sm:w-32">
              {portfolio?.avatarUrl ? (
                <img src={portfolio.avatarUrl} alt={displayName} className="h-full w-full object-cover" />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-emerald-700">
                  <UserRound size={52} />
                </div>
              )}
            </div>

            <div className="min-w-0 flex-1">
              <h1 className="line-clamp-1 text-3xl font-black tracking-tight text-slate-950 sm:text-4xl">
                {displayName}
              </h1>
              <p className="mt-2 text-base font-semibold text-slate-600">{headline}</p>

              <div className="mt-4 flex flex-wrap items-center gap-2 text-sm font-medium text-slate-600">
                {portfolio?.location && <InfoPill icon={<MapPin size={14} />}>{portfolio.location}</InfoPill>}
                {portfolio?.currentRole && <InfoPill icon={<Briefcase size={14} />}>{portfolio.currentRole}</InfoPill>}
                <InfoPill icon={<ShieldCheck size={14} />}>{repoCount} selected repos</InfoPill>
              </div>

              <div className="mt-4 flex flex-wrap gap-2">
                {portfolio?.publicEmail && <SocialPill href={`mailto:${portfolio.publicEmail}`} icon={<Mail size={15} />} label="Email" />}
                {portfolio?.githubUrl && <SocialPill href={portfolio.githubUrl} icon={<FaGithub size={15} />} label="GitHub" />}
                {portfolio?.linkedinUrl && <SocialPill href={portfolio.linkedinUrl} icon={<FaLinkedin size={15} />} label="LinkedIn" />}
                {portfolio?.personalWebsiteUrl && <SocialPill href={portfolio.personalWebsiteUrl} icon={<Globe2 size={15} />} label="Website" />}
              </div>
            </div>
          </div>
        </div>

        <div className="rounded-[1.4rem] border border-white/90 bg-white/80 p-5 shadow-sm backdrop-blur">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-lg font-black tracking-tight text-slate-950">About Me</h2>
              <p className="mt-1 text-sm text-slate-500">Profile overview and developer direction</p>
            </div>
            <div className="hidden rounded-2xl bg-emerald-50 p-3 text-emerald-700 ring-1 ring-emerald-100 sm:block">
              <Target size={20} />
            </div>
          </div>

          <p className="mt-4 line-clamp-5 whitespace-pre-line text-sm leading-7 text-slate-600">
            {portfolio?.bio || "No profile description has been added yet."}
          </p>

          <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2">
            <MiniInfo label="Current Role" value={portfolio?.currentRole || "Not updated"} />
            <MiniInfo label="Career Goal" value={portfolio?.careerGoal || "Not updated"} />
          </div>
        </div>
      </div>
    </section>
  );
}

function DecorativeBlob({ className }) {
  return <div className={`pointer-events-none absolute rounded-full blur-3xl ${className}`} />;
}

function MiniInfo({ label, value }) {
  return (
    <div className="rounded-2xl border border-slate-100 bg-white/70 p-4">
      <p className="text-[11px] font-black uppercase tracking-[0.16em] text-slate-400">{label}</p>
      <p className="mt-2 line-clamp-2 text-sm font-bold leading-6 text-slate-900">{value}</p>
    </div>
  );
}

function InfoPill({ icon, children }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full bg-white px-3 py-1.5 text-slate-700 shadow-sm ring-1 ring-slate-200">
      <span className="text-emerald-600">{icon}</span>
      {children}
    </span>
  );
}

function SocialPill({ href, icon, label }) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      className="inline-flex h-9 items-center gap-1.5 rounded-full bg-slate-950 px-3 text-sm font-bold text-white transition hover:bg-emerald-700"
    >
      {icon}
      {label}
      <Link2 size={13} />
    </a>
  );
}
