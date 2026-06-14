import {
  ExternalLink,
  Globe,
  Mail,
  MapPin,
  Target,
  UserRound,
} from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";

export default function PortfolioLinks({ portfolio }) {
  const hasLinks =
    portfolio.publicEmail ||
    portfolio.githubUrl ||
    portfolio.linkedinUrl ||
    portfolio.personalWebsiteUrl;

  return (
    <aside className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="text-lg font-bold text-slate-900">Profile Summary</h2>

        <div className="mt-5 space-y-3">
          <SummaryItem icon={<UserRound size={17} />} label="Role">
            {portfolio.currentRole || "Not updated"}
          </SummaryItem>

          <SummaryItem icon={<Target size={17} />} label="Goal">
            {portfolio.careerGoal || "Not updated"}
          </SummaryItem>

          <SummaryItem icon={<MapPin size={17} />} label="Location">
            {portfolio.location || "Not updated"}
          </SummaryItem>
        </div>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="text-lg font-bold text-slate-900">Contact & Links</h2>

        <div className="mt-5 space-y-3">
          {portfolio.publicEmail && (
            <LinkItem
              icon={<Mail size={17} />}
              label={portfolio.publicEmail}
              href={`mailto:${portfolio.publicEmail}`}
            />
          )}

          {portfolio.githubUrl && (
            <LinkItem
              icon={<FaGithub size={17} />}
              label="GitHub"
              href={portfolio.githubUrl}
            />
          )}

          {portfolio.linkedinUrl && (
            <LinkItem
              icon={<FaLinkedin size={17} />}
              label="LinkedIn"
              href={portfolio.linkedinUrl}
            />
          )}

          {portfolio.personalWebsiteUrl && (
            <LinkItem
              icon={<Globe size={17} />}
              label="Personal Website"
              href={portfolio.personalWebsiteUrl}
            />
          )}

          {!hasLinks && (
            <p className="text-sm text-slate-500">No public links yet.</p>
          )}
        </div>
      </section>
    </aside>
  );
}

function SummaryItem({ icon, label, children }) {
  return (
    <div className="rounded-lg bg-[#F7F1E8] p-4">
      <div className="flex items-center gap-2 text-sm font-semibold text-slate-500">
        <span className="text-[#1F6F5F]">{icon}</span>
        {label}
      </div>

      <p className="mt-1 text-sm font-semibold text-slate-900">{children}</p>
    </div>
  );
}

function LinkItem({ icon, label, href }) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      className="flex items-center justify-between rounded-lg border border-slate-200 px-4 py-3 text-sm font-medium text-slate-700 hover:border-[#B9D8CC] hover:bg-[#6FCF97]/15 hover:text-[#1F6F5F]"
    >
      <span className="flex min-w-0 items-center gap-2">
        <span className="text-[#1F6F5F]">{icon}</span>
        <span className="truncate">{label}</span>
      </span>

      <ExternalLink size={15} />
    </a>
  );
}