namespace RoadmapPlatform.Tests;

public sealed class MarketPulseTopCvSchemaTests
{
    [Fact]
    public void ConsolidatedMigrationEnforcesTopCvOnlyAndNewOperationalNames()
    {
        var root = FindRepositoryRoot();
        var sql = File.ReadAllText(Path.Combine(
            root,
            "database",
            "migrations",
            "039-market-pulse-topcv-consolidated.sql"));

        Assert.Contains("non-TopCV job sources", sql, StringComparison.Ordinal);
        Assert.Contains("market_pulse_import_run", sql, StringComparison.Ordinal);
        Assert.Contains("market_pulse_import_failure", sql, StringComparison.Ordinal);
        Assert.Contains("market_pulse_publication_history_state", sql, StringComparison.Ordinal);
        Assert.Contains("market_pulse_refresh_operation", sql, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE IF EXISTS public.market_pulse_daily_observation", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS uq_job_posting_external_id", sql, StringComparison.Ordinal);
        Assert.Contains("role.role_name = 'admin'", sql, StringComparison.Ordinal);
        Assert.Contains("permission.permission_name = 'market_pulse.view.catalog'", sql, StringComparison.Ordinal);
        Assert.Contains("COALESCE(lower(trim(source_name)), '') <> 'topcv'", sql, StringComparison.Ordinal);
        Assert.Contains("<null-or-blank>", sql, StringComparison.Ordinal);
        Assert.Contains("AT TIME ZONE 'Asia/Ho_Chi_Minh'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ix_job_posting_experience_filters", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaHasNoRuntimeSourceOrObservationTables()
    {
        var root = FindRepositoryRoot();
        var sql = File.ReadAllText(Path.Combine(root, "database", "schema.sql"));

        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS public.job_portal_source", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS public.market_pulse_daily_observation", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS public.market_pulse_source_health", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS public.market_pulse_import_run", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS public.market_pulse_refresh_operation", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS public.market_pulse_publication_history_state", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.market_pulse_pipeline_run", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("external_id varchar(120) NOT NULL UNIQUE", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS uq_job_posting_external_id", sql, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "database")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
