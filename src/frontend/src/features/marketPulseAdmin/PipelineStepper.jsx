import { Check, Circle, LoaderCircle, X } from "lucide-react";
import { operationStepState } from "./adminViewModel";

const steps = [
  { key: "crawler", label: "TopCV crawler", detail: "Collect listings and details" },
  { key: "import", label: ".NET import", detail: "Validate and upsert jobs" },
  { key: "analytics", label: "Analytics ready", detail: "Publish demand insights" },
];

export default function PipelineStepper({ operation }) {
  return (
    <section aria-label="End-to-end refresh progress" className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-base font-extrabold text-[#18332D]">Refresh pipeline</h2>
          <p className="mt-1 text-xs font-semibold text-slate-500">Progress remains available after a page reload or API restart.</p>
        </div>
        {operation && <StatusPill status={operation.status} />}
      </div>
      <ol className="mt-5 grid gap-3 md:grid-cols-3">
        {steps.map((step, index) => {
          const state = operationStepState(operation, step.key);
          return (
            <li key={step.key} className={`relative rounded-xl border p-3 ${stateClass(state)}`}>
              <div className="flex items-start gap-3">
                <span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-white" aria-hidden="true">{stateIcon(state)}</span>
                <div>
                  <div className="text-xs font-bold uppercase tracking-wide opacity-70">Step {index + 1}</div>
                  <div className="mt-0.5 text-sm font-extrabold">{step.label}</div>
                  <div className="mt-1 text-xs font-semibold opacity-75">{step.detail}</div>
                </div>
              </div>
            </li>
          );
        })}
      </ol>
      {operation?.error && <div role="alert" className="mt-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm font-bold text-red-800">{operation.error}</div>}
      {!operation && <p className="mt-4 text-sm font-semibold text-slate-500">No refresh is active. Start one when you need fresh TopCV data end to end.</p>}
    </section>
  );
}

export function StatusPill({ status }) {
  const value = String(status || "unknown").toLowerCase();
  const tone = ["success", "healthy", "ready", "high"].includes(value)
    ? "bg-emerald-50 text-emerald-800 border-emerald-200"
    : ["failed", "critical", "error", "blocked", "low"].includes(value)
      ? "bg-red-50 text-red-800 border-red-200"
      : ["queued", "crawling", "importing", "warning", "degraded", "stale", "medium"].includes(value)
        ? "bg-amber-50 text-amber-800 border-amber-200"
        : "bg-slate-50 text-slate-600 border-slate-200";
  return <span className={`inline-flex min-h-8 items-center rounded-full border px-2.5 text-xs font-extrabold capitalize ${tone}`}>{value.replaceAll("_", " ")}</span>;
}

function stateClass(state) {
  if (state === "complete") return "border-emerald-200 bg-emerald-50 text-emerald-900";
  if (state === "active") return "border-amber-300 bg-amber-50 text-amber-900";
  if (state === "failed") return "border-red-200 bg-red-50 text-red-900";
  return "border-slate-200 bg-slate-50 text-slate-500";
}

function stateIcon(state) {
  if (state === "complete") return <Check size={17} />;
  if (state === "active") return <LoaderCircle size={17} className="animate-spin" />;
  if (state === "failed") return <X size={17} />;
  return <Circle size={14} />;
}
