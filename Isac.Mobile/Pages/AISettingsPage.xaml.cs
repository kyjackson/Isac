using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Isac.Mobile.Pages;

public partial class AISettingsPage : ContentPage
{
    public AISettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load saved settings
        OpenAIApiKeyEntry.Text = Preferences.Get("OpenAIApiKey", string.Empty);
    }

    private async void OnSaveAISettingsClicked(object sender, EventArgs e)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(OpenAIApiKeyEntry.Text))
        {
            await DisplayAlert("Validation Error", "Please enter your OpenAI API key.", "OK");
            return;
        }

        // Save settings
        Preferences.Set("OpenAIApiKey", OpenAIApiKeyEntry.Text);

        await DisplayAlert("Success", "AI settings saved successfully!", "OK");
    }
}
