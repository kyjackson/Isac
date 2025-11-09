using Isac.Core.Shared.Client;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace Isac.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private readonly IRealtimeClient _realtimeClient;
    private readonly ICartesiaTTSClient _cartesiaClient;
    private bool _isRecording = false;
    private MemoryStream? _audioBuffer;

    public HomePage(IRealtimeClient realtimeClient, ICartesiaTTSClient cartesiaClient)
    {
        InitializeComponent();
        _realtimeClient = realtimeClient;
        _cartesiaClient = cartesiaClient;

        // Wire up button press/release events
        var pressGesture = new PointerGestureRecognizer();
        pressGesture.PointerPressed += OnRecordPressed;
        pressGesture.PointerReleased += OnRecordReleased;
        RecordButton.GestureRecognizers.Add(pressGesture);
    }

    private async void OnRecordPressed(object? sender, PointerEventArgs e)
    {
        if (_isRecording) return;

        try
        {
            _isRecording = true;
            StatusLabel.Text = "Listening...";
            
            // Start glow animation
            await AnimateGlow(true);

            // Start recording audio
            _audioBuffer = new MemoryStream();
            
            // TODO: Implement actual audio recording using platform-specific APIs
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            _isRecording = false;
            await AnimateGlow(false);
        }
    }

    private async void OnRecordReleased(object? sender, PointerEventArgs e)
    {
        if (!_isRecording) return;

        try
        {
            _isRecording = false;
            StatusLabel.Text = "Processing...";
            
            // Stop glow animation
            await AnimateGlow(false);

            // For MVP: simulate audio data (replace with actual recorded audio)
            var audioData = new byte[16000 * 2]; // 1 second of silence at 16kHz PCM16
            
            // Check API keys
            var openAIKey = Preferences.Get("OpenAI:ApiKey", string.Empty);
            var cartesiaKey = Preferences.Get("Cartesia:ApiKey", string.Empty);
            var voiceId = Preferences.Get("Cartesia:VoiceId", string.Empty);

            if (string.IsNullOrWhiteSpace(openAIKey))
            {
                StatusLabel.Text = "Set OpenAI API key in Settings";
                return;
            }

            if (string.IsNullOrWhiteSpace(cartesiaKey) || string.IsNullOrWhiteSpace(voiceId))
            {
                StatusLabel.Text = "Enroll your voice first";
                return;
            }

            // Connect to OpenAI Realtime API
            StatusLabel.Text = "Connecting to AI...";
            await _realtimeClient.ConnectAsync();

            // Send audio and get text response
            StatusLabel.Text = "Thinking...";
            var responseText = await _realtimeClient.SendAudioAndGetResponseAsync(audioData, 16000);

            // Disconnect from Realtime API
            await _realtimeClient.DisconnectAsync();

            // Generate speech from response text
            StatusLabel.Text = "Generating speech...";
            var speechAudio = await _cartesiaClient.SynthesizeSpeechAsync(responseText, voiceId);

            // Play response audio
            StatusLabel.Text = "Playing response...";
            await PlayAudioAsync(speechAudio);

            StatusLabel.Text = "Ready";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            Debug.WriteLine($"Error in voice interaction: {ex}");
        }
        finally
        {
            _audioBuffer?.Dispose();
            _audioBuffer = null;
        }
    }

    private async Task AnimateGlow(bool show)
    {
        if (show)
        {
            // Fade in glow and pulse
            await Task.WhenAll(
                GlowEffect.FadeTo(0.6, 500),
                GlowEffect.ScaleTo(1.1, 500)
            );
            // Continuous pulse while recording
            _ = Task.Run(async () =>
            {
                while (_isRecording)
                {
                    await GlowEffect.ScaleTo(1.15, 800);
                    await GlowEffect.ScaleTo(1.1, 800);
                }
            });
        }
        else
        {
            // Fade out glow
            await Task.WhenAll(
                GlowEffect.FadeTo(0, 300),
                GlowEffect.ScaleTo(1.0, 300)
            );
        }
    }

    private async Task PlayAudioAsync(byte[] audioData)
    {
        try
        {
            // TODO: Implement actual audio playback using platform-specific APIs
            // For now, simulate playback delay
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error playing audio: {ex}");
        }
    }
}
