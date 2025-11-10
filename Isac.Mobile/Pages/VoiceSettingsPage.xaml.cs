using Microsoft.Maui.Controls;

namespace Isac.Mobile.Pages
{
    public partial class VoiceSettingsPage : ContentPage
    {
        public VoiceSettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load saved settings
            if (CartesiaApiKeyEntry != null)
                CartesiaApiKeyEntry.Text = Preferences.Get("CartesiaApiKey", string.Empty);
            
            if (VoiceIdEntry != null)
                VoiceIdEntry.Text = Preferences.Get("VoiceId", string.Empty);
        }

        private async void OnSaveVoiceSettingsClicked(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(CartesiaApiKeyEntry?.Text) || 
                string.IsNullOrWhiteSpace(VoiceIdEntry?.Text))
            {
                await DisplayAlert("Validation Error", "Please fill in all fields.", "OK");
                return;
            }

            // Save settings
            Preferences.Set("CartesiaApiKey", CartesiaApiKeyEntry.Text);
            Preferences.Set("VoiceId", VoiceIdEntry.Text);

            await DisplayAlert("Success", "Voice settings saved successfully!", "OK");
        }
    }
}
