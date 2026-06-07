export default function RoadmapLegend({ isOpen, onToggle }) {
  return (
    <div className="absolute left-4 top-4 z-20 pointer-events-auto">
      <button
        type="button"
        onClick={onToggle}
        className="rounded-xl border border-[#B9D8CC] bg-white px-4 py-2 text-xs font-extrabold tracking-tight text-[#18332D] shadow-sm"
      >
        {isOpen ? "Hide legend" : "Show legend"}
      </button>

      {isOpen && (
        <div className="mt-3 w-[330px] rounded-xl border border-[#B9D8CC] bg-white p-4 text-[#18332D] shadow-lg">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h3 className="text-sm font-extrabold tracking-tight text-[#18332D]">Roadmap legend</h3>
              <p className="mt-1 text-xs font-semibold leading-5 text-slate-600">
                Use this to read node colors and connection types.
              </p>
            </div>

            <button
              type="button"
              onClick={onToggle}
              className="rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] px-2 py-1 text-[10px] font-extrabold"
            >
              Close
            </button>
          </div>

          <LegendSection title="Node types">
            <LegendItem color="#2FA084" label="Phase" description="Main section on the center spine." />
            <LegendItem color="#A7F3D0" label="Choice group" description="Pick or complete a set of options." />
            <LegendItem color="#FFE08A" label="Topic / option" description="Normal learning content." />
            <LegendItem color="#BFDBFE" label="Checkpoint / gate" description="Validation step before moving on." />
            <LegendItem color="#C4B5FD" label="Project / lab" description="Hands-on practice or build task." />
          </LegendSection>

          <LegendSection title="Progress colors">
            <LegendItem color="#22C55E" label="Completed" description="Finished." />
            <LegendItem color="#3B82F6" label="In progress" description="Started but not done." />
            <LegendItem color="#FACC15" label="Pending" description="Available but not started." />
            <LegendItem color="#9CA3AF" label="Locked" description="Prerequisites not completed yet." />
          </LegendSection>

          <LegendSection title="Edges">
            <LegendLine color="#2563EB" label="Blue solid" description="Main phase sequence." />
            <LegendLine color="#2FA084" dashed label="Orange dashed" description="Belongs to / contained in a phase." />
            <LegendLine color="#7C3AED" dashed label="Purple dashed" description="Choice group to its options." />
          </LegendSection>
        </div>
      )}
    </div>
  );
}

function LegendSection({ title, children }) {
  return (
    <section className="mt-4 border-t border-[#B9D8CC] pt-3">
      <h4 className="text-[10px] font-extrabold tracking-[0.16em] text-slate-500">{title}</h4>
      <div className="mt-2 grid gap-2">{children}</div>
    </section>
  );
}

function LegendItem({ color, label, description }) {
  return (
    <div className="flex gap-2 text-xs leading-5">
      <span className="mt-1 h-3 w-3 shrink-0 rounded-xl border border-[#B9D8CC]" style={{ background: color }} />
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
        className="mt-2 h-0 w-8 shrink-0 border-t-2"
        style={{ borderColor: color, borderStyle: dashed ? "dashed" : "solid" }}
      />
      <p>
        <span className="font-black">{label}</span>
        <span className="font-semibold text-slate-600"> — {description}</span>
      </p>
    </div>
  );
}
