using System.Text.RegularExpressions;

namespace RoadmapPlatform.Tests;

public sealed class SqlSeedContractTests
{
    private static readonly Regex IncludeRegex = new(
        @"^\s*\\i\s+(?<path>.+?)\s*(?:--.*)?$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void SqlIncludesPointToExistingFiles()
    {
        var root = FindRepositoryRoot();
        var missingIncludes = Directory
            .EnumerateFiles(Path.Combine(root, "database"), "*.sql", SearchOption.AllDirectories)
            .SelectMany(file => FindMissingIncludes(root, file))
            .ToList();

        Assert.Empty(missingIncludes);
    }

    [Fact]
    public void LearningModuleSeedDefinesPublishedModulesLessonsAndQuiz()
    {
        var root = FindRepositoryRoot();
        var seedPath = Path.Combine(
            root,
            "database",
            "seeds",
            "learning-modules",
            "published-learning-modules.seed.sql");

        var seedSql = File.ReadAllText(seedPath);

        Assert.Contains("INSERT INTO public.skill_module", seedSql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO public.skill_module_lesson", seedSql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO public.skill_module_quiz", seedSql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO public.skill_module_quiz_question", seedSql, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO public.skill_module_quiz_option", seedSql, StringComparison.Ordinal);
        Assert.DoesNotContain("frontend-roadmap-primary-skill-preview-v3-focused.seed.sql", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SkillGapCategorySeedRepairsLegacyUniqueConstraintBeforeUpsert()
    {
        var root = FindRepositoryRoot();
        var seedPath = Path.Combine(
            root,
            "database",
            "seeds",
            "assessments",
            "populate-skill-gap-category-config.seed.sql");

        var seedSql = File.ReadAllText(seedPath);

        Assert.Contains("DELETE FROM public.skill_gap_category_config", seedSql, StringComparison.Ordinal);
        Assert.Contains("DROP CONSTRAINT IF EXISTS uq_skill_gap_category", seedSql, StringComparison.Ordinal);
        Assert.Contains("ADD CONSTRAINT uq_skill_gap_category", seedSql, StringComparison.Ordinal);
        Assert.Contains("UNIQUE (roadmap_id, roadmap_version_id, category_name)", seedSql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void PostDateConfidenceMigrationBackfillsBeforeEnforcingNotNull()
    {
        var root = FindRepositoryRoot();
        var migrationPath = Path.Combine(
            root,
            "database",
            "migrations",
            "039-market-pulse-topcv-consolidated.sql");

        var migrationSql = File.ReadAllText(migrationPath);
        var backfillIndex = migrationSql.IndexOf(
            "UPDATE public.job_posting",
            StringComparison.Ordinal);
        var notNullIndex = migrationSql.IndexOf(
            "ALTER COLUMN post_date_confidence SET NOT NULL",
            StringComparison.Ordinal);

        Assert.True(backfillIndex >= 0);
        Assert.True(notNullIndex > backfillIndex);
        Assert.Contains("ELSE 'unknown'", migrationSql, StringComparison.Ordinal);
        Assert.Contains(
            "post_date_confidence IN ('exact', 'relative', 'unknown')",
            migrationSql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ConsolidatedMigrationAndSchemaDefinePublicationHistoryContract()
    {
        var root = FindRepositoryRoot();
        var migrationSql = File.ReadAllText(Path.Combine(
            root,
            "database",
            "migrations",
            "039-market-pulse-topcv-consolidated.sql"));
        var schemaSql = File.ReadAllText(Path.Combine(root, "database", "schema.sql"));

        Assert.Contains("public.market_pulse_publication_history_state", migrationSql, StringComparison.Ordinal);
        Assert.Contains("public.market_pulse_refresh_operation", migrationSql, StringComparison.Ordinal);
        Assert.Contains("public.market_pulse_publication_history_state", schemaSql, StringComparison.Ordinal);
        Assert.Contains("public.market_pulse_refresh_operation", schemaSql, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "CREATE TABLE IF NOT EXISTS public.market_pulse_daily_observation",
            schemaSql,
            StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindMissingIncludes(string root, string sqlFile)
    {
        var sql = File.ReadAllText(sqlFile);

        foreach (Match match in IncludeRegex.Matches(sql))
        {
            var includePath = match.Groups["path"].Value.Trim().Trim('"', '\'').Replace('\\', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(root, includePath);

            if (!File.Exists(fullPath))
            {
                yield return $"{Path.GetRelativePath(root, sqlFile)} -> {includePath}";
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "database", "schema.sql")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }
}
