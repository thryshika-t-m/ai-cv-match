using AiCvMatch.Api.Models;

namespace AiCvMatch.Api.Services;

public interface IMatchAnalysisService
{
    Task<CvMatchResult> AnalyzeAsync(
        string cvText,
        string jobDescription,
        CancellationToken cancellationToken = default);
}
