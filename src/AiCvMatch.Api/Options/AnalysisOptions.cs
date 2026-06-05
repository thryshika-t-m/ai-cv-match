namespace AiCvMatch.Api.Options;

public sealed class AnalysisOptions
{
    public const string SectionName = "Analysis";

    public string Provider { get; set; } = AnalysisProviders.Ollama;
}

public static class AnalysisProviders
{
    public const string Ollama = "Ollama";
    public const string Groq = "Groq";
    public const string Gemini = "Gemini";
}
