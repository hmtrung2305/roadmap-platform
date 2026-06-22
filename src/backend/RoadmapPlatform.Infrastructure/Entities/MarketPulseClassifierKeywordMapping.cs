using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class MarketPulseClassifierKeywordMapping
{
    public Guid MarketPulseClassifierKeywordMappingId { get; set; }

    public string Keyword { get; set; } = null!;

    public string Category { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public decimal Weight { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
