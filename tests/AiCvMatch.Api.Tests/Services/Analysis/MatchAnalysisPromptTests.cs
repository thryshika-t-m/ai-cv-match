using AiCvMatch.Api.Services.Analysis;

namespace AiCvMatch.Api.Tests.Services.Analysis;

public sealed class MatchAnalysisPromptTests
{
    [Fact]
    public void Build_IncludesCvAndJobDescription()
    {
        const string cvText = "Experienced .NET developer with REST API experience.";
        const string jobDescription = "Looking for a Senior .NET developer.";

        var prompt = MatchAnalysisPrompt.Build(cvText, jobDescription);

        Assert.Contains(cvText, prompt);
        Assert.Contains(jobDescription, prompt);
        Assert.Contains("matchScore", prompt);
        Assert.Contains("matchedSkills", prompt);
    }

    [Fact]
    public void Build_TruncatesVeryLongInputs()
    {
        var cvText = new string('C', 35_000);
        var jobDescription = new string('J', 15_000);

        var prompt = MatchAnalysisPrompt.Build(cvText, jobDescription);

        Assert.Contains(new string('C', 30_000), prompt);
        Assert.DoesNotContain(new string('C', 30_001), prompt);
        Assert.Contains(new string('J', 10_000), prompt);
        Assert.DoesNotContain(new string('J', 10_001), prompt);
    }
}
