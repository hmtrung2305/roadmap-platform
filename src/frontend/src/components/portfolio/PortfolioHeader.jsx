import { Briefcase, Link2, MapPin, UserRound } from "lucide-react";
import { FaGithub } from "react-icons/fa";

export default function PortfolioHeader({ portfolio }) {
  const displayName = portfolio.displayName || "Unnamed User";
  const headline = portfolio.headline || "No headline yet";

  return (
    <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className="relative h-52 bg-linear-to-r from-blue-800 via-slate-800 to-slate-950">
        {portfolio.coverImageUrl && (
          <img
            src={portfolio.coverImageUrl}
            alt="Portfolio cover"
            className="h-full w-full object-cover"
          />
        )}

        <div className="absolute inset-0 bg-linear-to-t from-slate-950/70 via-slate-950/20 to-transparent" />
      </div>

      <div className="relative px-8 pb-8">
        <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
          <div className="flex flex-col gap-5 md:flex-row md:items-start">
            <div className="-mt-20 h-32 w-32 shrink-0 overflow-hidden rounded-full border-4 border-white bg-blue-100 shadow-md">
              {portfolio.avatarUrl ? (
                <img
                  src={portfolio.avatarUrl}
                  alt={displayName}
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-blue-700">
                  <UserRound size={50} />
                </div>
              )}
            </div>

            <div className="pt-5">
              <h2 className="text-3xl font-bold text-slate-900">
                {displayName}
              </h2>

              <p className="mt-1 text-lg font-bold text-blue-700">
                {headline}
              </p>

              <div className="mt-4 flex flex-wrap items-center gap-3 text-sm font-medium text-slate-600">
                {portfolio.location && (
                  <InfoItem icon={<MapPin size={15} />}>
                    {portfolio.location}
                  </InfoItem>
                )}

                {portfolio.currentRole && (
                  <InfoItem icon={<Briefcase size={15} />}>
                    {portfolio.currentRole}
                  </InfoItem>
                )}

                {portfolio.githubUrl && (
                  <a
                    href={portfolio.githubUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1 text-slate-700 hover:bg-blue-50 hover:text-blue-700"
                  >
                    <FaGithub size={15} />
                    GitHub
                  </a>
                )}

                {portfolio.personalWebsiteUrl && (
                  <a
                    href={portfolio.personalWebsiteUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1 text-slate-700 hover:bg-blue-50 hover:text-blue-700"
                  >
                    <Link2 size={15} />
                    Website
                  </a>
                )}
              </div>
            </div>
          </div>

          {(portfolio.currentRole || portfolio.careerGoal) && (
            <div className="mt-5 rounded-2xl border border-blue-100 bg-blue-50 px-5 py-4 md:min-w-56">
              <p className="text-xs font-bold uppercase tracking-wide text-blue-700">
                Career Direction
              </p>
              <p className="mt-1 text-sm font-semibold text-slate-900">
                {portfolio.careerGoal || portfolio.currentRole}
              </p>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function InfoItem({ icon, children }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1 text-slate-700">
      {icon}
      {children}
    </span>
  );
}