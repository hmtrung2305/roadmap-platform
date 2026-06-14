import { Globe2, Mail, MapPin, PencilLine, UserRound } from "lucide-react";
import { Link } from "react-router-dom";
import { FaGithub, FaLinkedin } from "react-icons/fa";

export default function PortfolioHeader({ portfolio, username, isOwnPortfolio = false }) {
  const displayName = portfolio?.displayName || username || "Unnamed User";

  const headline =
    portfolio?.headline ||
    [portfolio?.currentRole, portfolio?.careerGoal].filter(Boolean).join(" | ") ||
    "Developer Portfolio";

  const coverImageUrl = portfolio?.coverImageUrl;
  const avatarUrl = portfolio?.avatarUrl;
  const hasContact =
    portfolio?.location ||
    portfolio?.githubUrl ||
    portfolio?.linkedinUrl ||
    portfolio?.personalWebsiteUrl ||
    portfolio?.publicEmail;

  return (
    <section className="relative rounded-lg border border-[#B9D8CC] bg-white shadow-[0_18px_50px_rgba(31,111,95,0.10)]">
      <div className="relative z-0 h-52 overflow-hidden rounded-t-lg bg-[#1F6F5F] sm:h-60">
        {coverImageUrl ? (
          <img
            src={coverImageUrl}
            alt={`${displayName} cover`}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="grid h-full grid-rows-3">
            <div className="bg-[#1F6F5F]" />
            <div className="bg-[#2FA084]" />
            <div className="bg-[#6FCF97]" />
          </div>
        )}
      </div>

      <div className="relative z-10 flex flex-col gap-4 px-5 pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div className="flex min-w-0 items-end gap-4">
          <div className="-mt-20 h-24 w-24 shrink-0 overflow-hidden rounded-lg border-4 border-white bg-white shadow-[0_16px_34px_rgba(31,111,95,0.22)] ring-1 ring-[#B9D8CC] sm:-mt-24 sm:h-28 sm:w-28">
            {avatarUrl ? (
              <img
                src={avatarUrl}
                alt={displayName}
                className="h-full w-full object-cover"
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center bg-[#EEEEEE] text-[#1F6F5F]">
                <UserRound size={42} />
              </div>
            )}
          </div>

          <div className="min-w-0 pb-1 pt-3">
            {isOwnPortfolio && (
              <div className="mb-2 flex flex-wrap gap-1.5">
                <span className="rounded-lg border border-[#B9D8CC] bg-[#EAF8F1] px-2 py-0.5 text-[8px] font-extrabold uppercase tracking-[0.14em] text-[#1F6F5F]">
                  Private preview
                </span>
              </div>
            )}

            <h1 className="truncate text-2xl font-extrabold tracking-tight text-[#18332D] sm:text-3xl">
              {displayName}
            </h1>

            <p className="mt-1 truncate text-sm font-bold text-slate-700">
              {headline}
            </p>
          </div>
        </div>

        {(hasContact || isOwnPortfolio) && (
          <div className="flex shrink-0 flex-col items-start gap-2 pb-1 sm:items-end">
            {isOwnPortfolio && (
              <Link
                to="/portfolio/edit"
                className="inline-flex shrink-0 items-center justify-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-[#EAF8F1] px-4 py-2 text-xs font-extrabold text-[#1F6F5F] no-underline shadow-sm transition-colors hover:border-[#1F6F5F] hover:bg-[#18332D] hover:text-white"
              >
                <PencilLine size={14} />
                Edit portfolio
              </Link>
            )}

            {hasContact && (
              <div className="flex max-w-full flex-wrap items-center gap-2 sm:justify-end">
                {portfolio?.location && (
                  <InfoPill icon={<MapPin size={14} />} label={portfolio.location} />
                )}

                {portfolio?.githubUrl && (
                  <ExternalPill href={portfolio.githubUrl} icon={<FaGithub size={14} />} label="GitHub" />
                )}

                {portfolio?.linkedinUrl && (
                  <ExternalPill href={portfolio.linkedinUrl} icon={<FaLinkedin size={14} />} label="LinkedIn" />
                )}

                {portfolio?.personalWebsiteUrl && (
                  <ExternalPill href={portfolio.personalWebsiteUrl} icon={<Globe2 size={14} />} label="Website" />
                )}

                {portfolio?.publicEmail && (
                  <ExternalPill href={`mailto:${portfolio.publicEmail}`} icon={<Mail size={14} />} label="Email" />
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </section>
  );
}

function InfoPill({ icon, label }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-2 text-xs font-extrabold text-[#18332D]">
      {icon}
      {label}
    </span>
  );
}

function ExternalPill({ href, icon, label }) {
  return (
    <a
      href={href}
      target={href.startsWith("mailto:") ? undefined : "_blank"}
      rel="noreferrer"
      className="inline-flex items-center gap-1.5 rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-xs font-extrabold text-[#18332D] transition hover:border-[#6FCF97] hover:bg-[#6FCF97]/25 hover:text-[#1F6F5F]"
    >
      {icon}
      {label}
    </a>
  );
}
