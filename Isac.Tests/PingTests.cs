using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http;

namespace Isac.Tests;

public class PingTests
{
    [Fact]
    public async Task PingEndpoint_ReturnsOkStatus()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/v1/ping", async context =>
                        {
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"status\":\"ok\"}");
                        });
                    });
                });
            });

        using var host = await builder.StartAsync();
        var client = host.GetTestClient();
        var resp = await client.GetAsync("/api/v1/ping");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ok", json);
    }
}
