using AiCvMatch.Api.Extensions;
using AiCvMatch.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiCvMatch.Api.Tests.Extensions;

public sealed class AnalysisServiceExtensionsTests
{
    [Theory]
    [InlineData("Ollama", typeof(OllamaMatchService))]
    [InlineData("ollama", typeof(OllamaMatchService))]
    [InlineData("Groq", typeof(GroqMatchService))]
    [InlineData("Gemini", typeof(GeminiMatchService))]
    public void AddMatchAnalysisProvider_RegistersExpectedService(string provider, Type expectedType)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Analysis:Provider"] = provider
        });

        services.AddMatchAnalysisProvider(configuration);
        using var providerScope = services.BuildServiceProvider();

        var service = providerScope.GetRequiredService<IMatchAnalysisService>();

        Assert.IsType(expectedType, service);
    }

    [Fact]
    public void ApplyAnalysisApiKeys_LoadsGeminiAndGroqKeysFromEnvironmentStyleNames()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = "gemini-secret",
                ["GROQ_API_KEY"] = "groq-secret"
            })
            .Build();

        var configurationManager = new ConfigurationManager();
        foreach (var pair in configuration.AsEnumerable())
        {
            if (pair.Value is not null)
            {
                configurationManager[pair.Key] = pair.Value;
            }
        }

        AnalysisServiceExtensions.ApplyAnalysisApiKeys(configurationManager);

        Assert.Equal("gemini-secret", configurationManager["Gemini:ApiKey"]);
        Assert.Equal("groq-secret", configurationManager["Groq:ApiKey"]);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
