import { useEffect, useState } from "react";
import { Eye, EyeOff, Globe2, ShieldCheck } from "lucide-react";
import { toast } from "react-toastify";
import { getMyProfileApi, updateMyProfileApi } from "../../api/profileApi";

const initialProfile = {
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

export default function PrivacySettingsPage() {
  const [profile, setProfile] = useState(initialProfile);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const fetchProfile = async () => {
    try {
      setLoading(true);
      setError("");

      const data = await getMyProfileApi();
      setProfile({ ...initialProfile, ...data });
    } catch (error) {
      console.error("Failed to load privacy settings:", error.response?.data || error);
      setError(error.response?.data?.message || "Unable to load privacy settings.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const handleTogglePublicPortfolio = async () => {
    const nextIsPublic = !profile.isPublic;
    const previousProfile = profile;

    try {
      setSaving(true);
      setError("");
      setProfile((current) => ({ ...current, isPublic: nextIsPublic }));

      await updateMyProfileApi({
        ...profile,
        isPublic: nextIsPublic,
      });

      toast.success(
        nextIsPublic
          ? "Public portfolio is now visible."
          : "Public portfolio is now private."
      );
    } catch (error) {
      console.error("Failed to update portfolio privacy:", error.response?.data || error);
      setProfile(previousProfile);
      setError(error.response?.data?.message || "Unable to update portfolio privacy.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl">
        <div className="tm-surface p-6">
          <p className="text-sm text-slate-500">Loading privacy settings...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <p className="text-xs font-bold uppercase tracking-[0.22em] text-[#2F7F98]">
          Privacy
        </p>

        <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
          Privacy settings
        </h1>

        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-500">
          Control what other people can see from your learning profile.
        </p>
      </div>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <section className="tm-surface overflow-hidden">
        <div className="border-b border-[#B9D8CC]/60 px-6 py-5">
          <div className="flex items-center gap-3">
            <span className="grid size-11 place-items-center rounded-lg bg-[#5A9CB5]/12 text-[#2F7F98]">
              <ShieldCheck size={21} />
            </span>
            <div>
              <h2 className="text-base font-bold text-[#18332D]">Portfolio visibility</h2>
              <p className="mt-1 text-sm leading-6 text-slate-500">
                Decide whether your portfolio can be viewed through its public link.
              </p>
            </div>
          </div>
        </div>

        <div className="px-6 py-5">
          <button
            type="button"
            onClick={handleTogglePublicPortfolio}
            disabled={saving}
            className="flex w-full items-center justify-between gap-4 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/55 px-5 py-4 text-left transition hover:border-[#5A9CB5] hover:bg-[#5A9CB5]/8 disabled:cursor-not-allowed disabled:opacity-70"
          >
            <div className="flex min-w-0 items-center gap-4">
              <span className="grid size-11 shrink-0 place-items-center rounded-lg bg-white text-[#2F7F98] shadow-sm ring-1 ring-[#B9D8CC]">
                {profile.isPublic ? <Eye size={20} /> : <EyeOff size={20} />}
              </span>

              <div className="min-w-0">
                <p className="text-sm font-bold text-[#18332D]">Public portfolio</p>
                <p className="mt-1 text-sm leading-6 text-slate-500">
                  {profile.isPublic
                    ? "Your portfolio is visible to visitors with the link."
                    : "Your portfolio is private until you turn this on."}
                </p>
              </div>
            </div>

            <span
              className={`relative h-7 w-12 shrink-0 rounded-full transition ${
                profile.isPublic ? "bg-[#2FA084]" : "bg-slate-300"
              }`}
            >
              <span
                className={`absolute top-1 size-5 rounded-full bg-white shadow transition ${
                  profile.isPublic ? "left-6" : "left-1"
                }`}
              />
            </span>
          </button>

          <div className="mt-4 flex items-center gap-2 rounded-lg bg-[#5A9CB5]/8 px-4 py-3 text-sm font-semibold text-[#2F7F98]">
            <Globe2 size={17} />
            {saving ? "Saving visibility..." : profile.isPublic ? "Public page enabled" : "Public page disabled"}
          </div>
        </div>
      </section>
    </div>
  );
}
