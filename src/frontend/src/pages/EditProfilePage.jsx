import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Globe,
  Mail,
  MapPin,
  Save,
  UserRound,
} from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";
import { getMyProfileApi, updateMyProfileApi } from "../api/profileApi";

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

export default function EditProfilePage() {
  const navigate = useNavigate();

  const [form, setForm] = useState(initialForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    async function fetchProfile() {
      try {
        setLoading(true);
        setError("");

        const data = await getMyProfileApi();

        setForm({
          ...initialForm,
          ...data,
        });
      } catch (err) {
        console.error("Failed to load profile:", err);
        setError(err.message || "Cannot load profile.");
      } finally {
        setLoading(false);
      }
    }

    fetchProfile();
  }, []);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    try {
      setSaving(true);
      setError("");

      await updateMyProfileApi(form);

      navigate("/portfolio");
    } catch (err) {
      console.error("Failed to update profile:", err);
      setError(err.message || "Cannot update profile.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-10">
        <div className="mx-auto max-w-5xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
          <p className="text-slate-500">Loading profile...</p>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-slate-100 px-6 py-10">
      <form
        onSubmit={handleSubmit}
        className="mx-auto max-w-6xl overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl"
      >
        <div className="grid grid-cols-1 lg:grid-cols-[340px_1fr]">
          <aside className="bg-blue-50 p-8">
            <div className="flex flex-col items-center text-center">
              <div className="h-32 w-32 overflow-hidden rounded-full border-4 border-white bg-blue-100 shadow-sm">
                {form.avatarUrl ? (
                  <img
                    src={form.avatarUrl}
                    alt={form.displayName || "Avatar"}
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <div className="flex h-full w-full items-center justify-center text-blue-700">
                    <UserRound size={48} />
                  </div>
                )}
              </div>

              <h2 className="mt-6 text-2xl font-bold text-slate-900">
                {form.displayName || "Your Name"}
              </h2>

              <p className="mt-1 text-sm text-slate-500">
                {form.headline || "Your headline"}
              </p>

              <span className="mt-5 rounded-full bg-blue-100 px-4 py-1.5 text-sm font-semibold text-blue-700">
                {form.isPublic ? "Public Portfolio" : "Private Portfolio"}
              </span>
            </div>

            <div className="mt-10 rounded-xl bg-white p-5 shadow-sm">
              <p className="text-sm font-bold text-slate-900">
                Profile Preview
              </p>

              <div className="mt-4 space-y-3 text-sm text-slate-600">
                {form.location && (
                  <PreviewItem
                    icon={<MapPin size={16} />}
                    text={form.location}
                  />
                )}

                {form.publicEmail && (
                  <PreviewItem
                    icon={<Mail size={16} />}
                    text={form.publicEmail}
                  />
                )}

                {form.githubUrl && (
                  <PreviewItem
                    icon={<FaGithub size={16} />}
                    text="GitHub linked"
                  />
                )}

                {form.linkedinUrl && (
                  <PreviewItem
                    icon={<FaLinkedin size={16} />}
                    text="LinkedIn linked"
                  />
                )}

                {form.personalWebsiteUrl && (
                  <PreviewItem
                    icon={<Globe size={16} />}
                    text="Website linked"
                  />
                )}

                {!form.location &&
                  !form.publicEmail &&
                  !form.githubUrl &&
                  !form.linkedinUrl &&
                  !form.personalWebsiteUrl && (
                    <p className="text-slate-400">No public links yet.</p>
                  )}
              </div>
            </div>
          </aside>

          <section className="p-8">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">
                Edit Profile
              </h1>
              <p className="mt-1 text-slate-500">
                Update your personal information and portfolio profile.
              </p>
            </div>

            {error && (
              <div className="mt-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                {error}
              </div>
            )}

            <div className="mt-8 border-t border-slate-200 pt-7">
              <SectionTitle>Basic Information</SectionTitle>

              <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                <Input
                  label="Display Name"
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
                  placeholder="Ho Chi Minh City, Vietnam"
                />

                <Input
                  label="Current Role"
                  name="currentRole"
                  value={form.currentRole}
                  onChange={handleChange}
                  placeholder="Student / Intern / Developer"
                />

                <Input
                  label="Career Goal"
                  name="careerGoal"
                  value={form.careerGoal}
                  onChange={handleChange}
                  placeholder="Full-stack Developer"
                />

                <Input
                  label="Public Email"
                  name="publicEmail"
                  value={form.publicEmail}
                  onChange={handleChange}
                  placeholder="example@email.com"
                />
              </div>

              <div className="mt-5">
                <label className="mb-2 block text-sm font-semibold text-slate-700">
                  Bio
                </label>
                <textarea
                  name="bio"
                  value={form.bio || ""}
                  onChange={handleChange}
                  rows={4}
                  placeholder="Write a short introduction about yourself..."
                  className="w-full resize-none rounded-lg border border-slate-300 bg-slate-50 px-4 py-3 text-slate-900 outline-none transition focus:border-blue-500 focus:bg-white"
                />
              </div>
            </div>

            <div className="mt-8 border-t border-slate-200 pt-7">
              <SectionTitle>Links & Media</SectionTitle>

              <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
                <Input
                  label="Avatar URL"
                  name="avatarUrl"
                  value={form.avatarUrl}
                  onChange={handleChange}
                  placeholder="https://..."
                />

                <Input
                  label="Cover Image URL"
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
                  label="Personal Website"
                  name="personalWebsiteUrl"
                  value={form.personalWebsiteUrl}
                  onChange={handleChange}
                  placeholder="https://your-site.com"
                />
              </div>

              <label className="mt-6 flex cursor-pointer items-center justify-between rounded-xl border border-slate-200 bg-slate-50 px-4 py-4">
                <div>
                  <p className="text-sm font-semibold text-slate-800">
                    Make portfolio public
                  </p>
                  <p className="mt-1 text-sm text-slate-500">
                    Allow other users to view your public portfolio by username.
                  </p>
                </div>

                <input
                  type="checkbox"
                  name="isPublic"
                  checked={form.isPublic}
                  onChange={handleChange}
                  className="h-5 w-5 accent-blue-700"
                />
              </label>
            </div>

            <div className="sticky bottom-0 mt-8 flex justify-end gap-3 border-t border-slate-200 bg-white pt-6">
              <button
                type="button"
                onClick={() => navigate(-1)}
                className="rounded-lg px-5 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100"
              >
                Cancel
              </button>

              <button
                type="submit"
                disabled={saving}
                className="inline-flex items-center gap-2 rounded-lg bg-blue-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <Save size={16} />
                {saving ? "Saving..." : "Save Changes"}
              </button>
            </div>
          </section>
        </div>
      </form>
    </main>
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
        className="w-full rounded-lg border border-slate-300 bg-slate-50 px-4 py-3 text-slate-900 outline-none transition focus:border-blue-500 focus:bg-white"
      />
    </div>
  );
}

function SectionTitle({ children }) {
  return (
    <h3 className="mb-5 text-sm font-bold uppercase tracking-wide text-blue-700">
      {children}
    </h3>
  );
}

function PreviewItem({ icon, text }) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-blue-700">{icon}</span>
      <span className="truncate">{text}</span>
    </div>
  );
}
