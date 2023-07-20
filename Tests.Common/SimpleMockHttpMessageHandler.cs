using System.Net;
using System.Text;
using System.Text.Json;

namespace PEXC.Case.Tests.Common;

public class SimpleMockHttpMessageHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly HttpResponseMessage _response;

    private readonly Predicate<HttpRequestMessage>? _requestPredicate;

    public SimpleMockHttpMessageHandler(
        object content,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Predicate<HttpRequestMessage>? requestPredicate = null)
    {
        var json = JsonSerializer.Serialize(content, SerializerOptions);
        _response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        _requestPredicate = requestPredicate;
    }

    public SimpleMockHttpMessageHandler(
        HttpResponseMessage response,
        Predicate<HttpRequestMessage>? requestPredicate = null)
    {
        _response = response;
        _requestPredicate = requestPredicate;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(Send(request, cancellationToken));

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_requestPredicate == null || _requestPredicate(request))
            return _response;

        throw new InvalidOperationException("HttpRequestMessage does not match the predicate.");
    }
}