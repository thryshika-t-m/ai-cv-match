namespace AiCvMatch.Api.Options;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama-3.1-8b-instant";
    public int MaxOutputTokens { get; set; } = 2048;
}
