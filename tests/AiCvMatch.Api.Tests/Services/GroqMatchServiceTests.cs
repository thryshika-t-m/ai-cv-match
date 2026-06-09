using System.Net;
using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services;
using AiCvMatch.Api.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiCvMatch.Api.Tests.Services;

public sealed class GroqMatchServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        var service = CreateService(HttpStatusCode.OK, "{}", apiKey: "");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync("CV text", "Job description"));

        Assert.Contains("Groq API key is not configured", exception.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_SuccessfulResponse_ReturnsParsedResult()
    {
        const string response = """
            {
              "choices": [
                {
                  "message": {
                    "content": "{\"matchScore\":68,\"matchedSkills\":[\"REST APIs\"],\"skillGaps\":[],\"recommendations\":[\"Add API metrics\"]}"
                  }
                }
              ]
            }
            """;

        var service = CreateService(HttpStatusCode.OK, response);

        var result = await service.AnalyzeAsync("CV text", "Job description");

        Assert.Equal(68, result.MatchScore);
        Assert.Equal(["REST APIs"], result.MatchedSkills);
    }

    [Fact]
    public async Task AnalyzeAsync_HttpFailure_ThrowsHttpRequestException()
    {
        var service = CreateService(HttpStatusCode.TooManyRequests, "quota exceeded");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeAsync("CV text", "Job description"));

        Assert.Contains("Groq API request failed", exception.Message);
    }

    private static GroqMatchService CreateService(
        HttpStatusCode statusCode,
        string content,
        string apiKey = "test-key")
    {
        var handler = new MockHttpMessageHandler(statusCode, content);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.groq.com/")
        };

        return new GroqMatchService(
            client,
            Microsoft.Extensions.Options.Options.Create(new GroqOptions
            {
                ApiKey = apiKey,
                Model = "llama-3.1-8b-instant"
            }),
            NullLogger<GroqMatchService>.Instance);
    }
}
