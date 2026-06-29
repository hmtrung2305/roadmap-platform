using System.Text.Json;
using System.Text.Json.Nodes;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapNodeContent
{
    public static JsonElement? ToJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static List<string> DeserializeStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializeStringArray(IEnumerable<string>? values)
    {
        var normalized = NormalizeList(values);
        return JsonSerializer.Serialize(normalized);
    }

    public static string UpdateMetadata(string? existingMetadata, string? nodeType, UpdateRoadmapNodeGuideRequestDto? guide)
    {
        if (guide == null)
        {
            return string.IsNullOrWhiteSpace(existingMetadata) ? "{}" : existingMetadata;
        }

        var metadata = ParseObject(existingMetadata);
        var normalizedNodeType = ContentManagerRoadmapText.NormalizeOptionalText(nodeType)?.ToLowerInvariant();

        switch (normalizedNodeType)
        {
            case "project":
                RemoveProjectGuideKeys(metadata);
                SetText(metadata, "whatToBuild", guide.WhatToBuild);
                SetStringArray(metadata, "buildSteps", guide.BuildSteps);
                break;
            case "checkpoint":
                RemoveCheckpointGuideKeys(metadata);
                SetText(metadata, "reviewFocus", guide.ReviewFocus);
                SetStringArray(metadata, "reviewCriteria", guide.ReviewCriteria);
                break;
            default:
                return string.IsNullOrWhiteSpace(existingMetadata) ? "{}" : existingMetadata;
        }

        return metadata.ToJsonString();
    }

    private static void RemoveProjectGuideKeys(JsonObject metadata)
    {
        foreach (var key in new[]
        {
            "projectBrief",
            "suggestedSteps",
            "expectedEvidence",
            "deliverables",
            "artifacts",
            "outputs",
            "reviewEvidence",
            "reviewFocus"
        })
        {
            metadata.Remove(key);
        }
    }

    private static void RemoveCheckpointGuideKeys(JsonObject metadata)
    {
        foreach (var key in new[]
        {
            "expectedEvidence",
            "reviewEvidence",
            "reviewQuestions",
            "nextActions",
            "followUpActions",
            "followUps",
            "nextSteps"
        })
        {
            metadata.Remove(key);
        }
    }

    private static JsonObject ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(json)?.AsObject() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void SetText(JsonObject metadata, string key, string? value)
    {
        var normalized = ContentManagerRoadmapText.NormalizeOptionalText(value);
        if (normalized == null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalized;
    }

    private static void SetStringArray(JsonObject metadata, string key, IEnumerable<string>? values)
    {
        if (values == null)
        {
            return;
        }

        var normalized = NormalizeList(values);
        if (normalized.Count == 0)
        {
            metadata.Remove(key);
            return;
        }

        var array = new JsonArray();
        foreach (var item in normalized)
        {
            array.Add(item);
        }

        metadata[key] = array;
    }

    private static List<string> NormalizeList(IEnumerable<string>? values)
    {
        if (values == null)
        {
            return [];
        }

        return values
            .Select(ContentManagerRoadmapText.NormalizeOptionalText)
            .Where(value => value != null)
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
