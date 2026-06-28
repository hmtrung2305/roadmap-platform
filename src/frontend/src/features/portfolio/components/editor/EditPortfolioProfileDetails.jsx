import { GraduationCap, PencilLine } from "lucide-react";
import { Link } from "react-router-dom";
import { FaGithub, FaLinkedin } from "react-icons/fa";

export default function EditPortfolioProfileDetails({ portfolio, displayName, headline }) {
  const rows = [
    ["Display name", displayName],
    ["Headline", headline],
    ["Current role", portfolio?.currentRole || "Learning role not set"],
    ["Career goal", portfolio?.careerGoal || "Career goal not set"],
    ["Location", portfolio?.location || "Location not set"],
    ["Public email", portfolio?.publicEmail || portfolio?.email || "Email not shown"],
  ];

  return (
    <section className="flex h-full min-h-[620px] flex-col rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-[0_18px_45px_rgba(31,111,95,0.08)] lg:min-h-[640px]">
      <div className="flex items-center justify-between gap-3">
        <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">Profile details</p>
        <GraduationCap className="text-[#1F6F5F]" size={22} />
      </div>

      <div className="mt-5 divide-y divide-[#DCEBE5]">
        {rows.map(([label, value]) => (
          <div key={label} className="py-3">
            <p className="text-[11px] font-bold uppercase tracking-[0.14em] text-[#8BA39B]">{label}</p>
            <p className="portfolio-editor-nowrap mt-1 text-sm font-semibold leading-6 text-[#18332D]">{value}</p>
          </div>
        ))}
      </div>

      <div className="mt-4 grid gap-2 text-sm font-semibold text-[#34544C]">
        {portfolio?.githubUrl && (
          <a href={portfolio.githubUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 rounded-lg bg-[#F7F1E8] px-3 py-2 hover:text-[#1F6F5F]">
            <FaGithub /> GitHub profile
          </a>
        )}
        {portfolio?.linkedinUrl && (
          <a href={portfolio.linkedinUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 rounded-lg bg-[#F7F1E8] px-3 py-2 hover:text-[#1F6F5F]">
            <FaLinkedin /> LinkedIn profile
          </a>
        )}
      </div>

      <Link to="/settings/profile" className="mt-auto inline-flex w-full items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-bold text-[#1F6F5F] transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/20">
        <PencilLine size={16} />
        Edit profile fields
      </Link>
    </section>
  );
}
