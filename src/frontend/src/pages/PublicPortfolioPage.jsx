import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { API_BASE_URL } from "../api/apiConfig";
import { normalizeFetchErrorResponse } from "../utils/apiErrorUtils";

function PublicPortfolioPage() {
  const { username } = useParams();
  const navigate = useNavigate();
  const [portfolio, setPortfolio] = useState(null);
  const [currentUser, setCurrentUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [status, setStatus] = useState("loading");
  const [message, setMessage] = useState("");
  const [toast, setToast] = useState("");

  useEffect(() => { loadPortfolio(); checkAuthStatus(); }, [username]);

  async function getErrorMessage(response) {
    const error = await normalizeFetchErrorResponse(response);
    return error.message || "Something went wrong.";
  }

  async function checkAuthStatus() {
    try {
      const r = await fetch(`${API_BASE_URL}/me`, { credentials: "include" });
      setIsAuthenticated(r.ok);
      if (r.ok) setCurrentUser(await r.json());
    } catch { setIsAuthenticated(false); setCurrentUser(null); }
  }

  async function loadPortfolio() {
    if (!username) { setStatus("error"); setMessage("No username was provided."); return; }
    setStatus("loading");
    try {
      const r = await fetch(`${API_BASE_URL}/portfolios/${encodeURIComponent(username)}`);
      if (!r.ok) { setMessage(await getErrorMessage(r)); setStatus("error"); return; }
      setPortfolio(await r.json());
      setStatus("success");
    } catch (e) { console.error(e); setMessage("Failed to load portfolio."); setStatus("error"); }
  }

  async function copyPublicLink() {
    const link = `${window.location.origin}/portfolios/${encodeURIComponent(username || "")}`;

    try {
      await navigator.clipboard.writeText(link);
      setToast("Portfolio link copied.");
    } catch {
      setToast("Could not copy the link.");
    }
  }

  async function handleLogout() {
    try { await fetch(`${API_BASE_URL}/auth/logout`, { method: "POST", credentials: "include" }); }
    finally { navigate("/login", { replace: true }); }
  }

  if (status === "loading") return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-[#F7F1E8] text-sm text-slate-500">
      <div className="h-5 w-5 animate-spin rounded-xl border border-[#B9D8CC] border-t-[#2FA084]" />
      Loading portfolio…
    </div>
  );

  if (status === "error") {
    return (
      <div className="min-h-screen bg-[#F7F1E8]">
        <main className="flex min-h-[calc(100vh-60px)] items-center justify-center px-6">
          <style>{`
            @keyframes fadeUp { from { opacity:0; transform:translateY(10px); } to { opacity:1; transform:translateY(0); } }
            .fu { animation: fadeUp 0.4s both; }
          `}</style>
          <div className="fu max-w-sm rounded-xl border border-[#B9D8CC] bg-white p-8 text-center shadow-sm">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-[#EAF8F1]">
              <svg width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.75" viewBox="0 0 24 24" className="text-slate-500">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            <h1 className="text-xl font-black text-[#18332D]">Portfolio unavailable</h1>
            <p className="mt-2 text-sm text-slate-600">{message}</p>
            <button onClick={() => navigate(isAuthenticated ? "/portfolio" : "/login")}
              className="mt-6 rounded-xl bg-[#2FA084] px-5 py-2 text-sm font-semibold text-white transition-none hover:bg-[#1F6F5F]">
              {isAuthenticated ? "Back to portfolio" : "Sign in"}
            </button>
          </div>
        </main>
      </div>
    );
  }

  const repositories = portfolio?.repositories || [];
  const skills = Array.from(new Set(repositories.map((r) => r.primaryLanguage).filter(Boolean)));
  const totalStars = repositories.reduce((s, r) => s + (r.stars || 0), 0);
  const displayName = portfolio?.displayName || username;

  return (
    <div className="min-h-screen bg-[#F7F1E8] text-[#18332D]">
      <style>{`
        @keyframes fadeUp { from { opacity:0; transform:translateY(10px); } to { opacity:1; transform:translateY(0); } }
        .fu { animation: fadeUp 0.4s both; }
        .fu-1 { animation-delay:0.06s; } .fu-2 { animation-delay:0.12s; } .fu-3 { animation-delay:0.18s; }
      `}</style>

      <main className="mx-auto max-w-7xl space-y-6 px-6 py-12">
        {toast && (
          <div className="fu flex items-center gap-3 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] px-4 py-3 text-sm font-medium text-slate-700">
            <span className="h-1.5 w-1.5 rounded-xl bg-[#2FA084]" />
            {toast}
          </div>
        )}

        {/* Profile header */}
        <section className="fu overflow-hidden rounded-xl border border-[#B9D8CC] bg-white shadow-sm">
          <CoverImage src={portfolio?.coverImageUrl} />
          <div className="px-7 pb-7">
            <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
              <div className="flex items-end gap-5">
                <Avatar src={portfolio?.avatarUrl} name={displayName} overlap={Boolean(portfolio?.coverImageUrl)} />
                <div className="pb-1">
                  <h1 className="mt-0.5 text-2xl font-black text-[#18332D]">{displayName}</h1>
                  <p className="mt-0.5 text-sm text-slate-600">{portfolio?.headline || portfolio?.currentRole || "Software Engineering Student"}</p>
                </div>
              </div>
              <div className="flex flex-wrap items-end gap-2 pb-1">
                <button onClick={copyPublicLink} className="rounded-xl border border-[#B9D8CC] bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm transition-none hover:bg-[#F7F1E8]">
                  Copy link
                </button>
              </div>
            </div>
          </div>
        </section>

        {/* Stats + focus */}
        <section className="fu fu-1 grid gap-5 sm:grid-cols-4">
          <StatCard value={repositories.length} label="Projects" />
          <StatCard value={totalStars} label="Stars" />
          <StatCard value={skills.length} label="Languages" />
          <div className="sm:col-span-1 rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-500">Goal</p>
            <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">{portfolio?.careerGoal || "Building practical engineering projects"}</p>
          </div>
        </section>

        <section className="fu fu-1 rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center gap-2">
            {portfolio?.location && <Pill><PinIcon />{portfolio.location}</Pill>}
            {portfolio?.publicEmail && <LinkPill href={`mailto:${portfolio.publicEmail}`}>Email</LinkPill>}
            {portfolio?.githubUrl && <LinkPill href={portfolio.githubUrl}><GitHubIcon />GitHub</LinkPill>}
            {portfolio?.linkedinUrl && <LinkPill href={portfolio.linkedinUrl}><LinkedInIcon />LinkedIn</LinkPill>}
            {portfolio?.personalWebsiteUrl && <LinkPill href={portfolio.personalWebsiteUrl}><WebIcon />Website</LinkPill>}
          </div>
        </section>

        {/* Bio */}
        {portfolio?.bio && (
          <section className="fu fu-2 rounded-xl border border-[#B9D8CC] bg-white p-6 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-500">About</p>
            <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-700">{portfolio.bio}</p>
          </section>
        )}

        {/* Projects */}
        <section className="fu fu-2">
          <div className="mb-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-widest text-slate-500">Featured projects</p>
              <p className="mt-1 text-sm text-slate-600">
                {repositories.length} selected {repositories.length === 1 ? "project" : "projects"} · public repositories
              </p>
            </div>
          </div>

          {repositories.length === 0 ? (
            <div className="rounded-xl border border-[#B9D8CC] bg-white p-8 text-center text-sm text-slate-500">
              No projects selected yet.
            </div>
          ) : (
            <div className="grid gap-4 lg:grid-cols-2 2xl:grid-cols-3">
              {repositories.map((repo, index) => (
                <div key={repo.repositoryId} className="fu" style={{ animationDelay: `${0.12 + index * 0.03}s` }}>
                  <RepoCard repo={repo} />
                </div>
              ))}
            </div>
          )}
        </section>

        {/* Skills */}
        {skills.length > 0 && (
          <section className="fu fu-3 rounded-xl border border-[#B9D8CC] bg-white p-6 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-500">Languages & technologies</p>
            <div className="mt-4 flex flex-wrap gap-2">
              {skills.map((s) => (
                <span key={s} className="rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-1.5 text-sm font-medium text-slate-700">{s}</span>
              ))}
            </div>
          </section>
        )}
      </main>
    </div>
  );
}

function CoverImage({ src }) {
  if (!src) return <div className="h-24 bg-gradient-to-br from-[#EAF8F1] via-white to-[#FED7AA]/35" />;
  return <div className="h-48 sm:h-60"><img src={src} alt="" className="h-full w-full object-cover" /></div>;
}
function Avatar({ src, name, overlap = false }) {
  const margin = overlap ? "-mt-12" : "mt-5";
  return src
    ? <img src={src} alt={name} className={`h-20 w-20 ${margin} rounded-xl object-cover ring-4 ring-white shadow-sm`} />
    : <div className={`flex h-20 w-20 ${margin} items-center justify-center rounded-xl bg-[#2FA084] text-2xl font-black text-white ring-4 ring-white shadow-sm`}>{name?.[0]?.toUpperCase() || "U"}</div>;
}
function StatCard({ value, label }) {
  return (
    <div className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
      <p className="text-3xl font-black text-[#18332D]">{value}</p>
      <p className="mt-0.5 text-xs font-medium text-slate-500">{label}</p>
    </div>
  );
}
function Pill({ children }) { return <span className="inline-flex items-center gap-1.5 rounded-xl border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-1 text-xs font-medium text-slate-700">{children}</span>; }
function LinkPill({ href, children }) { return <a href={href} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1.5 rounded-xl border border-[#B9D8CC] bg-white px-3 py-1 text-xs font-semibold text-slate-700 shadow-sm transition-none hover:border-[#B9D8CC] hover:text-[#1F6F5F] hover:shadow-sm">{children}</a>; }
function RepoCard({ repo }) {
  return (
    <a href={repo.htmlUrl || "#"} target="_blank" rel="noreferrer"
      className="group flex min-h-48 flex-col rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm transition-none hover:border-[#B9D8CC] hover:shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <h3 className="font-bold text-[#18332D] group-hover:text-[#1F6F5F] transition-none">{repo.name}</h3>
        <span className="shrink-0 rounded-xl bg-[#EAF8F1] px-2.5 py-0.5 text-[10px] font-bold text-[#2FA084] ring-1 ring-[#B9D8CC]">repo</span>
      </div>
      <p className="mt-2.5 line-clamp-3 flex-1 text-sm leading-6 text-slate-600">{repo.description || "No description."}</p>
      <div className="mt-4 flex flex-wrap gap-1.5 border-t border-[#B9D8CC] pt-3 text-xs">
        {repo.primaryLanguage && <Tag>{repo.primaryLanguage}</Tag>}
        <Tag>★ {repo.stars || 0}</Tag>
        <Tag>⑂ {repo.forks || 0}</Tag>
      </div>
    </a>
  );
}
function Tag({ children }) { return <span className="rounded-xl bg-[#EAF8F1] px-2.5 py-0.5 font-medium text-slate-600">{children}</span>; }
function PinIcon() { return <svg width="10" height="10" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/><path strokeLinecap="round" strokeLinejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/></svg>; }
function GitHubIcon() { return <svg width="10" height="10" fill="currentColor" viewBox="0 0 24 24"><path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"/></svg>; }
function LinkedInIcon() { return <svg width="10" height="10" fill="currentColor" viewBox="0 0 24 24"><path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/></svg>; }
function WebIcon() { return <svg width="10" height="10" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9"/></svg>; }

export default PublicPortfolioPage;
