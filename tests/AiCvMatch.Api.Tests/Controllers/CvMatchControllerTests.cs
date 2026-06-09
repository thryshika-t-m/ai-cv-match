using AiCvMatch.Api.Controllers;
using AiCvMatch.Api.Models;
using AiCvMatch.Api.Services;
using AiCvMatch.Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AiCvMatch.Api.Tests.Controllers;

public sealed class CvMatchControllerTests
{
    private readonly Mock<IPdfTextExtractor> _pdfTextExtractor = new();
    private readonly Mock<IMatchAnalysisService> _matchAnalysisService = new();
    private readonly CvMatchController _controller;

    public CvMatchControllerTests()
    {
        _controller = new CvMatchController(
            _pdfTextExtractor.Object,
            _matchAnalysisService.Object,
            NullLogger<CvMatchController>.Instance);
    }

    [Fact]
    public async Task MatchAsync_NoFile_ReturnsBadRequest()
    {
        var result = await _controller.MatchAsync(null, "Job description", CancellationToken.None);

        var problem = GetBadRequestProblem(result);
        Assert.Contains("non-empty PDF", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_EmptyFile_ReturnsBadRequest()
    {
        var file = FormFileHelper.Create(length: 0);

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        GetBadRequestProblem(result);
    }

    [Fact]
    public async Task MatchAsync_FileTooLarge_ReturnsBadRequest()
    {
        var file = FormFileHelper.Create(length: 11 * 1024 * 1024);

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var problem = GetBadRequestProblem(result);
        Assert.Contains("exceeds the maximum size", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_NonPdfFile_ReturnsBadRequest()
    {
        var file = FormFileHelper.Create(
            fileName: "resume.txt",
            contentType: "text/plain");

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var problem = GetBadRequestProblem(result);
        Assert.Contains("Only PDF", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_MissingJobDescription_ReturnsBadRequest()
    {
        var file = FormFileHelper.Create();

        var result = await _controller.MatchAsync(file, "   ", CancellationToken.None);

        var problem = GetBadRequestProblem(result);
        Assert.Contains("Job description is required", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_PdfExtractionFails_ReturnsBadRequest()
    {
        var file = FormFileHelper.Create();
        _pdfTextExtractor
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad pdf"));

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var problem = GetBadRequestProblem(result);
        Assert.Contains("Could not read text", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_EmptyExtractedText_ReturnsBadRequest()
    {
        var file = FormFileHelper.Create();
        _pdfTextExtractor
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("   ");

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var problem = GetBadRequestProblem(result);
        Assert.Contains("No extractable text", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_ValidRequest_ReturnsOkWithResult()
    {
        var file = FormFileHelper.Create();
        var expected = new CvMatchResult
        {
            MatchScore = 82,
            MatchedSkills = ["C#"],
            SkillGaps = ["Azure"],
            Recommendations = ["Add cloud examples"]
        };

        _pdfTextExtractor
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CV text");
        _matchAnalysisService
            .Setup(x => x.AnalyzeAsync("CV text", "Job description", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actual = Assert.IsType<CvMatchResult>(okResult.Value);
        Assert.Equal(82, actual.MatchScore);
        Assert.Equal(["C#"], actual.MatchedSkills);
    }

    [Fact]
    public async Task MatchAsync_AnalysisHttpFailure_ReturnsBadGateway()
    {
        var file = FormFileHelper.Create();
        _pdfTextExtractor
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CV text");
        _matchAnalysisService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("provider down", null, System.Net.HttpStatusCode.BadGateway));

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("AI analysis service is unavailable", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_AnalysisConfigurationFailure_ReturnsBadGatewayWithMessage()
    {
        var file = FormFileHelper.Create();
        _pdfTextExtractor
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CV text");
        _matchAnalysisService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API key is not configured."));

        var result = await _controller.MatchAsync(file, "Job description", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("API key is not configured.", problem.Detail);
    }

    private static ProblemDetails GetBadRequestProblem(ActionResult<CvMatchResult> result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        return Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);
    }
}
