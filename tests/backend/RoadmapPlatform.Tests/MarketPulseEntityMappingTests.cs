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
}
