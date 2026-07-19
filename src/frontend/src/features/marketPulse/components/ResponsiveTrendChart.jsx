import { Table2 } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { formatDate, formatDecimal, formatEstimate } from "../marketPulseViewModel";

export default function ResponsiveTrendChart({
  series,
  ariaLabel,
  emptyMessage = "No publication trend data is available yet.",
}) {
  const containerRef = useRef(null);
  const [width, setWidth] = useState(720);
  const [activeDate, setActiveDate] = useState(null);
  const [hiddenKeys, setHiddenKeys] = useState(() => new Set());
  const [showTable, setShowTable] = useState(false);

  useEffect(() => {
    if (!containerRef.current) return undefined;
    const updateWidth = () => setWidth(Math.max(280, Math.floor(containerRef.current?.clientWidth || 720)));
    updateWidth();
    const observer = new ResizeObserver(updateWidth);
    observer.observe(containerRef.current);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    setHiddenKeys((current) => {
      const valid = new Set(series.map((item) => item.key));
      const next = new Set([...current].filter((key) => valid.has(key)));
      // If a parent replaces the series and leaves only a previously hidden
      // key, reset the legend. Otherwise the empty-state removes the controls
      // needed to make that series visible again.
      return valid.size > 0 && next.size === valid.size ? new Set() : next;
    });
  }, [series]);

  const visibleSeries = series.filter((item) => !hiddenKeys.has(item.key));
  const dates = useMemo(
    () => [...new Set(series.flatMap((item) => item.points.map((point) => point.date)).filter(Boolean))].sort(),
    [series],
  );
  const observedValues = visibleSeries.flatMap((item) => item.points.map((point) => point.value)).filter(Number.isFinite);
  const hasObservedValues = observedValues.length > 0;

  if (!hasObservedValues || dates.length === 0) {
    return <EmptyChart message={emptyMessage} />;
  }

  const mobile = width < 520;
  const height = mobile ? 250 : 320;
  const padding = { top: 22, right: 18, bottom: 42, left: mobile ? 38 : 50 };
  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;
  const maxValue = Math.max(1, ...observedValues);
  const xForIndex = (index) => dates.length <= 1
    ? padding.left + innerWidth / 2
    : padding.left + (index / (dates.length - 1)) * innerWidth;
  const yForValue = (value) => padding.top + innerHeight - (Number(value || 0) / maxValue) * innerHeight;
  const tickStep = Math.max(1, Math.ceil(dates.length / (mobile ? 4 : 6)));
  const activeIndex = activeDate ? dates.indexOf(activeDate) : -1;

  const selectNearestDate = (event) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const pointerX = event.clientX - rect.left;
    const chartX = ((pointerX / Math.max(1, rect.width)) * width - padding.left) / Math.max(1, innerWidth);
    const index = Math.max(0, Math.min(dates.length - 1, Math.round(chartX * (dates.length - 1))));
    setActiveDate(dates[index]);
  };

  return (
    <div ref={containerRef} className="min-w-0">
      <div className="mb-3 flex flex-wrap gap-2" aria-label="Chart series">
        {series.map((item) => {
          const visible = !hiddenKeys.has(item.key);
          return (
            <button
              key={item.key}
              type="button"
              aria-pressed={visible}
              onClick={() => setHiddenKeys((current) => {
                const next = new Set(current);
                if (visible && visibleSeries.length === 1) return next;
                if (visible) next.add(item.key);
                else next.delete(item.key);
                return next;
              })}
              className={`inline-flex min-h-11 items-center gap-2 rounded-full border px-3 text-xs font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] ${
                visible ? "border-[#B9D8CC] bg-white text-[#18332D]" : "border-slate-200 bg-slate-50 text-slate-400"
              }`}
            >
              <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: visible ? item.color : "#cbd5e1" }} />
              {item.label}
            </button>
          );
        })}
      </div>

      <div className="relative rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] p-2">
        <svg
          width="100%"
          height={height}
          viewBox={`0 0 ${width} ${height}`}
          role="img"
          aria-label={ariaLabel}
          onPointerMove={selectNearestDate}
          onPointerDown={selectNearestDate}
          onPointerLeave={() => setActiveDate(null)}
          className="block touch-pan-y"
        >
          {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
            const y = padding.top + innerHeight * ratio;
            return (
              <g key={ratio}>
                <line x1={padding.left} x2={width - padding.right} y1={y} y2={y} stroke="#DCEBE5" />
                <text x={padding.left - 8} y={y + 4} textAnchor="end" fill="#64748b" fontSize="10">
                  {Math.round(maxValue * (1 - ratio))}
                </text>
              </g>
            );
          })}

          {dates.map((date, index) => {
            if (index % tickStep !== 0 && index !== dates.length - 1) return null;
            return (
              <text key={date} x={xForIndex(index)} y={height - 14} textAnchor="middle" fill="#64748b" fontSize="10">
                {date.slice(5)}
              </text>
            );
          })}

          {visibleSeries.map((item) => {
            const pointByDate = new Map(item.points.map((point) => [point.date, point.value]));
            const segments = buildPathSegments(dates, pointByDate, xForIndex, yForValue);
            return (
              <g key={item.key}>
                {segments.map((path, index) => (
                  <path
                    key={`${item.key}-${index}`}
                    d={path}
                    fill="none"
                    stroke={item.color}
                    strokeWidth="3"
                    strokeDasharray={item.strokeDasharray}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                ))}
                {dates.map((date, index) => {
                  const value = pointByDate.get(date);
                  if (!Number.isFinite(value)) return null;
                  return <circle key={date} cx={xForIndex(index)} cy={yForValue(value)} r={activeDate === date ? 5 : 3.5} fill={item.color} />;
                })}
              </g>
            );
          })}

          {activeIndex >= 0 && (
            <line
              x1={xForIndex(activeIndex)}
              x2={xForIndex(activeIndex)}
              y1={padding.top}
              y2={padding.top + innerHeight}
              stroke="#64748b"
              strokeDasharray="4 4"
            />
          )}
        </svg>

        {activeIndex >= 0 && (
          <ChartTooltip date={activeDate} series={visibleSeries} alignRight={activeIndex > dates.length / 2} />
        )}
      </div>

      <button
        type="button"
        aria-expanded={showTable}
        onClick={() => setShowTable((current) => !current)}
        className="mt-3 inline-flex min-h-11 items-center gap-2 rounded-xl px-3 text-sm font-bold text-[#1F6F5F] hover:bg-[#EAF8F1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
      >
        <Table2 size={16} aria-hidden="true" />
        {showTable ? "Hide data table" : "View as table"}
      </button>

      {showTable && <ChartTable dates={dates} series={visibleSeries} />}
    </div>
  );
}

