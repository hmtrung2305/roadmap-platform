import { Globe2, Mail, MapPin, UserRound } from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";

export default function PortfolioHeader({ portfolio, username }) {
  const displayName = portfolio?.displayName || username || "Unnamed User";

  const headline =
    portfolio?.headline ||
    [portfolio?.currentRole, portfolio?.careerGoal].filter(Boolean).join(" | ") ||
    "Developer Portfolio";

  const coverImageUrl = portfolio?.coverImageUrl;
  const avatarUrl = portfolio?.avatarUrl;

  return (
    <section className="relative rounded-3xl border border-[#B9D8CC] bg-white shadow-[0_18px_50px_rgba(31,111,95,0.10)]">
      <div className="relative z-0 h-52 overflow-hidden rounded-t-3xl bg-[#1F6F5F] sm:h-60">
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
        <div className="flex items-end gap-4">
          <div className="-mt-14 h-24 w-24 shrink-0 overflow-hidden rounded-2xl border-4 border-white bg-white shadow-[0_16px_34px_rgba(31,111,95,0.22)] ring-1 ring-[#B9D8CC] sm:-mt-16 sm:h-28 sm:w-28">
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

          <div className="pb-1 pt-3">
            <h1 className="text-2xl font-extrabold tracking-tight text-[#18332D] sm:text-3xl">
              {displayName}
            </h1>

            <p className="mt-1 text-sm font-bold text-slate-700">
              {headline}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap gap-2 pb-1">
          {portfolio?.location && (
            <InfoButton
              icon={<MapPin size={14} />}
              label={portfolio.location}
            />
          )}

          {portfolio?.githubUrl && (
            <ExternalButton
              href={portfolio.githubUrl}
              icon={<FaGithub size={14} />}
              label="GitHub"
            />
          )}

          {portfolio?.linkedinUrl && (
            <ExternalButton
              href={portfolio.linkedinUrl}
              icon={<FaLinkedin size={14} />}
              label="LinkedIn"
            />
          )}

          {portfolio?.personalWebsiteUrl && (
            <ExternalButton
              href={portfolio.personalWebsiteUrl}
              icon={<Globe2 size={14} />}
              label="Website"
            />
          )}

          {portfolio?.publicEmail && (
            <ExternalButton
              href={`mailto:${portfolio.publicEmail}`}
              icon={<Mail size={14} />}
              label="Email"
            />
          )}
        </div>
      </div>
    </section>
  );
}

function InfoButton({ icon, label }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-2 text-xs font-extrabold text-[#18332D]">
      {icon}
      {label}
    </span>
  );
}

function ExternalButton({ href, icon, label }) {
  return (
    <a
      href={href}
      target={href.startsWith("mailto:") ? undefined : "_blank"}
      rel="noreferrer"
      className="inline-flex items-center gap-1.5 rounded-xl border border-[#B9D8CC] bg-white px-3 py-2 text-xs font-extrabold text-[#18332D] transition hover:border-[#6FCF97] hover:bg-[#6FCF97]/25 hover:text-[#1F6F5F]"
    >
      {icon}
      {label}
    </a>
  );
}
