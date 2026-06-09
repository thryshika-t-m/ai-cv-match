using Microsoft.AspNetCore.Http;
using Moq;

namespace AiCvMatch.Api.Tests.Helpers;

public static class FormFileHelper
{
    public static IFormFile Create(
        string fileName = "resume.pdf",
        string contentType = "application/pdf",
        byte[]? content = null,
        long? length = null)
    {
        content ??= "%PDF-1.4 sample"u8.ToArray();
        var stream = new MemoryStream(content);

        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length ?? content.Length);
        file.Setup(f => f.OpenReadStream()).Returns(stream);

        return file.Object;
    }
}
