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
      <p className="mb-4 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
        Detected Skills
      </p>

      <div className="rounded-2xl border border-[#B9D8CC] bg-white p-5 shadow-[0_14px_34px_rgba(31,111,95,0.08)]">
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
                      className="rounded-full bg-[#EEEEEE] px-3 py-1 text-xs font-bold text-[#18332D] ring-1 ring-[#B9D8CC]"
                    >
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
