using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Isac.Core.Shared.Client;
using Isac.Core.Shared.Configuration;
using Xunit;

namespace Isac.Tests;

public class IsacHttpClientTests
{
    [Fact]
    public async Task PingAsync_ReturnsFalse_OnNetworkError()
    {
        var handler = new FailingHandler();
        var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://localhost") };
        var isac = new IsacHttpClient(client, new IsacApiOptions { BaseUrl = "https://localhost" });
        var result = await isac.PingAsync();
        Assert.False(result);
    }

    private class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network down");
        }
    }
}