function ChartTooltip({ date, series, alignRight }) {
  return (
    <div
      className={`pointer-events-none absolute top-3 z-10 min-w-40 rounded-xl border border-[#B9D8CC] bg-white/95 p-3 text-xs shadow-lg ${alignRight ? "right-3" : "left-3"}`}
      role="status"
    >
      <div className="font-extrabold text-[#18332D]">{formatDate(date)}</div>
      <div className="mt-2 space-y-1.5">
        {series.map((item) => {
          const point = item.points.find((candidate) => candidate.date === date);
          const value = point?.value;
          return (
            <div key={item.key} className="border-t border-[#DCEBE5] pt-1.5 first:border-0 first:pt-0 text-slate-600">
              <div className="flex items-center justify-between gap-4">
                <span className="inline-flex items-center gap-1.5">
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: item.color }} />
                  {item.label}
                </span>
                <strong className="text-[#18332D]">{Number.isFinite(value) ? formatEstimate(value, point?.approximate) : "Unavailable"}</strong>
              </div>
              {Number.isFinite(point?.exactValue) && Number.isFinite(point?.estimatedValue) && (
                <div className="mt-1 text-[11px] text-slate-500">
                  {formatDecimal(point.exactValue)} exact + {formatDecimal(point.estimatedValue)} estimated
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function ChartTable({ dates, series }) {
  return (
    <div className="mt-2 max-h-72 overflow-auto rounded-xl border border-[#DCEBE5]">
      <table className="w-full min-w-[440px] border-collapse text-left text-xs">
        <caption className="sr-only">Trend chart values by publication date</caption>
        <thead className="sticky top-0 bg-[#EAF8F1] text-[#18332D]">
          <tr>
            <th className="px-3 py-2">Date</th>
            {series.map((item) => <th key={item.key} className="px-3 py-2 text-right">{item.label}</th>)}
          </tr>
        </thead>
        <tbody>
          {dates.map((date) => (
            <tr key={date} className="border-t border-[#DCEBE5] bg-white">
              <td className="px-3 py-2 font-bold text-[#18332D]">{formatDate(date)}</td>
              {series.map((item) => {
                const point = item.points.find((candidate) => candidate.date === date);
                const value = point?.value;
                return (
                  <td key={item.key} className="px-3 py-2 text-right text-slate-600">
                    {Number.isFinite(value) ? formatEstimate(value, point?.approximate) : "Unavailable"}
                    {Number.isFinite(point?.exactValue) && Number.isFinite(point?.estimatedValue) && (
                      <span className="block text-[10px] text-slate-400">{formatDecimal(point.exactValue)} exact / {formatDecimal(point.estimatedValue)} estimated</span>
                    )}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function EmptyChart({ message }) {
  return (
    <div className="flex min-h-56 items-center justify-center rounded-xl border border-dashed border-[#B9D8CC] bg-[#FCFAF6] px-5 text-center text-sm font-semibold text-slate-500">
      {message}
    </div>
  );
}

function buildPathSegments(dates, pointByDate, xForIndex, yForValue) {
  const segments = [];
  let current = [];
  dates.forEach((date, index) => {
    const value = pointByDate.get(date);
    if (!Number.isFinite(value)) {
      if (current.length > 0) segments.push(current);
      current = [];
      return;
    }
    current.push({ x: xForIndex(index), y: yForValue(value) });
  });
  if (current.length > 0) segments.push(current);
  return segments.map((segment) => segment.map((point, index) => `${index === 0 ? "M" : "L"} ${point.x} ${point.y}`).join(" "));
}
