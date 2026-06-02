export default function ResourceCard({ resource, onOpen, onDelete }) {
  return (
    <article
      onClick={() => onOpen(resource)}
      className="group cursor-pointer rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-blue-300 hover:shadow-md"
    >
      <div>
        <h3 className="line-clamp-2 text-base font-semibold text-slate-900 group-hover:text-blue-700">
          {resource.title}
        </h3>

        <div className="mt-2 flex flex-wrap items-center gap-2 text-xs">
          <span className="rounded-full bg-blue-50 px-2.5 py-1 font-medium text-blue-700">
            {resource.skillName || "General"}
          </span>

          <span className="rounded-full bg-slate-100 px-2.5 py-1 font-medium text-slate-600">
            {resource.type || "article"}
          </span>

          {resource.createdAt && (
            <span className="text-slate-500">
              {new Date(resource.createdAt).toLocaleDateString("vi-VN")}
            </span>
          )}
        </div>
      </div>

      <p className="mt-4 line-clamp-3 text-sm leading-6 text-slate-600">
        {resource.metadata?.summary || "Chưa có tóm tắt cho tài liệu này."}
      </p>

      <div className="mt-5 flex items-center justify-between border-t border-slate-100 pt-4">
        <span className="text-xs font-medium text-slate-400">
          {resource.durationMinutes || 15} phút đọc
        </span>

        <button
          type="button"
          onClick={(event) => {
            event.stopPropagation();
            onDelete(resource.resourceId);
          }}
          className="rounded-lg border border-red-200 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-50"
        >
          Xóa
        </button>
      </div>
    </article>
  );
}