import { useNavigate } from "react-router-dom";
import AuthLogo from "../../features/auth/components/AuthLogo";

const FOOTER_GROUPS = [
  {
    title: "Learning",
    items: [
      {
        label: "Roadmaps",
        path: "/roadmaps",
      },
      {
        label: "Learning modules",
        path: "/learning-modules",
      },
      {
        label: "Browse modules",
        path: "/learning-modules/browse",
      },
    ],
  },
  {
    title: "Platform",
    items: [
      {
        label: "Public Portfolio",
        path: "/portfolio",
      },
      {
        label: "Market Pulse",
        path: "/market-pulse",
      },
    ],
  },
  {
    title: "Support",
    items: [
      {
        label: "Settings",
        path: "/settings",
      },
      {
        label: "Profile Settings",
        path: "/settings/profile",
      },
      {
        label: "Terms of Service",
        path: "",
      },
    ],
  },
];

export default function Footer() {
  const navigate = useNavigate();

  return (
    <footer className="relative w-full overflow-hidden text-[#2C3D36]">
      {/* Nền chính */}
      <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(118deg,#F5F1E9_0%,#FAF8F3_36%,#F1F4EC_72%,#F6F1E7_100%)]" />

      {/* Hiệu ứng sáng phía trên */}
      <div className="pointer-events-none absolute inset-x-0 top-0 z-[2] h-14 bg-[linear-gradient(180deg,rgba(255,255,255,0.88)_0%,rgba(250,248,243,0.62)_34%,rgba(245,241,233,0.22)_68%,rgba(245,241,233,0)_100%)] blur-[8px]" />

      {/* Đường sáng phía trên */}
      <div className="pointer-events-none absolute inset-x-[3%] top-0 z-[3] h-px bg-gradient-to-r from-transparent via-white/80 to-transparent" />

      {/* Bóng chuyển tiếp với section phía trên */}
      <div className="pointer-events-none absolute left-1/2 top-0 z-[1] h-10 w-[88%] -translate-x-1/2 rounded-[50%] bg-[#DCD8CF]/25 blur-2xl" />

      {/* Các vùng màu trang trí */}
      <div className="pointer-events-none absolute -left-28 -top-28 h-[300px] w-[300px] rounded-full bg-white/75 blur-[85px]" />

      <div className="pointer-events-none absolute left-[36%] top-[-160px] h-[320px] w-[320px] rounded-full bg-[#EEE6D8]/65 blur-[100px]" />

      <div className="pointer-events-none absolute -bottom-32 -right-16 h-[350px] w-[350px] rounded-full bg-[#E0E9D8]/45 blur-[110px]" />

      <div className="pointer-events-none absolute bottom-[-180px] left-[52%] h-[300px] w-[300px] rounded-full bg-[#F5EBDD]/55 blur-[95px]" />

      {/* Nội dung footer */}
      <div className="relative z-10 w-full px-6 pb-6 pt-8 sm:px-9 lg:px-12 lg:pb-7 lg:pt-9 xl:px-16">
        <div className="pl-[5px]">
          <div
            className="
              grid gap-8
              lg:grid-cols-[minmax(280px,0.82fr)_minmax(0,2.18fr)]
              lg:gap-6
              xl:grid-cols-[minmax(300px,0.84fr)_minmax(0,2.16fr)]
              xl:gap-8
            "
          >
            {/* Phần thương hiệu */}
            <div className="w-full max-w-[410px]">
              <button
                type="button"
                onClick={() => navigate("/roadmaps")}
                aria-label="Go to TechMap roadmaps"
                className="inline-flex rounded-xl text-left transition duration-200 hover:opacity-80 focus:outline-none focus-visible:ring-2 focus-visible:ring-[#678A74] focus-visible:ring-offset-2"
              >
                <AuthLogo compact />
              </button>

              <div className="mt-[18px]">
                <p className=" text-[14.5px] font-medium leading-[1.7] text-[#626861]">
                  Build a role-based learning path, track your repositories,
                  develop practical skills, and publish a portfolio that proves
                  your work.
                </p>

                <div className="mt-3.5 inline-flex items-center gap-2 rounded-full border border-[#E3DBCE] bg-[#FFFCF7]/75 px-3.5 py-1.5 shadow-[0_5px_16px_rgba(92,77,55,0.05)] backdrop-blur-md">
                  <span className="h-1.5 w-1.5 rounded-full bg-[#6F9279] shadow-[0_0_0_3px_rgba(111,146,121,0.12)]" />

                  <span className="text-[9px] font-extrabold uppercase tracking-[0.18em] text-[#627A69]">
                    Learn · Build · Prove
                  </span>
                </div>
              </div>
            </div>

            {/* Các nhóm liên kết */}
            <div
              className="
                grid grid-cols-2 gap-x-10 gap-y-7
                sm:grid-cols-3
                lg:border-l lg:border-[#DDD6CA]/75
                lg:pl-13
                xl:pl-18
              "
            >
              {FOOTER_GROUPS.map((group) => (
                <FooterGroup
                  key={group.title}
                  title={group.title}
                  items={group.items}
                  navigate={navigate}
                />
              ))}
            </div>
          </div>

          {/* Thanh cuối footer */}
          <div className="mt-5 border-t border-[#DDD6CA]/80 pt-4">
            <div className="flex flex-col gap-3 text-[11px] font-semibold text-[#747870] sm:flex-row sm:items-center sm:justify-between">
              <p>© 2026 TechMap. All rights reserved.</p>

              <div className="flex flex-wrap items-center gap-x-5 gap-y-2">
                <FooterBottomLink
                  label="Settings"
                  onClick={() => navigate("/settings")}
                />

                <FooterBottomLink
                  label="Profile Settings"
                  onClick={() => navigate("/settings/profile")}
                />

                <FooterBottomLink
                  label="Terms of Service"
                  disabled
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
}

function FooterGroup({ title, items, navigate }) {
  return (
    <div className="min-w-0">
      <div className="flex items-center gap-2.5">
        <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-gradient-to-br from-[#8CAD94] to-[#577762] shadow-[0_0_0_3px_rgba(111,146,121,0.1)]" />

        <h3 className="text-[15px] font-extrabold leading-none tracking-[-0.01em] text-[#34473E]">
          {title}
        </h3>
      </div>

      <div className="mt-3.5 space-y-1">
        {items.map(({ label, path }) => {
          const isAvailable = Boolean(path);

          return (
            <button
              key={label}
              type="button"
              disabled={!isAvailable}
              onClick={() => {
                if (isAvailable) {
                  navigate(path);
                }
              }}
              className={`group flex w-fit items-center rounded-lg py-1.5 text-left text-[13px] font-medium transition-all duration-200 ${
                isAvailable
                  ? "text-[#666B65] hover:translate-x-1 hover:text-[#597362]"
                  : "cursor-default text-[#A6A49E]"
              }`}
            >
              <span
                className={`mr-0 h-[2px] w-0 rounded-full bg-gradient-to-r from-[#6E8E78] to-[#B5A683] transition-all duration-200 ${
                  isAvailable
                    ? "group-hover:mr-2 group-hover:w-3"
                    : ""
                }`}
              />

              <span>{label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

function FooterBottomLink({
  label,
  onClick,
  disabled = false,
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className={`transition-colors duration-200 ${disabled
        ? "cursor-default text-[#AAA8A2]"
        : "text-[#747870] hover:text-[#597362]"
        }`}
    >
      {label}
    </button>
  );
}