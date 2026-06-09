using System.Net;
using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services;
using AiCvMatch.Api.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiCvMatch.Api.Tests.Services;

public sealed class OllamaMatchServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_SuccessfulResponse_ReturnsParsedResult()
    {
        const string response = """
            {
              "message": {
                "content": "{\"matchScore\":75,\"matchedSkills\":[\"C#\"],\"skillGaps\":[\"Azure\"],\"recommendations\":[\"Add cloud projects\"]}"
              }
            }
            """;

        var service = CreateService(HttpStatusCode.OK, response);

        var result = await service.AnalyzeAsync("CV text", "Job description");

        Assert.Equal(75, result.MatchScore);
        Assert.Equal(["C#"], result.MatchedSkills);
        Assert.Equal(["Azure"], result.SkillGaps);
    }

    [Fact]
    public async Task AnalyzeAsync_HttpFailure_ThrowsHttpRequestException()
    {
        var service = CreateService(HttpStatusCode.InternalServerError, "error");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeAsync("CV text", "Job description"));

        Assert.Contains("Ollama API request failed", exception.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyContent_ThrowsInvalidOperationException()
    {
        const string response = """{ "message": { "content": "" } }""";
        var service = CreateService(HttpStatusCode.OK, response);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AnalyzeAsync("CV text", "Job description"));
    }

    private static OllamaMatchService CreateService(HttpStatusCode statusCode, string content)
    {
        var handler = new MockHttpMessageHandler(statusCode, content);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };

        return new OllamaMatchService(
            client,
            Microsoft.Extensions.Options.Options.Create(new OllamaOptions { Model = "llama3.2" }),
            NullLogger<OllamaMatchService>.Instance);
    }
}
