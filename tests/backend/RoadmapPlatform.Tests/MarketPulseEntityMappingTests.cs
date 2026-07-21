using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests;

public sealed class MarketPulseEntityMappingTests
{
    [Fact]
    public void PostDateTextMapsToPostDateTextColumn()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=market_pulse_model_test;Username=test;Password=test",
                npgsqlOptions => npgsqlOptions.UseVector())
            .Options;

        using var context = new ApplicationDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(JobPosting));
        var property = entityType?.FindProperty(nameof(JobPosting.PostDateText));
        var table = StoreObjectIdentifier.Table("job_posting", schema: null);

        Assert.NotNull(property);
        Assert.Equal("post_date_text", property.GetColumnName(table));
        Assert.Equal(80, property.GetMaxLength());
    }

    [Fact]
    public void PipelineAndFailureMapToConsolidatedRunKey()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=market_pulse_model_test;Username=test;Password=test",
                npgsqlOptions => npgsqlOptions.UseVector())
            .Options;

        using var context = new ApplicationDbContext(options);
        var pipeline = context.Model.FindEntityType(typeof(MarketPulsePipelineRun));
        var failure = context.Model.FindEntityType(typeof(MarketPulseFailedItem));
        var pipelineTable = StoreObjectIdentifier.Table("market_pulse_pipeline_run", schema: null);
        var failureTable = StoreObjectIdentifier.Table("market_pulse_import_failure", schema: null);

        Assert.Equal("market_pulse_pipeline_run", pipeline?.GetTableName());
        Assert.Equal(
            "operation_type",
            pipeline?.FindProperty(nameof(MarketPulsePipelineRun.OperationType))?.GetColumnName(pipelineTable));
        Assert.Equal(
            "market_pulse_pipeline_run_id",
            failure?.FindProperty(nameof(MarketPulseFailedItem.MarketPulsePipelineRunId))?.GetColumnName(failureTable));
    }
}
