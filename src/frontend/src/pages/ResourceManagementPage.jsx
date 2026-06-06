import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { AlertCircle, BookOpenText, Library } from "lucide-react";
import { toast } from "react-toastify";
import UploadResourceForm from "../components/resource/UploadResourceForm";
import ResourceGrid from "../components/resource/ResourceGrid";
import { useResourceStore } from "../stores/useResourceStore";

export default function ResourceManagementPage() {
  const navigate = useNavigate();

  const resources = useResourceStore((state) => state.resources);
  const isFetching = useResourceStore((state) => state.isFetching);
  const isUploading = useResourceStore((state) => state.isUploading);
  const error = useResourceStore((state) => state.error);

  const fetchResources = useResourceStore((state) => state.fetchResources);
  const uploadResource = useResourceStore((state) => state.uploadResource);
  const deleteResource = useResourceStore((state) => state.deleteResource);
  const clearError = useResourceStore((state) => state.clearError);

  useEffect(() => {
    fetchResources();
  }, [fetchResources]);

  const handleUpload = async ({ title, skillName, file }) => {
    try {
      await uploadResource({
        title,
        skillName,
        file,
      });
      toast.success("Resource uploaded successfully!");
    } catch (err) {
      const message =
        err?.response?.data?.message ||
        "Upload failed. Please try again.";

      toast.error(message);
    }
  };

  const handleDelete = async (resourceId) => {
    const confirmed = window.confirm(
      "Are you sure you want to delete this resource?",
    );

    if (!confirmed) return;

    try {
      await deleteResource(resourceId);
      toast.success("Resource deleted successfully.");
    } catch (error) {
      const message = error?.response?.data.message;
      toast.error(message);
    }
  };

  const handleOpenResource = (resource) => {
    navigate(`/study/${resource.resourceId}`, {
      state: {
        resource,
      },
    });
  };

  return (
    <main className="min-h-screen bg-[#F7F1E8] px-6 py-8">
      <div className="mx-auto max-w-7xl space-y-6">
        <header className="overflow-hidden rounded-[2rem] border border-[#B9D8CC] bg-white shadow-[0_18px_50px_rgba(31,111,95,0.10)]">
          <div className="bg-gradient-to-r from-[#1F6F5F] via-[#2FA084] to-[#6FCF97] px-6 py-8 text-white">
            <div className="flex items-center gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/15 text-white ring-1 ring-white/25">
                <Library size={24} />
              </div>
              <div>
                <p className="text-sm font-extrabold uppercase tracking-[0.18em] text-white/80">
                  TechMap Learning
                </p>

                <h1 className="mt-1 text-3xl font-extrabold tracking-tight">
                  Manage learning documents
                </h1>
              </div>
            </div>

            <p className="mt-4 max-w-2xl text-sm leading-7 text-white/85">
              Upload markdown documents, organize them by skill, and use them as
              context for the AI mentor inside your study room.
            </p>
          </div>

          <div className="grid grid-cols-1 gap-4 bg-white px-6 py-5 sm:grid-cols-3">
            <Metric label="Documents" value={resources.length} />
            <Metric label="Current mode" value="Markdown" />
            <Metric label="AI support" value="Enabled" />
          </div>
        </header>

        <UploadResourceForm
          onUpload={handleUpload}
          isUploading={isUploading}
        />

        {error && (
          <div className="flex items-center justify-between rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            <span className="inline-flex items-center gap-2">
              <AlertCircle size={16} />
              {error}
            </span>

            <button
              type="button"
              onClick={clearError}
              className="text-xs font-bold uppercase tracking-wide hover:underline"
            >
              Close
            </button>
          </div>
        )}

        <section>
          <div className="mb-4 flex items-center gap-2">
            <BookOpenText size={20} className="text-[#1F6F5F]" />
            <h2 className="text-xl font-extrabold text-[#18332D]">
              Uploaded documents
            </h2>
          </div>

          {isFetching ? (
            <div className="rounded-3xl border border-[#B9D8CC] bg-white p-10 text-center text-sm text-slate-500 shadow-sm">
              Loading documents...
            </div>
          ) : (
            <ResourceGrid
              resources={resources}
              onOpen={handleOpenResource}
              onDelete={handleDelete}
            />
          )}
        </section>
      </div>
    </main>
  );
}

function Metric({ label, value }) {
  return (
    <div className="rounded-2xl border border-[#B9D8CC] bg-[#F7F1E8]/60 px-4 py-3">
      <p className="text-xs font-extrabold uppercase tracking-[0.16em] text-[#1F6F5F]">
        {label}
      </p>
      <p className="mt-1 text-lg font-extrabold text-[#18332D]">{value}</p>
    </div>
  );
}
