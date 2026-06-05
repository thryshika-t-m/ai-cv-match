using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services;

namespace AiCvMatch.Api.Extensions;

public static class AnalysisServiceExtensions
{
    public static IServiceCollection AddMatchAnalysisProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AnalysisOptions>(configuration.GetSection(AnalysisOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));

        var provider = configuration[$"{AnalysisOptions.SectionName}:Provider"]
            ?? AnalysisProviders.Ollama;

        switch (provider.Trim().ToLowerInvariant())
        {
            case "gemini":
                services.AddHttpClient<IMatchAnalysisService, GeminiMatchService>(client =>
                {
                    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                    client.Timeout = TimeSpan.FromSeconds(120);
                });
                break;

            case "groq":
                services.AddHttpClient<IMatchAnalysisService, GroqMatchService>(client =>
                {
                    client.BaseAddress = new Uri("https://api.groq.com/");
                    client.Timeout = TimeSpan.FromSeconds(120);
                });
                break;

            default:
                var ollamaBaseUrl = configuration[$"{OllamaOptions.SectionName}:BaseUrl"]
                    ?? "http://localhost:11434";

                services.AddHttpClient<IMatchAnalysisService, OllamaMatchService>(client =>
                {
                    client.BaseAddress = new Uri(ollamaBaseUrl.TrimEnd('/') + "/");
                    client.Timeout = TimeSpan.FromSeconds(300);
                });
                break;
        }

        return services;
    }

    public static void ApplyAnalysisApiKeys(IConfigurationManager configuration)
    {
        var geminiApiKey = configuration["Gemini:ApiKey"] ?? configuration["GEMINI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(geminiApiKey))
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GeminiOptions.SectionName}:ApiKey"] = geminiApiKey
            });
        }

        var groqApiKey = configuration["Groq:ApiKey"] ?? configuration["GROQ_API_KEY"];
        if (!string.IsNullOrWhiteSpace(groqApiKey))
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GroqOptions.SectionName}:ApiKey"] = groqApiKey
            });
        }
    }
}
