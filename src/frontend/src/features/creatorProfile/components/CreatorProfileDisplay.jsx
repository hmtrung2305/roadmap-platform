import { useEffect } from "react";
import { createPortal } from "react-dom";
import { Globe2, UserRound, X } from "lucide-react";
import { FaGithub, FaLinkedin } from "react-icons/fa";

function getInitials(name) {
  return (
    String(name || "")
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join("") || "CP"
  );
}

function CreatorAvatar({ creatorProfile, size = "md" }) {
  const dimensionClass =
    size === "sm" ? "h-6 w-6 text-[9px]" : "h-12 w-12 text-xs";

  if (creatorProfile?.avatarUrl) {
    return (
      <img
        src={creatorProfile.avatarUrl}
        alt={`${creatorProfile.displayName} avatar`}
        className={`${dimensionClass} shrink-0 rounded-full border border-[#B9D8CC] object-cover`}
      />
    );
  }

  return (
    <span
      className={`${dimensionClass} grid shrink-0 place-items-center rounded-full border border-[#B9D8CC] bg-[#EAF8F1] font-black text-[#1F6F5F]`}
      aria-hidden="true"
    >
      {size === "sm" ? (
        getInitials(creatorProfile?.displayName)
      ) : (
        <UserRound size={20} />
      )}
    </span>
  );
}

function CreatorLinks({ creatorProfile }) {
  const links = [
    { href: creatorProfile.githubUrl, label: "GitHub", icon: FaGithub },
    { href: creatorProfile.linkedinUrl, label: "LinkedIn", icon: FaLinkedin },
    { href: creatorProfile.personalWebsiteUrl, label: "Website", icon: Globe2 },
  ].filter((item) => item.href);

  if (links.length === 0) return null;

  return (
    <div className="mt-3 flex flex-wrap gap-2">
      {links.map(({ href, label, icon: Icon }) => (
        <a
          key={label}
          href={href}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1.5 rounded-md border border-[#B9D8CC] bg-[#F7F1E8]/70 px-2.5 py-1 text-xs font-extrabold text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#EAF8F1]"
        >
          <Icon size={13} aria-hidden="true" />
          {label}
        </a>
      ))}
    </div>
  );
}

export function CreatorByline({
  creatorProfile,
  label = "Created by",
  className = "",
  showHeadline = false,
  interactive = false,
}) {
  if (!creatorProfile) return null;

  return (
    <div
      className={`flex min-w-0 items-center gap-2 text-xs font-semibold text-slate-500 ${className}`}
    >
      <CreatorAvatar creatorProfile={creatorProfile} size="sm" />
      <span className="min-w-0 truncate">
        {label}{" "}
        <span
          className={`font-extrabold text-[#1F6F5F] ${
            interactive
              ? "transition group-hover/creator:underline group-hover/creator:decoration-[#8CC9B4] group-hover/creator:underline-offset-4"
              : ""
          }`}
        >
          {creatorProfile.displayName}
        </span>
        {showHeadline && creatorProfile.headline ? (
          <span className="text-slate-400"> · {creatorProfile.headline}</span>
        ) : null}
      </span>
    </div>
  );
}

export function CreatorProfileCard({
  creatorProfile,
  label = "Created by",
  className = "",
}) {
  if (!creatorProfile) return null;

  return (
    <section
      className={`rounded-lg border border-[#B9D8CC] bg-white p-4 shadow-sm ${className}`}
    >
      <p className="text-[11px] font-black uppercase tracking-[0.14em] text-[#1F6F5F]">
        {label}
      </p>

      <div className="mt-3 flex items-start gap-3">
        <CreatorAvatar creatorProfile={creatorProfile} />

        <div className="min-w-0 flex-1">
          <h3 className="truncate text-sm font-black text-[#18332D]">
            {creatorProfile.displayName}
          </h3>

          {creatorProfile.headline && (
            <p className="mt-0.5 text-xs font-bold text-slate-600">
              {creatorProfile.headline}
            </p>
          )}

          {creatorProfile.bio && (
            <p className="mt-2 whitespace-pre-line text-sm font-medium leading-6 text-slate-700">
              {creatorProfile.bio}
            </p>
          )}

          <CreatorLinks creatorProfile={creatorProfile} />
        </div>
      </div>
    </section>
  );
}

export function CreatorProfileModal({
  creatorProfile,
  onClose,
  label = "Roadmap creator",
}) {
  useEffect(() => {
    if (!creatorProfile) return undefined;

    const handleKeyDown = (event) => {
      if (event.key === "Escape") {
        onClose?.();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [creatorProfile, onClose]);

  if (!creatorProfile || typeof document === "undefined") return null;

  return createPortal(
    <div
      className="fixed inset-0 z-[120] flex items-center justify-center bg-[#18332D]/45 px-4 py-8 backdrop-blur-[2px]"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose?.();
        }
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={`${creatorProfile.displayName} creator profile`}
        className="relative w-full max-w-md rounded-2xl border border-[#B9D8CC] bg-[#F7F1E8] p-3 shadow-2xl"
      >
        <button
          type="button"
          aria-label="Close creator profile"
          onClick={onClose}
          className="absolute right-5 top-5 z-10 grid h-8 w-8 place-items-center rounded-full border border-[#B9D8CC] bg-white text-slate-500 transition hover:border-[#2FA084] hover:text-[#1F6F5F]"
        >
          <X size={16} />
        </button>

        <CreatorProfileCard
          creatorProfile={creatorProfile}
          label={label}
          className="border-0 bg-white pr-12 shadow-none"
        />
      </div>
    </div>,
    document.body,
  );
}
