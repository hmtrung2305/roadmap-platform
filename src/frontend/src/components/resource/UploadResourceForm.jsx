import { FileUp, UploadCloud } from "lucide-react";
import { useState } from "react";

export default function UploadResourceForm({ onUpload, isUploading }) {
  const [title, setTitle] = useState("");
  const [skillName, setSkillName] = useState("");
  const [file, setFile] = useState(null);

  const handleSubmit = async (event) => {
    event.preventDefault();

    const formElement = event.currentTarget;

    if (!title.trim()) {
      alert("Please enter a document title.");
      return;
    }

    if (!skillName.trim()) {
      alert("Please enter a skill or topic.");
      return;
    }

    if (!file) {
      alert("Please choose a markdown file.");
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
      className="overflow-hidden rounded-lg border border-[#B9D8CC] bg-white shadow-[0_18px_50px_rgba(31,111,95,0.10)]"
    >
      <div className="border-b border-[#DCEBE5] bg-gradient-to-r from-[#F7F1E8] via-white to-[#EEF7F1] px-6 py-5">
        <div className="flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-[#2FA084] text-white">
            <UploadCloud size={21} />
          </div>
          <div>
            <h2 className="text-lg font-extrabold text-[#18332D]">
              Upload learning document
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              Upload markdown files and let AI use them as learning context.
            </p>
          </div>
        </div>
      </div>

      <div className="p-6">
        <div className="grid gap-4 md:grid-cols-2">
          <Field label="Document title">
            <input
              type="text"
              placeholder="Example: React useEffect basics"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              className="w-full rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/60 px-4 py-2.5 text-sm outline-none transition focus:border-[#2FA084] focus:bg-white focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </Field>

          <Field label="Skill / topic">
            <input
              type="text"
              placeholder="Example: ReactJS"
              value={skillName}
              onChange={(event) => setSkillName(event.target.value)}
              className="w-full rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/60 px-4 py-2.5 text-sm outline-none transition focus:border-[#2FA084] focus:bg-white focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </Field>
        </div>

        <div className="mt-4">
          <Field label="Markdown file">
            <label className="flex cursor-pointer flex-col items-center justify-center rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 px-4 py-6 text-center transition hover:border-[#2FA084] hover:bg-[#6FCF97]/10">
              <FileUp className="text-[#1F6F5F]" size={24} />
              <span className="mt-2 text-sm font-bold text-[#18332D]">
                {file ? file.name : "Choose a .md file"}
              </span>
              <span className="mt-1 text-xs text-slate-500">
                Markdown only
              </span>
              <input
                type="file"
                accept=".md"
                onChange={(event) => setFile(event.target.files?.[0] || null)}
                className="hidden"
              />
            </label>
          </Field>
        </div>

        <div className="mt-5 flex justify-end">
          <button
            type="submit"
            disabled={isUploading}
            className="rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-bold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-[#6FCF97]"
          >
            {isUploading ? "Uploading..." : "Upload document"}
          </button>
        </div>
      </div>
    </form>
  );
}

function Field({ label, children }) {
  return (
    <div>
      <label className="mb-1.5 block text-sm font-bold text-[#18332D]">
        {label}
      </label>
      {children}
    </div>
  );
}
