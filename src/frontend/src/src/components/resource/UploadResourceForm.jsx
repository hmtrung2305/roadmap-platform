import { useState } from "react";

export default function UploadResourceForm({ onUpload, isUploading }) {
  const [title, setTitle] = useState("");
  const [skillName, setSkillName] = useState("");
  const [file, setFile] = useState(null);

  const handleSubmit = async (event) => {
    event.preventDefault();

    const formElement = event.currentTarget;

    if (!title.trim()) {
      alert("Vui lòng nhập tiêu đề tài liệu.");
      return;
    }

    if (!skillName.trim()) {
      alert("Vui lòng nhập kỹ năng hoặc chủ đề.");
      return;
    }

    if (!file) {
      alert("Vui lòng chọn file markdown.");
      return;
    }

    try {
      await onUpload({
        title: title.trim(),
        skillName: skillName.trim(),
        file,
      });

      setTitle("");
      setSkillName("");
      setFile(null);
      formElement.reset();
    } catch (error) {
      console.error("Submit upload failed:", error);
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
    >
      <div className="mb-4">
        <h2 className="text-lg font-semibold text-slate-900">
          Upload tài liệu học
        </h2>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">
            Tiêu đề tài liệu
          </label>

          <input
            type="text"
            placeholder="Ví dụ: React useEffect cơ bản"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">
            Kỹ năng / chủ đề
          </label>

          <input
            type="text"
            placeholder="Ví dụ: ReactJS"
            value={skillName}
            onChange={(event) => setSkillName(event.target.value)}
            className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>
      </div>

      <div className="mt-4">
        <label className="mb-1 block text-sm font-medium text-slate-700">
          File markdown
        </label>

        <input
          type="file"
          accept=".md"
          onChange={(event) => setFile(event.target.files?.[0] || null)}
          className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm"
        />
      </div>

      <div className="mt-5 flex justify-end">
        <button
          type="submit"
          disabled={isUploading}
          className="rounded-xl bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
        >
          {isUploading ? "Đang upload..." : "Upload tài liệu"}
        </button>
      </div>
    </form>
  );
}
