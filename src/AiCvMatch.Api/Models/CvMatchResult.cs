namespace AiCvMatch.Api.Models;

public sealed class CvMatchResult
{
    public int MatchScore { get; set; }
    public IReadOnlyList<string> MatchedSkills { get; set; } = [];
    public IReadOnlyList<string> SkillGaps { get; set; } = [];
    public IReadOnlyList<string> Recommendations { get; set; } = [];
}
