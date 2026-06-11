import ResourceCard from "./ResourceCard";

export default function ResourceGrid({ resources, onOpen, onDelete }) {
  if (!resources || resources.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-white p-10 text-center shadow-sm">
        <h3 className="text-base font-extrabold text-[#18332D]">
          No documents yet
        </h3>

        <p className="mt-2 text-sm text-slate-500">
          Upload your first markdown document to start building your learning library.
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
