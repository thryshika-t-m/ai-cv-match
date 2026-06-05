using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiCvMatch.Api.Models;
using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services.Analysis;
using Microsoft.Extensions.Options;

namespace AiCvMatch.Api.Services;

public sealed class GeminiMatchService : IMatchAnalysisService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiMatchService> _logger;

    public GeminiMatchService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiMatchService> logger)
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
                "Gemini API key is not configured. Set Gemini:ApiKey or GEMINI_API_KEY.");
        }

        var prompt = MatchAnalysisPrompt.Build(cvText, jobDescription);
        var requestUri =
            $"v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

        var requestBody = new GeminiGenerateContentRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts = [new GeminiPart { Text = prompt }]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                ResponseMimeType = "application/json",
                Temperature = 0.2,
                MaxOutputTokens = _options.MaxOutputTokens
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        var responseBody = await SendAsync(httpRequest, cancellationToken);

        var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(
            responseBody,
            SerializerOptions);

        var jsonText = geminiResponse?.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

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
                "Gemini API returned {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Gemini API request failed with status {(int)response.StatusCode}.",
                inner: null,
                statusCode: response.StatusCode);
        }

        return responseBody;
    }

    private sealed class GeminiGenerateContentRequest
    {
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
    }

    private sealed class GeminiGenerationConfig
    {
        public string? ResponseMimeType { get; set; }
        public double Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
    }

    private sealed class GeminiGenerateContentResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }
}
