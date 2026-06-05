using AiCvMatch.Api.Models;
using AiCvMatch.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiCvMatch.Api.Controllers;

[ApiController]
[Route("api/cv-match")]
public sealed class CvMatchController : ControllerBase
{
    private const long MaxPdfBytes = 10 * 1024 * 1024;

    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IMatchAnalysisService _matchAnalysisService;
    private readonly ILogger<CvMatchController> _logger;

    public CvMatchController(
        IPdfTextExtractor pdfTextExtractor,
        IMatchAnalysisService matchAnalysisService,
        ILogger<CvMatchController> logger)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _matchAnalysisService = matchAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a CV PDF against a job description and returns a structured match report.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CvMatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [RequestSizeLimit(MaxPdfBytes + 1024 * 1024)]
    public async Task<ActionResult<CvMatchResult>> MatchAsync(
        [FromForm] IFormFile? cvPdf,
        [FromForm] string? jobDescription,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(cvPdf, jobDescription);
        if (validationError is not null)
        {
            return ValidationProblem(
                detail: validationError,
                statusCode: StatusCodes.Status400BadRequest);
        }

        string cvText;
        try
        {
            await using var pdfStream = cvPdf!.OpenReadStream();
            cvText = await _pdfTextExtractor.ExtractTextAsync(pdfStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from uploaded PDF.");
            return ValidationProblem(
                detail: "Could not read text from the uploaded PDF. Ensure the file is a valid, text-based PDF.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(cvText))
        {
            return ValidationProblem(
                detail: "No extractable text was found in the PDF. Scanned image-only PDFs are not supported.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await _matchAnalysisService.AnalyzeAsync(
                cvText,
                jobDescription!,
                cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI analysis provider call failed.");
            return Problem(
                detail: "The AI analysis service is unavailable or returned an error. Check Analysis:Provider and provider settings in appsettings.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI analysis failed.");
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static string? ValidateRequest(IFormFile? cvPdf, string? jobDescription)
    {
        if (cvPdf is null || cvPdf.Length == 0)
        {
            return "A non-empty PDF file is required (form field: cvPdf).";
        }

        if (cvPdf.Length > MaxPdfBytes)
        {
            return $"PDF file exceeds the maximum size of {MaxPdfBytes / (1024 * 1024)} MB.";
        }

        var extension = Path.GetExtension(cvPdf.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(cvPdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "Only PDF files are supported.";
        }

        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            return "Job description is required (form field: jobDescription).";
        }

        return null;
    }
}
