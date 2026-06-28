import { Sparkles } from "lucide-react";

export default function PortfolioSkillGroups({ skillGroups = [] }) {
  const normalizedGroups = skillGroups
    .map((group) => ({
      name: group.groupName || group.name || group.skillGroupName || "Skills",
      skills: group.skills || group.items || [],
    }))
    .filter((group) => group.skills.length > 0);

  if (normalizedGroups.length === 0) return null;

  return (
    <section>
      <div className="mb-4 flex items-center gap-3">
        <span className="grid size-10 place-items-center rounded-lg bg-[#6FCF97]/18 text-[#1F6F5F] ring-1 ring-[#B9D8CC]">
          <Sparkles size={19} aria-hidden="true" />
        </span>
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          Detected Skills
        </p>
      </div>

      <div className="rounded-2xl border border-[#B9D8CC]/75 bg-white p-5 shadow-[0_8px_18px_rgba(31,111,95,0.05)] transition duration-200 hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md">
        <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
          {normalizedGroups.map((group) => (
            <div key={group.name}>
              <h3 className="text-sm font-extrabold text-[#18332D]">{group.name}</h3>
              <div className="mt-3 flex flex-wrap gap-2">
                {group.skills.map((skill) => {
                  const label =
                    typeof skill === "string"
                      ? skill
                      : skill.skillName || skill.name || skill.title || "Skill";

                  return (
                    <span
                      key={label}
                      className="inline-flex items-center gap-1.5 rounded-full bg-[#EEEEEE] px-3 py-1 text-xs font-bold text-[#18332D] ring-1 ring-[#B9D8CC]"
                    >
                      <span className="size-1.5 rounded-full bg-[#2FA084]" />
                      {label}
                    </span>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
