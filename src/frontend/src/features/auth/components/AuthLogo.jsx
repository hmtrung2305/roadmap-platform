export default function AuthLogo({
  compact = false,
  showTagline = true,
}) {
  const logoSize = compact ? "h-10 w-10" : "h-11 w-11";
  const titleSize = compact
    ? "text-[clamp(18px,1.3vw,21px)]"
    : "text-[22px]";

  return (
    <div className="inline-flex min-w-0 items-center gap-3">
      {/* Logo icon */}
      <div
        className={`flex shrink-0 items-center justify-center rounded-[13px] bg-gradient-to-br from-[#789F8B] to-[#456D5B] shadow-[0_8px_20px_rgba(55,92,75,0.22)] ${logoSize}`}
      >
        <svg
          width={compact ? 19 : 21}
          height={compact ? 19 : 21}
          viewBox="0 0 24 24"
          fill="none"
          aria-hidden="true"
        >
          <rect
            x="3"
            y="3"
            width="7.5"
            height="7.5"
            rx="2"
            fill="#FFFFFF"
            opacity="0.76"
          />

          <rect
            x="13.5"
            y="13.5"
            width="7.5"
            height="7.5"
            rx="2"
            fill="#FFFFFF"
          />

          <path
            d="M10.5 6.75H15.3V10.3"
            stroke="#FFFFFF"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          <path
            d="M17.25 13.5V10.3H13"
            stroke="#FFFFFF"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </div>

      {/* Logo text */}
      <div className="flex min-w-0 flex-col items-start justify-center text-left">
        <p
          className={`m-0 w-full text-left font-black leading-none tracking-[-0.04em] text-[#142D23] ${titleSize}`}
        >
          TechMap
        </p>

        {showTagline && (
          <p className="m-0 mt-[7px] w-full whitespace-nowrap text-left text-[clamp(7px,0.52vw,9px)] font-semibold uppercase leading-none tracking-[0.22em] text-[#397458]">
            Engineer your future
          </p>
        )}
      </div>
    </div>
  );
}