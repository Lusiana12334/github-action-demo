using System.Net.Http.Json;
using System.Text.Json;

namespace PEXC.Case.Infrastructure;

public static class HttpClientExtensions
{
    public static HttpClient SetBearer(this HttpClient httpClient, string bearerToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        return httpClient;
    }

    public static async Task<TResponse?> PostAsync<TRequest, TResponse>(
        this HttpClient httpClient, 
        string requestUri, 
        TRequest request,
        JsonSerializerOptions? serializerOptions = default)
    {
        var result = await httpClient.PostAsJsonAsync(requestUri, request, serializerOptions);

        result.EnsureSuccessStatusCode();

        return await result.Content.ReadFromJsonAsync<TResponse>();
    }
}