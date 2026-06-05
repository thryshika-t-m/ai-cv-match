using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiCvMatch.Api.Models;
using AiCvMatch.Api.Options;
using AiCvMatch.Api.Services.Analysis;
using Microsoft.Extensions.Options;

namespace AiCvMatch.Api.Services;

public sealed class OllamaMatchService : IMatchAnalysisService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaMatchService> _logger;

    public OllamaMatchService(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaMatchService> logger)
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
        var prompt = MatchAnalysisPrompt.Build(cvText, jobDescription);
        var requestBody = new OllamaChatRequest
        {
            Model = _options.Model,
            Stream = false,
            Format = "json",
            Messages =
            [
                new OllamaMessage { Role = "user", Content = prompt }
            ]
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        var responseBody = await SendAsync(httpRequest, cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, SerializerOptions);
        var jsonText = chatResponse?.Message?.Content;

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
                "Ollama API returned {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Ollama API request failed with status {(int)response.StatusCode}. " +
                "Ensure Ollama is running and the model is pulled (e.g. ollama pull llama3.2).",
                inner: null,
                statusCode: response.StatusCode);
        }

        return responseBody;
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<OllamaMessage> Messages { get; set; } = [];
        public bool Stream { get; set; }
        public string Format { get; set; } = "json";
    }

    private sealed class OllamaMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }
}
