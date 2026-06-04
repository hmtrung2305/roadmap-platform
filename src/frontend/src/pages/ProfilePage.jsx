import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Briefcase,
  Edit3,
  Globe,
  Mail,
  MapPin,
  Target,
  UserRound,
} from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";
import { getMyProfileApi } from "../api/profileApi";

export default function ProfilePage() {
  const navigate = useNavigate();

  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function fetchProfile() {
      try {
        setLoading(true);
        setError("");

        const data = await getMyProfileApi();
        setProfile(data);
      } catch (error) {
        console.error("Failed to load profile:", error.response?.data || error);

        const serverMessage =
          error.response?.data?.message || "Unable to load profile.";

        setError(serverMessage);
      } finally {
        setLoading(false);
      }
    }

    fetchProfile();
  }, []);

  if (loading) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-slate-50 px-6 py-8">
        <div className="mx-auto max-w-5xl rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <p className="text-sm text-slate-500">Loading profile...</p>
        </div>
      </main>
    );
  }

  if (error) {
    return (
      <main className="min-h-[calc(100vh-4rem)] bg-slate-50 px-6 py-8">
        <div className="mx-auto max-w-5xl rounded-3xl border border-red-200 bg-red-50 p-6 text-sm text-red-600">
          {error}
        </div>
      </main>
    );
  }

  const hasProfileDetails =
    profile?.bio ||
    profile?.location ||
    profile?.publicEmail ||
    profile?.currentRole ||
    profile?.careerGoal;

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-slate-50 px-6 py-8 text-slate-900">
      <div className="mx-auto max-w-5xl space-y-6">
        <section className="rounded-3xl border border-slate-200 bg-white shadow-sm">
          <div className="overflow-hidden rounded-t-3xl">
            <div className="relative h-50 bg-indigo-50">
              {profile?.coverImageUrl ? (
                <img
                  src={profile.coverImageUrl}
                  alt="Profile cover"
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="h-full w-full bg-gradient-to-r from-indigo-100 via-blue-50 to-teal-50" />
              )}

              <button
                type="button"
                onClick={() => navigate("/settings/profile")}
                className="absolute right-5 top-5 inline-flex h-10 items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50"
              >
                <Edit3 size={16} />
                Edit profile
              </button>
            </div>
          </div>

          <div className="px-6 pb-3">
            <div className="-mt-12 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                <div className="relative z-10 h-24 w-24 shrink-0 overflow-hidden rounded-3xl border-4 border-white bg-indigo-50 shadow-md">
                  {profile?.avatarUrl ? (
                    <img
                      src={profile.avatarUrl}
                      alt={profile.displayName || "Avatar"}
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <div className="flex h-full w-full items-center justify-center text-indigo-700">
                      <UserRound size={38} />
                    </div>
                  )}
                </div>

                <div className="pb-1 sm:pb-2 mt-14">
                  <h1 className="text-3xl font-bold tracking-tight text-slate-900">
                    {profile?.displayName || "Your name"}
                  </h1>

                  <p className="mt-1 text-sm text-slate-500">
                    {profile?.headline || "No headline yet"}
                  </p>
                </div>
              </div>

              <span
                className={`self-start rounded-full px-4 py-2 text-xs font-bold sm:self-end ${
                  profile?.isPublic
                    ? "bg-teal-50 text-teal-700"
                    : "bg-slate-100 text-slate-600"
                }`}
              >
                {profile?.isPublic ? "Public profile" : "Private profile"}
              </span>
            </div>

            {profile?.bio ? (
              <p className="mt-6 max-w-3xl text-sm leading-7 text-slate-600">
                {profile.bio}
              </p>
            ) : (
              <div className="mt-6 rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-4 text-sm text-slate-500">
                Add a short bio to introduce yourself to others.
              </div>
            )}

            <div className="mt-5 flex flex-wrap gap-3">
              {profile?.location && (
                <ProfilePill
                  icon={<MapPin size={16} />}
                  text={profile.location}
                />
              )}

              {profile?.publicEmail && (
                <ProfilePill
                  icon={<Mail size={16} />}
                  text={profile.publicEmail}
                />
              )}

              {profile?.currentRole && (
                <ProfilePill
                  icon={<Briefcase size={16} />}
                  text={profile.currentRole}
                />
              )}

              {profile?.careerGoal && (
                <ProfilePill
                  icon={<Target size={16} />}
                  text={profile.careerGoal}
                />
              )}
            </div>
          </div>
        </section>

        <section className="grid gap-6 md:grid-cols-[1fr_0.9fr]">
          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-900">About</h2>

              {!hasProfileDetails && (
                <button
                  type="button"
                  onClick={() => navigate("/settings/profile")}
                  className="text-sm font-semibold text-indigo-700 transition hover:text-indigo-800"
                >
                  Complete profile
                </button>
              )}
            </div>

            <div className="mt-5 space-y-5">
              <InfoItem label="Current role" value={profile?.currentRole} />
              <InfoItem label="Career goal" value={profile?.careerGoal} />
              <InfoItem label="Location" value={profile?.location} />
              <InfoItem label="Public email" value={profile?.publicEmail} />
            </div>
          </div>

          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-base font-bold text-slate-900">Links</h2>

            <div className="mt-5 space-y-3">
              <LinkItem
                icon={<FaGithub size={17} />}
                label="GitHub"
                url={profile?.githubUrl}
              />

              <LinkItem
                icon={<FaLinkedin size={17} />}
                label="LinkedIn"
                url={profile?.linkedinUrl}
              />

              <LinkItem
                icon={<Globe size={17} />}
                label="Website"
                url={profile?.personalWebsiteUrl}
              />
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}

function ProfilePill({ icon, text }) {
  return (
    <div className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1.5 text-sm text-slate-600">
      <span className="text-indigo-700">{icon}</span>
      <span>{text}</span>
    </div>
  );
}

function InfoItem({ label, value }) {
  return (
    <div>
      <p className="text-xs font-bold uppercase tracking-wide text-slate-400">
        {label}
      </p>

      <p
        className={`mt-1 text-sm font-medium ${
          value ? "text-slate-800" : "text-slate-400"
        }`}
      >
        {value || "Not set"}
      </p>
    </div>
  );
}

function LinkItem({ icon, label, url }) {
  if (!url) {
    return (
      <div className="flex items-center gap-3 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-400">
        <span>{icon}</span>
        <span>{label} not added</span>
      </div>
    );
  }

  return (
    <a
      href={url}
      target="_blank"
      rel="noreferrer"
      className="flex items-center gap-3 rounded-2xl bg-indigo-50 px-4 py-3 text-sm font-semibold text-indigo-700 transition hover:bg-indigo-100"
    >
      <span>{icon}</span>
      <span>{label}</span>
    </a>
  );
}