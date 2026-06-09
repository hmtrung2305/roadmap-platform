export default function RoadmapLegend({ isOpen, onToggle }) {
  return (
    <div className="pointer-events-auto absolute left-4 top-4 z-20">
      <button
        type="button"
        onClick={onToggle}
        className="rounded-lg border border-[#B9D8CC] bg-white px-4 py-2 text-xs font-extrabold tracking-tight text-[#18332D] shadow-sm transition-colors hover:bg-[#F3FBEF]"
      >
        {isOpen ? "Hide legend" : "Show legend"}
      </button>

      {isOpen && (
        <div className="mt-3 w-[320px] rounded-lg border border-[#B9D8CC] bg-white p-4 text-[#18332D] shadow-lg">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h3 className="text-sm font-extrabold tracking-tight text-[#18332D]">Roadmap legend</h3>
              <p className="mt-1 text-xs font-semibold leading-5 text-slate-600">
                Compact learning palette with stable phase color and original edge colors.
              </p>
            </div>

            <button
              type="button"
              onClick={onToggle}
              className="rounded-lg border border-[#B9D8CC] bg-[#F3FBEF] px-2 py-1 text-[10px] font-extrabold text-[#03A791]"
            >
              Close
            </button>
          </div>

          <LegendSection title="Node groups">
            <LegendItem color="#03A791" label="Phase" description="Main roadmap section." />
            <LegendItem color="#FFE08A" label="Topic / option" description="Core learning item." />
            <LegendItem color="#A8EFCB" label="Choice group" description="Grouped options." />
            <LegendItem color="#F1BA88" label="Checkpoint / project" description="Review or hands-on work." />
          </LegendSection>

          <LegendSection title="Progress state">
            <LegendItem color="#D8C98F" label="Completed" description="Finished item keeps its type color, slightly darker, with line-through text." />
            <LegendItem color="#24B1B1" label="In progress" description="Choice group in progress uses the stronger blue-green fill." />
            <LegendItem color="#E5E7EB" label="Locked" description="Phase and choice group turn gray; skill nodes keep their fill with muted text." />
            <LegendItem color="#4F6F73" label="Skipped" description="Skipped item uses the HTML-like blue-green fill with line-through text." />
          </LegendSection>

          <LegendSection title="Edges">
            <LegendLine color="#2563EB" label="Sequence" description="Main phase progression." />
            <LegendLine color="#2FA084" dashed label="Contains" description="Parent-to-child grouping." />
            <LegendLine color="#7C3AED" dashed label="Choice" description="Choice group to option." />
          </LegendSection>
        </div>
      )}
    </div>
  );
}

function LegendSection({ title, children }) {
  return (
    <section className="mt-4 border-t border-[#E3EEE8] pt-3">
      <h4 className="text-[10px] font-extrabold uppercase tracking-[0.16em] text-slate-500">{title}</h4>
      <div className="mt-2 grid gap-2">{children}</div>
    </section>
  );
}

function LegendItem({ color, label, description }) {
  return (
    <div className="flex gap-2 text-xs leading-5">
      <span className="mt-1 h-3 w-3 shrink-0 rounded-full border border-[#B9D8CC]" style={{ background: color }} />
      <p>
        <span className="font-black">{label}</span>
        <span className="font-semibold text-slate-600"> — {description}</span>
      </p>
    </div>
  );
}

function LegendLine({ color, dashed = false, label, description }) {
  return (
    <div className="flex gap-2 text-xs leading-5">
      <span
        className="mt-2 h-0 w-8 shrink-0 border-t-[3px]"
        style={{ borderColor: color, borderStyle: dashed ? "dashed" : "solid" }}
      />
      <p>
        <span className="font-black">{label}</span>
        <span className="font-semibold text-slate-600"> — {description}</span>
      </p>
    </div>
  );
}
