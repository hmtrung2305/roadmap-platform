import { Eye, EyeOff, ShieldCheck } from "lucide-react";
import EditPortfolioInfoTile from "./EditPortfolioInfoTile";

export default function EditPortfolioStatsGrid({
  username,
  isGitHubLinked,
  isPortfolioPublic,
  selectedCount,
  totalStars,
  onManageVisibility,
}) {
  return (
    <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <EditPortfolioInfoTile
        icon={isPortfolioPublic ? <Eye size={18} /> : <EyeOff size={18} />}
        label="Visibility"
        value={isPortfolioPublic ? "Public" : "Private"}
        helper={
          isPortfolioPublic
            ? "Public visitors can view your portfolio."
            : "Public visitors cannot view your portfolio yet."
        }
        actionLabel="Manage visibility"
        onClick={onManageVisibility}
      />
      <EditPortfolioInfoTile label="Username" value={username || "Not set"} helper="Shown on portfolio." />
      <EditPortfolioInfoTile label="GitHub" value={isGitHubLinked ? "Linked" : "Not linked"} helper="Repository sync source." />
      <EditPortfolioInfoTile icon={<ShieldCheck size={18} />} label="Featured" value={`${selectedCount} projects`} helper={`${totalStars} stars imported.`} />
    </section>
  );
}
