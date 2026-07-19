import { ArrowUpRight, Link2, Target } from "lucide-react";
import { useState } from "react";
import { capitalize, formatDecimal, formatNumber } from "../marketPulseViewModel";

export default function LearningSignalsSection({ recommendations, pairs, onExploreSkill }) {
  const [expanded, setExpanded] = useState(false);
  const visibleRecommendations = expanded ? recommendations : recommendations.slice(0, 3);
  const visiblePairs = expanded ? pairs : pairs.slice(0, 5);

  return (
    <section aria-labelledby="learning-signals-title" className="mt-8">
      <div>
        <h2 id="learning-signals-title" className="text-xl font-extrabold text-[#18332D]">Learning signals</h2>
        <p className="mt-1 text-sm text-slate-600">Turn market movement into a clearer learning priority.</p>
      </div>
      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        <article className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
          <div className="flex items-center gap-2">
            <Target size={18} className="text-[#1F6F5F]" aria-hidden="true" />
            <h3 className="text-base font-extrabold text-[#18332D]">Recommended focus</h3>
          </div>
          {visibleRecommendations.length === 0 ? (
            <Empty message="No learning recommendation is available yet." />
          ) : (
            <div className="mt-3 divide-y divide-[#DCEBE5]">
              {visibleRecommendations.map((item) => (
                <div key={`${item.title}-${item.skillSlug || "general"}`} className="py-4 first:pt-2">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-sm font-extrabold text-[#18332D]">{item.title}</div>
                      <p className="mt-1 text-xs font-semibold leading-5 text-slate-500">{item.detail}</p>
                    </div>
                    <span className="shrink-0 rounded-full bg-[#EAF8F1] px-2 py-1 text-[11px] font-extrabold text-[#1F6F5F]">
                      {capitalize(item.priority || "medium")}
                    </span>
                  </div>
                  {item.skillSlug && (
                    <button
                      type="button"
                      onClick={() => onExploreSkill(item.skillSlug)}
                      className="mt-3 inline-flex min-h-11 items-center gap-1.5 rounded-xl px-3 text-xs font-extrabold text-[#1F6F5F] hover:bg-[#EAF8F1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
                    >
                      {item.actionLabel || "Explore trend"}
                      <ArrowUpRight size={14} aria-hidden="true" />
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </article>

        <article className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
          <div className="flex items-center gap-2">
            <Link2 size={18} className="text-[#1F6F5F]" aria-hidden="true" />
            <h3 className="text-base font-extrabold text-[#18332D]">Common skill combinations</h3>
          </div>
          {visiblePairs.length === 0 ? (
            <Empty message="No skill-pair signal is available yet." />
          ) : (
            <div className="mt-3 divide-y divide-[#DCEBE5]">
              {visiblePairs.map((pair) => (
                <div key={`${pair.skillASlug}-${pair.skillBSlug}`} className="flex items-center justify-between gap-3 py-3 first:pt-2">
                  <div className="min-w-0 text-sm font-extrabold text-[#18332D]">
                    <span>{pair.skillA}</span><span className="mx-1 text-[#2FA084]">+</span><span>{pair.skillB}</span>
                  </div>
                  <div className="shrink-0 text-right text-xs font-bold text-slate-500">
                    {formatNumber(pair.postingCount)} jobs<br />{formatDecimal(pair.percentOfSample)}%
                  </div>
                </div>
              ))}
            </div>
          )}
        </article>
      </div>

      {(recommendations.length > 3 || pairs.length > 5) && (
        <button
          type="button"
          aria-expanded={expanded}
          onClick={() => setExpanded((current) => !current)}
          className="mt-3 min-h-11 rounded-xl px-3 text-sm font-extrabold text-[#1F6F5F] hover:bg-[#EAF8F1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
        >
          {expanded ? "Show less" : "Show more learning signals"}
        </button>
      )}
    </section>
  );
}

function Empty({ message }) {
  return <div className="mt-3 rounded-xl border border-dashed border-[#B9D8CC] bg-[#FCFAF6] px-4 py-7 text-center text-sm font-semibold text-slate-500">{message}</div>;
}
