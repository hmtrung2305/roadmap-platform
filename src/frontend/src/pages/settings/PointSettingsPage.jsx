import { useEffect } from "react";
import { Clock3, Trophy } from "lucide-react";
import { toast } from "react-toastify";
import { useStreakStore } from "../../stores/useStreakStore";
import { FaCalendarCheck, FaFireAlt } from "react-icons/fa";

function StreakStatCard({ icon: Icon, title, value }) {
  return (
    <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-3">
        <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-50 text-indigo-700">
          <Icon size={20} />
        </div>

        <div>
          <p className="text-sm font-semibold text-slate-500">{title}</p>
          <p className="mt-1 text-2xl font-bold text-slate-900">{value}</p>
        </div>
      </div>

    </div>
  );
}

export default function PointsSettingsPage() {
  const streak = useStreakStore((state) => state.streak);
  const loading = useStreakStore((state) => state.loading);
  const tracking = useStreakStore((state) => state.tracking);
  const error = useStreakStore((state) => state.error);
  const fetchStreak = useStreakStore((state) => state.fetchStreak);
  const trackStreak = useStreakStore((state) => state.trackStreak);

  useEffect(() => {
    fetchStreak();
  }, [fetchStreak]);

  const handleTrackStreak = async () => {
    const data = await trackStreak();

    if (!data) {
      toast.error("Unable to complete daily streak.");
      return;
    }

    if (data.increasedToday) {
      toast.success("Daily streak completed successfully.");
      return;
    }

    toast.info("You have already completed today’s streak.");
  };

  const formatDateTime = (value) => {
    if (!value) return "No activity yet";

    return new Date(value).toLocaleString("en-US", {
      month: "short",
      day: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  if (loading) {
    return (
      <div className="mx-auto max-w-4xl">
        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <p className="text-sm text-slate-500">Loading points and streak...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
          Points and streak
        </h1>

        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-500">
          Track your daily learning consistency based on your current streak
          activity.
        </p>
      </div>

      {error && (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-3">
        <StreakStatCard
          icon={FaFireAlt}
          title="Current streak"
          value={streak?.currentStreak ?? 0}
        />

        <StreakStatCard
          icon={Trophy}
          title="Longest streak"
          value={streak?.longestStreak ?? 0}
        />

        <StreakStatCard
          icon={FaCalendarCheck}
          title="Today"
          value={streak?.isCompletedStreakToday ? "Completed" : "Pending"}
          
        />
      </div>

      <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-6 py-5">
          <h2 className="text-base font-bold text-slate-900">
            Daily check-in
          </h2>

          <p className="mt-1 text-sm leading-6 text-slate-500">
            Keep your learning streak alive by completing one activity each day.
          </p>
        </div>

        <div className="flex flex-col gap-5 px-6 py-5 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-4">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-50 text-indigo-700">
              <FaFireAlt size={20} />
            </div>

            <div>
              <h3 className="text-sm font-bold text-slate-900">
                {streak?.isCompletedStreakToday
                  ? "Completed today"
                  : "Daily check-in available"}
              </h3>

              <p className="mt-1 text-sm text-slate-500">
                {streak?.isCompletedStreakToday
                  ? "Come back tomorrow to continue your streak."
                  : "Complete today’s streak check-in now."}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={handleTrackStreak}
            disabled={tracking || streak?.isCompletedStreakToday}
            className="h-10 rounded-xl bg-indigo-700 px-5 text-sm font-semibold text-white shadow-lg shadow-indigo-700/20 transition hover:bg-indigo-800 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-400 disabled:shadow-none"
          >
            {tracking
              ? "Checking..."
              : streak?.isCompletedStreakToday
                ? "Completed"
                : "Check in"}
          </button>
        </div>
      </section>

      <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 px-6 py-5">
          <h2 className="text-base font-bold text-slate-900">
            Latest activity
          </h2>

          <p className="mt-1 text-sm leading-6 text-slate-500">
            Based on your latest recorded streak interaction.
          </p>
        </div>

        <div className="flex items-center justify-between gap-4 px-6 py-5">
          <div className="flex items-center gap-4">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-slate-600">
              <Clock3 size={20} />
            </div>

            <div>
              <h3 className="text-sm font-bold text-slate-900">
                Daily learning activity
              </h3>

              <p className="mt-1 text-sm text-slate-500">
                {formatDateTime(streak?.lastInteraction)}
              </p>
            </div>
          </div>

          {streak?.lastInteraction && (
            <span className="rounded-xl bg-emerald-50 px-3 py-2 text-sm font-bold text-indigo-700">
              +1 streak
            </span>
          )}
        </div>
      </section>
    </div>
  );
}