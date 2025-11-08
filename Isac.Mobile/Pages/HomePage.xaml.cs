using Isac.Core.Shared.Client;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Isac.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private IIsacClient? _client; // built per entered URL

    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnTestConnection(object? sender, EventArgs e)
    {
        var baseUrl = BaseUrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            ResultLabel.Text = "Enter base URL.";
            return;
        }

        var collection = new ServiceCollection();
        collection.AddIsacClient(o => o.BaseUrl = baseUrl);
        using var provider = collection.BuildServiceProvider();
        _client = provider.GetRequiredService<IIsacClient>();

        ResultLabel.Text = "Pinging...";
        var ok = await _client.PingAsync();
        ResultLabel.Text = ok ? "Ping success" : "Ping failed";
    }
}
