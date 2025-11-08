using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Isac.Mobile.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BaseUrlEntry.Text = Preferences.Get("Isac:Api:BaseUrl", string.Empty);
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var url = BaseUrlEntry.Text?.Trim() ?? string.Empty;
        Preferences.Set("Isac:Api:BaseUrl", url);
        StatusLabel.Text = "Saved.";
    }
}
