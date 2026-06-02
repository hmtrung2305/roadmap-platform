import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useResourceStore } from "../stores/useResourceStore";
import UploadResourceForm from "../components/resource/UploadResourceForm";
import ResourceGrid from "../components/resource/ResourceGrid";
import { toast } from "react-toastify";

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
      toast.success("Upload tài liệu thành công!");
    } catch (err) {
      const message =
      err?.response?.data?.message ||
      "Upload thất bại, vui lòng thử lại.";

    toast.error(message);
    }
  };
  const handleDelete = async (resourceId) => {
    const confirmed = window.confirm(
      "Bạn có chắc chắn muốn xóa tài liệu này không?",
    );

    if (!confirmed) return;

    try {
      await deleteResource(resourceId);
      toast.success("Xóa tài liệu thành công");

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
    <main className="min-h-screen bg-slate-50 px-6 py-8">
      <div className="mx-auto max-w-7xl">
        <header className="mb-6">
          <p className="text-sm font-semibold uppercase tracking-wide text-blue-600">
            TechMap Learning
          </p>

          <h1 className="mt-1 text-3xl font-bold text-slate-950">
            Quản lý tài liệu học
          </h1>

          <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
            Upload và quản lý tài liệu học. Mỗi tài liệu sẽ được backend chia
            thành nhiều chunk để phục vụ AI chat theo nội dung tài liệu.
          </p>
        </header>

        <section className="mb-6">
          <UploadResourceForm
            onUpload={handleUpload}
            isUploading={isUploading}
          />
        </section>

        {error && (
          <div className="mb-5 flex items-center justify-between rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            <span>{error}</span>

            <button
              type="button"
              onClick={clearError}
              className="font-semibold hover:underline"
            >
              Đóng
            </button>
          </div>
        )}

        <section>
          <div className="mb-4 flex items-center justify-between gap-4">
            <div>
              <h2 className="text-xl font-semibold text-slate-900">
                Danh sách tài liệu
              </h2>

              <p className="mt-1 text-sm text-slate-500">
                Tổng cộng {resources.length} tài liệu.
              </p>
            </div>

            <button
              type="button"
              onClick={fetchResources}
              disabled={isFetching}
              className="rounded-xl border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isFetching ? "Đang tải..." : "Refresh"}
            </button>
          </div>

          {isFetching ? (
            <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
              Đang tải tài liệu...
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
