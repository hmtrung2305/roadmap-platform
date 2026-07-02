import { motion } from "framer-motion";

export default function EditorStat({ label, value, hint }) {
  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.18 }}
      className="rounded-xl border border-[#B9D8CC]/70 bg-white px-4 py-3 shadow-sm"
    >
      <div className="text-[11px] font-bold uppercase tracking-wide text-slate-500">{label}</div>
      <div className="mt-1 truncate text-xl font-extrabold text-[#18332D]">{value}</div>
      {hint && <div className="mt-1 truncate text-xs font-semibold text-slate-500">{hint}</div>}
    </motion.div>
  );
}
