using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiCvMatch.Api.Models;
using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services.Analysis;
using Microsoft.Extensions.Options;

namespace AiCvMatch.Api.Services;

public sealed class GroqMatchService : IMatchAnalysisService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqMatchService> _logger;

    public GroqMatchService(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        ILogger<GroqMatchService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CvMatchResult> AnalyzeAsync(
        string cvText,
        string jobDescription,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Groq API key is not configured. Set Groq:ApiKey or GROQ_API_KEY.");
        }

        var prompt = MatchAnalysisPrompt.Build(cvText, jobDescription);
        var requestBody = new GroqChatRequest
        {
            Model = _options.Model,
            Temperature = 0.2,
            MaxTokens = _options.MaxOutputTokens,
            ResponseFormat = new GroqResponseFormat { Type = "json_object" },
            Messages =
            [
                new GroqMessage { Role = "user", Content = prompt }
            ]
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var responseBody = await SendAsync(httpRequest, cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<GroqChatResponse>(responseBody, SerializerOptions);
        var jsonText = chatResponse?.Choices?
            .Select(choice => choice.Message?.Content)
            .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content));

        return MatchAnalysisResponseParser.Parse(jsonText ?? string.Empty);
    }

    private async Task<string> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Groq API returned {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Groq API request failed with status {(int)response.StatusCode}.",
                inner: null,
                statusCode: response.StatusCode);
        }

        return responseBody;
    }

    private sealed class GroqChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<GroqMessage> Messages { get; set; } = [];
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public GroqResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class GroqMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GroqResponseFormat
    {
        public string Type { get; set; } = string.Empty;
    }

    private sealed class GroqChatResponse
    {
        public List<GroqChoice>? Choices { get; set; }
    }

    private sealed class GroqChoice
    {
        public GroqMessage? Message { get; set; }
    }
}
