using AiCvMatch.Api.Services.Analysis;

namespace AiCvMatch.Api.Tests.Services.Analysis;

public sealed class MatchAnalysisResponseParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsNormalizedResult()
    {
        const string json = """
            {
              "matchScore": 150,
              "matchedSkills": [" C# ", "c#", "ASP.NET Core"],
              "skillGaps": [" Azure "],
              "recommendations": ["Add metrics"]
            }
            """;

        var result = MatchAnalysisResponseParser.Parse(json);

        Assert.Equal(100, result.MatchScore);
        Assert.Equal(["C#", "ASP.NET Core"], result.MatchedSkills);
        Assert.Equal(["Azure"], result.SkillGaps);
        Assert.Equal(["Add metrics"], result.Recommendations);
    }

    [Fact]
    public void Parse_MarkdownFencedJson_ReturnsResult()
    {
        const string json = """
            ```json
            {
              "matchScore": 72,
              "matchedSkills": ["REST APIs"],
              "skillGaps": [],
              "recommendations": ["Highlight API work"]
            }
            ```
            """;

        var result = MatchAnalysisResponseParser.Parse(json);

        Assert.Equal(72, result.MatchScore);
        Assert.Equal(["REST APIs"], result.MatchedSkills);
    }

    [Fact]
    public void Parse_EmptyString_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => MatchAnalysisResponseParser.Parse(""));

        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        Assert.Throws<System.Text.Json.JsonException>(
            () => MatchAnalysisResponseParser.Parse("{ not-json"));
    }

    [Fact]
    public void Parse_NegativeScore_ClampsToZero()
    {
        const string json = """
            {
              "matchScore": -10,
              "matchedSkills": [],
              "skillGaps": [],
              "recommendations": []
            }
            """;

        var result = MatchAnalysisResponseParser.Parse(json);

        Assert.Equal(0, result.MatchScore);
    }
}
