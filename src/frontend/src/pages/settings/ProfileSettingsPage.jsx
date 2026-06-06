import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Eye, Globe, Mail, MapPin, Save, UserRound } from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";
import { toast } from "react-toastify";
import { getMyProfileApi, updateMyProfileApi } from "../../api/profileApi";

const initialForm = {
  displayName: "",
  headline: "",
  bio: "",
  location: "",
  avatarUrl: "",
  coverImageUrl: "",
  careerGoal: "",
  currentRole: "",
  publicEmail: "",
  githubUrl: "",
  linkedinUrl: "",
  personalWebsiteUrl: "",
  isPublic: false,
};

export default function ProfileSettingsPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState(initialForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const fetchProfile = async () => {
    try {
      setLoading(true);
      setError("");

      const data = await getMyProfileApi();

      setForm({
        ...initialForm,
        ...data,
      });
    } catch (error) {
      console.error("Failed to load profile:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message || "Unable to load profile.";

      setError(serverMessage);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;

    if (error) {
      setError("");
    }

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleCancel = () => {
    toast.info("Profile editing cancelled.");
    navigate("/profile");
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    try {
      setSaving(true);
      setError("");

      await updateMyProfileApi(form);

      toast.success("Profile updated successfully.");
      navigate("/profile");
    } catch (error) {
      console.error("Failed to update profile:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message || "Unable to update profile.";

      setError(serverMessage);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl">
        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <p className="text-sm text-slate-500">Loading profile...</p>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto max-w-4xl space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
            Profile settings
          </h1>
        </div>

        <button
          type="button"
          onClick={() => navigate("/profile")}
          className="inline-flex h-10 items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-4 text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50"
        >
          <Eye size={16} />
          View profile
        </button>
      </div>

      {error && (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <div className="relative h-40 bg-indigo-50">
          {form.coverImageUrl ? (
            <img
              src={form.coverImageUrl}
              alt="Cover"
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="h-full w-full bg-gradient-to-r from-indigo-100 via-blue-50 to-teal-50" />
          )}

          <div className="absolute -bottom-12 left-6 h-24 w-24 overflow-hidden rounded-3xl border-4 border-white bg-indigo-50 shadow-sm">
            {form.avatarUrl ? (
              <img
                src={form.avatarUrl}
                alt={form.displayName || "Avatar"}
                className="h-full w-full object-cover"
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center text-indigo-700">
                <UserRound size={38} />
              </div>
            )}
          </div>
        </div>

        <div className="px-6 pb-6 pt-16">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-2xl font-bold text-slate-900">
                {form.displayName || "Your name"}
              </h2>

              <p className="mt-1 text-sm text-slate-500">
                {form.headline || "Your headline"}
              </p>
            </div>

            <span
              className={`w-fit rounded-full px-4 py-2 text-xs font-bold ${
                form.isPublic
                  ? "bg-teal-50 text-teal-700"
                  : "bg-slate-100 text-slate-600"
              }`}
            >
              {form.isPublic ? "Public profile" : "Private profile"}
            </span>
          </div>

          <div className="mt-5 flex flex-wrap gap-3 text-sm text-slate-500">
            {form.location && (
              <PreviewItem icon={<MapPin size={16} />} text={form.location} />
            )}

            {form.publicEmail && (
              <PreviewItem icon={<Mail size={16} />} text={form.publicEmail} />
            )}

            {form.githubUrl && (
              <PreviewItem icon={<FaGithub size={16} />} text="GitHub" />
            )}

            {form.linkedinUrl && (
              <PreviewItem icon={<FaLinkedin size={16} />} text="LinkedIn" />
            )}

            {form.personalWebsiteUrl && (
              <PreviewItem icon={<Globe size={16} />} text="Website" />
            )}
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-6 py-5">
          <h2 className="text-base font-bold text-slate-900">Basic info</h2>
        </div>

        <div className="grid grid-cols-1 gap-5 px-6 py-5 md:grid-cols-2">
          <Input
            label="Display name"
            name="displayName"
            value={form.displayName}
            onChange={handleChange}
            placeholder="Nguyen Van A"
          />

          <Input
            label="Headline"
            name="headline"
            value={form.headline}
            onChange={handleChange}
            placeholder="Frontend Developer"
          />

          <Input
            label="Location"
            name="location"
            value={form.location}
            onChange={handleChange}
            placeholder="Ho Chi Minh City"
          />

          <Input
            label="Current role"
            name="currentRole"
            value={form.currentRole}
            onChange={handleChange}
            placeholder="Student / Developer"
          />

          <Input
            label="Career goal"
            name="careerGoal"
            value={form.careerGoal}
            onChange={handleChange}
            placeholder="Full-stack Developer"
          />

          <Input
            label="Public email"
            name="publicEmail"
            value={form.publicEmail}
            onChange={handleChange}
            placeholder="example@email.com"
          />

          <div className="md:col-span-2">
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Bio
            </label>

            <textarea
              name="bio"
              value={form.bio || ""}
              onChange={handleChange}
              rows={4}
              placeholder="Write a short introduction..."
              className="w-full resize-none rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-6 py-5">
          <h2 className="text-base font-bold text-slate-900">Links & media</h2>
        </div>

        <div className="grid grid-cols-1 gap-5 px-6 py-5 md:grid-cols-2">
          <Input
            label="Avatar URL"
            name="avatarUrl"
            value={form.avatarUrl}
            onChange={handleChange}
            placeholder="https://..."
          />

          <Input
            label="Cover image URL"
            name="coverImageUrl"
            value={form.coverImageUrl}
            onChange={handleChange}
            placeholder="https://..."
          />

          <Input
            label="GitHub URL"
            name="githubUrl"
            value={form.githubUrl}
            onChange={handleChange}
            placeholder="https://github.com/username"
          />

          <Input
            label="LinkedIn URL"
            name="linkedinUrl"
            value={form.linkedinUrl}
            onChange={handleChange}
            placeholder="https://linkedin.com/in/username"
          />

          <Input
            label="Website"
            name="personalWebsiteUrl"
            value={form.personalWebsiteUrl}
            onChange={handleChange}
            placeholder="https://your-site.com"
          />

          <label className="flex cursor-pointer items-center justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
            <div>
              <p className="text-sm font-bold text-slate-900">
                Public profile
              </p>
            </div>

            <input
              type="checkbox"
              name="isPublic"
              checked={form.isPublic}
              onChange={handleChange}
              className="h-5 w-5 accent-indigo-700"
            />
          </label>
        </div>
      </section>

      <div className="flex justify-end gap-3 rounded-3xl border border-slate-200 bg-white px-6 py-5 shadow-sm">
        <button
          type="button"
          onClick={handleCancel}
          disabled={saving}
          className="h-10 rounded-xl px-5 text-sm font-semibold text-slate-600 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Cancel
        </button>

        <button
          type="submit"
          disabled={saving}
          className="inline-flex h-10 items-center gap-2 rounded-xl bg-indigo-700 px-5 text-sm font-semibold text-white shadow-lg shadow-indigo-700/20 transition hover:bg-indigo-800 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Save size={16} />
          {saving ? "Saving..." : "Save changes"}
        </button>
      </div>
    </form>
  );
}

function Input({ label, name, value, onChange, placeholder }) {
  return (
    <div>
      <label className="mb-2 block text-sm font-semibold text-slate-700">
        {label}
      </label>

      <input
        name={name}
        value={value || ""}
        onChange={onChange}
        placeholder={placeholder}
        className="h-11 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
      />
    </div>
  );
}

function PreviewItem({ icon, text }) {
  return (
    <div className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1.5">
      <span className="text-indigo-700">{icon}</span>
      <span className="truncate">{text}</span>
    </div>
  );
}