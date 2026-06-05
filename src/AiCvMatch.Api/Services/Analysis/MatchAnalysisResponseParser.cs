using System.Text.Json;
using System.Text.Json.Serialization;
using AiCvMatch.Api.Models;

namespace AiCvMatch.Api.Services.Analysis;

public static class MatchAnalysisResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static CvMatchResult Parse(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("The AI provider returned an empty analysis response.");
        }

        var trimmed = jsonText.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = ExtractJsonFromMarkdownFence(trimmed);
        }

        var result = JsonSerializer.Deserialize<CvMatchResult>(trimmed, SerializerOptions);
        if (result is null)
        {
            throw new InvalidOperationException("The AI provider returned analysis JSON that could not be parsed.");
        }

        return NormalizeResult(result);
    }

    private static string ExtractJsonFromMarkdownFence(string value)
    {
        var lines = value.Split('\n');
        var jsonLines = lines
            .Skip(1)
            .TakeWhile(line => !line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            .ToArray();

        return string.Join('\n', jsonLines).Trim();
    }

    private static CvMatchResult NormalizeResult(CvMatchResult result)
    {
        return new CvMatchResult
        {
            MatchScore = Math.Clamp(result.MatchScore, 0, 100),
            MatchedSkills = NormalizeList(result.MatchedSkills),
            SkillGaps = NormalizeList(result.SkillGaps),
            Recommendations = NormalizeList(result.Recommendations)
        };
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
