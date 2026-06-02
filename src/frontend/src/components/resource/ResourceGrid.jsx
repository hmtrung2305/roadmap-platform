import ResourceCard from "./ResourceCard";

export default function ResourceGrid({ resources, onOpen, onDelete }) {
  if (!resources || resources.length === 0) {
    return (
      <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
        <h3 className="text-base font-semibold text-slate-800">
          Chưa có tài liệu nào
        </h3>

        <p className="mt-2 text-sm text-slate-500">
          Upload tài liệu markdown đầu tiên để bắt đầu xây dựng kho học liệu.
        </p>
      </div>
    );
  }

  return (
    <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
      {resources.map((resource) => (
        <ResourceCard
          key={resource.resourceId}
          resource={resource}
          onOpen={onOpen}
          onDelete={onDelete}
        />
      ))}
    </div>
  );
}