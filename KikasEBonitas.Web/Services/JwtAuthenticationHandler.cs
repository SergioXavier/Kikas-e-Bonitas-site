using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace KikasEBonitas.Web.Services;

public class JwtAuthenticationHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;

    public JwtAuthenticationHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Lê o token guardado no localStorage do browser
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}