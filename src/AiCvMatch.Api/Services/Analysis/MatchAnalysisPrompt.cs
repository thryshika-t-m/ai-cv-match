namespace AiCvMatch.Api.Services.Analysis;

public static class MatchAnalysisPrompt
{
    private const int MaxCvCharacters = 30_000;
    private const int MaxJobDescriptionCharacters = 10_000;

    public static string Build(string cvText, string jobDescription)
    {
        var trimmedCv = TrimToLength(cvText, MaxCvCharacters);
        var trimmedJobDescription = TrimToLength(jobDescription, MaxJobDescriptionCharacters);

        return $$"""
            You are an expert technical recruiter. Compare the candidate CV text to the job description.

            Return ONLY valid JSON with this exact shape (no markdown, no extra keys):
            {
              "matchScore": <integer 0-100>,
              "matchedSkills": [<strings>],
              "skillGaps": [<strings>],
              "recommendations": [<strings>]
            }

            Rules:
            - matchScore reflects overall fit for the role (0 = no fit, 100 = excellent fit).
            - matchedSkills: skills or qualifications from the job description that are clearly supported by the CV.
            - skillGaps: important job requirements missing or weak in the CV.
            - recommendations: concise, actionable advice for the candidate (3-6 items).
            - Use specific skill names; avoid vague phrases.
            - If the CV text is sparse, still infer carefully and note uncertainty in recommendations.

            JOB DESCRIPTION:
            {{trimmedJobDescription}}

            CV TEXT:
            {{trimmedCv}}
            """;
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
