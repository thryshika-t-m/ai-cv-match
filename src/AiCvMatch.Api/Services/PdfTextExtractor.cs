using System.Text;
using UglyToad.PdfPig;

namespace AiCvMatch.Api.Services;

public sealed class PdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var document = PdfDocument.Open(pdfStream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = page.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(text.Trim());
        }

        var result = builder.ToString().Trim();
        return Task.FromResult(result);
    }
}
