using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Isac.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        OpenAIKeyEntry.Text = Preferences.Get("OpenAI:ApiKey", string.Empty);
        CartesiaKeyEntry.Text = Preferences.Get("Cartesia:ApiKey", string.Empty);
        VoiceIdEntry.Text = Preferences.Get("Cartesia:VoiceId", string.Empty);
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var openAIKey = OpenAIKeyEntry.Text?.Trim() ?? string.Empty;
        var cartesiaKey = CartesiaKeyEntry.Text?.Trim() ?? string.Empty;

        Preferences.Set("OpenAI:ApiKey", openAIKey);
        Preferences.Set("Cartesia:ApiKey", cartesiaKey);
        
        StatusLabel.Text = "API keys saved.";
    }
}
