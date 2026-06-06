import { Clock, FileText, Trash2 } from "lucide-react";

export default function ResourceCard({ resource, onOpen, onDelete }) {
  return (
    <article
      onClick={() => onOpen(resource)}
      className="group cursor-pointer rounded-3xl border border-[#B9D8CC] bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-[#6FCF97] hover:shadow-[0_18px_44px_rgba(31,111,95,0.12)]"
    >
      <div className="flex items-start gap-3">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-[#6FCF97]/20 text-[#1F6F5F]">
          <FileText size={19} />
        </div>

        <div className="min-w-0">
          <h3 className="line-clamp-2 text-base font-extrabold text-[#18332D] group-hover:text-[#1F6F5F]">
            {resource.title}
          </h3>

          <div className="mt-2 flex flex-wrap items-center gap-2 text-xs">
            <span className="rounded-full bg-[#6FCF97]/20 px-2.5 py-1 font-bold text-[#1F6F5F]">
              {resource.skillName || "General"}
            </span>

            <span className="rounded-full bg-[#EEEEEE] px-2.5 py-1 font-medium text-slate-600">
              {resource.type || "article"}
            </span>
          </div>
        </div>
      </div>

      <p className="mt-4 line-clamp-3 text-sm leading-6 text-slate-600">
        {resource.metadata?.summary || "No summary available for this document yet."}
      </p>

      <div className="mt-5 flex items-center justify-between border-t border-[#DCEBE5] pt-4">
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-slate-400">
          <Clock size={13} />
          {resource.durationMinutes || 15} min read
        </span>

        <button
          type="button"
          onClick={(event) => {
            event.stopPropagation();
            onDelete(resource.resourceId);
          }}
          className="inline-flex items-center gap-1.5 rounded-lg border border-red-200 px-3 py-1.5 text-xs font-bold text-red-600 hover:bg-red-50"
        >
          <Trash2 size={13} />
          Delete
        </button>
      </div>
    </article>
  );
}
