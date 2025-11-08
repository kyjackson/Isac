using Isac.Core.Shared.Client;
using Isac.Core.Shared.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Isac.Mobile.Pages;

public partial class EnrollmentPage : ContentPage
{
    private readonly List<VoiceSampleDescriptor> _clips = new();

    public EnrollmentPage()
    {
        InitializeComponent();
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select an audio file"
        });
        if (file is null) return;
        using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        _clips.Add(new VoiceSampleDescriptor(file.FileName, "audio/wav", ms.ToArray()));
        StatusLabel.Text = $"Clips: {_clips.Count}";
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        var baseUrl = Preferences.Get("Isac:Api:BaseUrl", string.Empty);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            StatusLabel.Text = "Set API Base URL in Settings.";
            return;
        }
        if (_clips.Count == 0)
        {
            StatusLabel.Text = "Add at least one clip.";
            return;
        }

        var services = new ServiceCollection();
        services.AddIsacClient(o => o.BaseUrl = baseUrl);
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IIsacClient>();

        StatusLabel.Text = "Uploading...";
        try
        {
            var resp = await client.EnrollVoiceAsync(new VoiceEnrollRequest("demo-user", _clips));
            StatusLabel.Text = $"Enrolled: {resp.VoiceProfileId}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }
}
