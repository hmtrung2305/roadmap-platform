import { ShieldCheck } from "lucide-react";
import EditPortfolioInfoTile from "./EditPortfolioInfoTile";

export default function EditPortfolioStatsGrid({ username, isGitHubLinked, selectedCount, totalStars }) {
  return (
    <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <EditPortfolioInfoTile icon={<ShieldCheck size={18} />} label="Visibility" value="Public" helper="Public preview enabled." />
      <EditPortfolioInfoTile label="Username" value={username || "Not set"} helper="Shown on portfolio." />
      <EditPortfolioInfoTile label="GitHub" value={isGitHubLinked ? "Linked" : "Not linked"} helper="Repository sync source." />
      <EditPortfolioInfoTile label="Featured" value={`${selectedCount} projects`} helper={`${totalStars} stars imported.`} />
    </section>
  );
}
