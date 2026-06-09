using System.Net;

namespace AiCvMatch.Api.Tests.Helpers;

public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        : this(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        })
    {
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
