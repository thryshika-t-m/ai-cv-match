using System.Net;
using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services;
using AiCvMatch.Api.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiCvMatch.Api.Tests.Services;

public sealed class GeminiMatchServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        var service = CreateService(HttpStatusCode.OK, "{}", apiKey: "");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync("CV text", "Job description"));

        Assert.Contains("Gemini API key is not configured", exception.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_SuccessfulResponse_ReturnsParsedResult()
    {
        const string response = """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      {
                        "text": "{\"matchScore\":91,\"matchedSkills\":[\"ASP.NET Core\"],\"skillGaps\":[\"Kubernetes\"],\"recommendations\":[\"Mention container work\"]}"
                      }
                    ]
                  }
                }
              ]
            }
            """;

        var service = CreateService(HttpStatusCode.OK, response);

        var result = await service.AnalyzeAsync("CV text", "Job description");

        Assert.Equal(91, result.MatchScore);
        Assert.Equal(["ASP.NET Core"], result.MatchedSkills);
        Assert.Equal(["Kubernetes"], result.SkillGaps);
    }

    [Fact]
    public async Task AnalyzeAsync_HttpFailure_ThrowsHttpRequestException()
    {
        var service = CreateService(HttpStatusCode.Forbidden, "forbidden");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeAsync("CV text", "Job description"));

        Assert.Contains("Gemini API request failed", exception.Message);
    }

    private static GeminiMatchService CreateService(
        HttpStatusCode statusCode,
        string content,
        string apiKey = "test-key")
    {
        var handler = new MockHttpMessageHandler(statusCode, content);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        return new GeminiMatchService(
            client,
            Microsoft.Extensions.Options.Options.Create(new GeminiOptions
            {
                ApiKey = apiKey,
                Model = "gemini-2.0-flash-lite"
            }),
            NullLogger<GeminiMatchService>.Instance);
    }
}
