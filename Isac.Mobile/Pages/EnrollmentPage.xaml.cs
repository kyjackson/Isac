using Isac.Core.Shared.Client;
using Isac.Core.Shared.Transport;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Isac.Mobile.Pages;

public partial class EnrollmentPage : ContentPage
{
    private readonly List<VoiceSampleDescriptor> _samples = new();
    private readonly ICartesiaTTSClient _cartesiaClient;

    public EnrollmentPage(ICartesiaTTSClient cartesiaClient)
    {
        InitializeComponent();
        _cartesiaClient = cartesiaClient;
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        try
        {
            // For MVP: use file picker to select audio samples
            // TODO: Replace with actual audio recording using platform-specific APIs
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select an audio sample (WAV or MP3)"
            });

            if (file is null) return;

            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var sample = new VoiceSampleDescriptor(
                file.FileName,
                file.ContentType ?? "audio/wav",
                ms.ToArray()
            );

            _samples.Add(sample);
            SamplesLabel.Text = $"Samples recorded: {_samples.Count}";
            UploadButton.IsEnabled = _samples.Count >= 3;
            StatusLabel.Text = $"Added sample: {file.FileName}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnUploadClicked(object? sender, EventArgs e)
    {
        var cartesiaKey = Preferences.Get("Cartesia:ApiKey", string.Empty);
        if (string.IsNullOrWhiteSpace(cartesiaKey))
        {
            StatusLabel.Text = "Set Cartesia API Key in Settings first.";
            return;
        }

        if (_samples.Count < 3)
        {
            StatusLabel.Text = "Record at least 3 samples.";
            return;
        }

        StatusLabel.Text = "Uploading to Cartesia...";
        UploadButton.IsEnabled = false;

        try
        {
            var voiceId = await _cartesiaClient.CreateVoiceAsync(_samples, "ISAC_User_Voice");
            Preferences.Set("Cartesia:VoiceId", voiceId);
            StatusLabel.Text = $"Voice created! ID: {voiceId}";

            // Clear samples after successful upload
            _samples.Clear();
            SamplesLabel.Text = "Samples recorded: 0";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Upload failed: {ex.Message}";
            UploadButton.IsEnabled = true;
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _samples.Clear();
        SamplesLabel.Text = "Samples recorded: 0";
        UploadButton.IsEnabled = false;
        StatusLabel.Text = "Samples cleared.";
    }
}
