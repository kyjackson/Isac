using Isac.Core.Shared.Client;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Text; // For StringBuilder
using System.Threading; // For SemaphoreSlim

namespace Isac.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private IRealtimeClient? _realtimeClient;
    private ICartesiaTTSClient? _cartesiaClient;
    private bool _isRecording = false;
    private MemoryStream? _audioBuffer;
    private string? _pendingResponseText;
    private CancellationTokenSource? _recordingCancellation;
    private StringBuilder? _textBuffer; // Buffer for accumulating text from OpenAI
    private SemaphoreSlim? _ttsSemaphore; // Prevent concurrent TTS requests

    public HomePage()
    {
        InitializeComponent();
        
        _textBuffer = new StringBuilder();
        _ttsSemaphore = new SemaphoreSlim(1, 1); // Only allow 1 TTS request at a time
        
        // Get services from dependency injection
        _realtimeClient = Application.Current?.Handler?.MauiContext?.Services?.GetService<IRealtimeClient>();
        _cartesiaClient = Application.Current?.Handler?.MauiContext?.Services?.GetService<ICartesiaTTSClient>();

        // Subscribe to Realtime API events
        if (_realtimeClient != null)
        {
            _realtimeClient.OnTextReceived += OnTextResponseReceived;
            _realtimeClient.OnError += OnAPIError;
        }

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
            // Request microphone permission
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    StatusLabel.Text = "Microphone permission required";
                    return;
                }
            }

            _isRecording = true;
            _textBuffer?.Clear(); // Clear text buffer for new interaction
            StatusLabel.Text = "Listening...";
            
            // Start glow animation
            await AnimateGlow(true);

            // Start recording audio
            _audioBuffer = new MemoryStream();
            _recordingCancellation = new CancellationTokenSource();
            
            // Start recording from microphone on a background thread
            _ = Task.Run(() => RecordAudioAsync(_recordingCancellation.Token));
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
            
            // Stop recording
            _recordingCancellation?.Cancel();
            
            // Give it a moment to finish writing
            await Task.Delay(100);
            
            StatusLabel.Text = "Processing...";
            
            // Stop glow animation
            await AnimateGlow(false);

            // Check if services are available
            if (_realtimeClient == null || _cartesiaClient == null)
            {
                StatusLabel.Text = "Services not configured";
                return;
            }
            
            // Check API keys
            var openAIKey = Preferences.Get("OpenAIApiKey", string.Empty);
            var cartesiaKey = Preferences.Get("CartesiaApiKey", string.Empty);
            var voiceId = Preferences.Get("VoiceId", string.Empty);

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

            // Get recorded audio data
            byte[] audioData;
            if (_audioBuffer != null && _audioBuffer.Length > 0)
            {
                audioData = _audioBuffer.ToArray();
                Debug.WriteLine($"Recorded {audioData.Length} bytes ({audioData.Length / 48000.0:F2} seconds at 24kHz PCM16)");
            }
            else
            {
                // Fallback to test tone if no audio was recorded
                Debug.WriteLine("No audio recorded, using test tone");
                audioData = GenerateTestAudio();
            }

            // Connect to OpenAI Realtime API if not connected
            if (!_realtimeClient.IsConnected)
            {
                StatusLabel.Text = "Connecting to AI...";
                var connected = await _realtimeClient.ConnectAsync();
                if (!connected)
                {
                    StatusLabel.Text = "Failed to connect to OpenAI";
                    return;
                }
            }

            // Send audio via event-based API
            StatusLabel.Text = "Thinking...";
            await _realtimeClient.SendAudioAsync(audioData);
            await _realtimeClient.CommitAudioAsync();
            
            // Response will arrive via OnTextResponseReceived event
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
            _recordingCancellation?.Dispose();
            _recordingCancellation = null;
        }
    }

#if ANDROID
    private void RecordAudioAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Android-specific audio recording using AudioRecord
            // Using 24kHz to match OpenAI Realtime API expectations
            var encoding = Android.Media.Encoding.Pcm16bit;
            var channelConfig = Android.Media.ChannelIn.Mono;
            int sampleRate = 24000; // Changed from 16000 to 24000 for OpenAI compatibility
            
            int bufferSize = Android.Media.AudioRecord.GetMinBufferSize(sampleRate, channelConfig, encoding);
            
            // Check for error values (negative numbers indicate errors)
            if (bufferSize < 0)
            {
                Debug.WriteLine($"Failed to get buffer size for AudioRecord: {bufferSize}");
                return;
            }

            var audioRecord = new Android.Media.AudioRecord(
                Android.Media.AudioSource.Mic,
                sampleRate,
                channelConfig,
                encoding,
                bufferSize * 2);

            if (audioRecord.State != Android.Media.State.Initialized)
            {
                Debug.WriteLine("AudioRecord failed to initialize");
                audioRecord.Release();
                return;
            }

            audioRecord.StartRecording();
            Debug.WriteLine("Started recording audio at 24kHz");

            byte[] buffer = new byte[bufferSize];
            
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = audioRecord.Read(buffer, 0, buffer.Length);
                
                if (bytesRead > 0)
                {
                    _audioBuffer?.Write(buffer, 0, bytesRead);
                }
                else if (bytesRead < 0)
                {
                    Debug.WriteLine($"AudioRecord read error: {bytesRead}");
                    break;
                }
            }

            audioRecord.Stop();
            audioRecord.Release();
            Debug.WriteLine($"Stopped recording. Total bytes: {_audioBuffer?.Length ?? 0}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error recording audio: {ex}");
        }
    }
#else
    private void RecordAudioAsync(CancellationToken cancellationToken)
    {
        // Platform-specific recording not implemented for iOS/Windows yet
        Debug.WriteLine("Audio recording not implemented for this platform");
    }
#endif

    private byte[] GenerateTestAudio()
    {
        // Generate 1 second of 440Hz sine wave at 24kHz sample rate (for OpenAI compatibility)
        // This provides real audio data instead of silence
        int sampleRate = 24000;
        int duration = 1; // seconds
        int frequency = 440; // Hz (A4 note)
        int numSamples = sampleRate * duration;
        byte[] audioData = new byte[numSamples * 2]; // 2 bytes per sample (PCM16)

        for (int i = 0; i < numSamples; i++)
        {
            // Generate sine wave
            double time = i / (double)sampleRate;
            double amplitude = 0.5; // 50% volume
            short sample = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequency * time));
            
            // Convert to little-endian bytes (PCM16 format)
            audioData[i * 2] = (byte)(sample & 0xFF);
            audioData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return audioData;
    }

    private void OnTextResponseReceived(string responseText)
    {
        // FIXED: Marshal to main thread and buffer text to avoid rate limiting
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Filter out punctuation-only text
                if (string.IsNullOrWhiteSpace(responseText) || 
                    responseText.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c)))
                {
                    Debug.WriteLine($"[HomePage] Skipping punctuation-only text: '{responseText}'");
                    return;
                }
                
                // Append to buffer
                _textBuffer?.Append(responseText);
                
                // Check if we have a complete sentence (ends with . ! or ?)
                var bufferedText = _textBuffer?.ToString() ?? string.Empty;
                bool isCompleteSentence = bufferedText.TrimEnd().EndsWith(".") ||
                                         bufferedText.TrimEnd().EndsWith("!") ||
                                         bufferedText.TrimEnd().EndsWith("?"); // FIXED: Changed from ">" to "?"
                
                // Only synthesize when we have a complete sentence AND no TTS is running
                if (!isCompleteSentence)
                {
                    Debug.WriteLine($"[HomePage] Buffering text: '{responseText}' (total: {bufferedText.Length} chars)");
                    return;
                }
                
                // Use semaphore to prevent concurrent TTS requests
                if (_ttsSemaphore == null || !await _ttsSemaphore.WaitAsync(0))
                {
                    Debug.WriteLine("[HomePage] TTS already in progress, skipping");
                    return;
                }
                
                try
                {
                    var finalText = bufferedText.Trim();
                    _textBuffer?.Clear(); // Clear buffer after extracting text
                    
                    Debug.WriteLine($"[HomePage] Synthesizing complete sentence: '{finalText}'");
                    
                    // Get voice ID
                    var voiceId = Preferences.Get("VoiceId", string.Empty);
                    if (string.IsNullOrWhiteSpace(voiceId))
                    {
                        StatusLabel.Text = "No voice enrolled";
                        return;
                    }

                    // Generate speech from response text
                    StatusLabel.Text = "Generating speech...";
                    var speechAudio = await _cartesiaClient!.SynthesizeSpeechAsync(finalText, voiceId);

                    // Play response audio
                    StatusLabel.Text = "Playing response...";
                    await PlayAudioAsync(speechAudio);

                    StatusLabel.Text = "Ready";
                }
                finally
                {
                    _ttsSemaphore?.Release();
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"TTS Error: {ex.Message}";
                Debug.WriteLine($"Error in TTS: {ex}");
            }
        });
    }

    private bool IsPunctuationOnly(string text)
    {
        // Check if the text contains only punctuation characters
        foreach (char c in text)
        {
            if (!char.IsPunctuation(c) && !char.IsWhiteSpace(c))
            {
                return false;
            }
        }
        return true;
    }

    private void OnAPIError(string error)
    {
        // FIXED: Marshal to main thread to avoid threading errors
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = $"API Error: {error}";
            Debug.WriteLine($"OpenAI API Error: {error}");
        });
    }

    private async Task AnimateGlow(bool show)
    {
        if (show)
        {
            // Fade in glow and pulse
            await Task.WhenAll(
                GlowEffect.FadeToAsync(0.6, 500),
                GlowEffect.ScaleToAsync(1.1, 500)
            );
            // Continuous pulse while recording
            _ = Task.Run(async () =>
            {
                while (_isRecording)
                {
                    await GlowEffect.ScaleToAsync(1.15, 800);
                    await GlowEffect.ScaleToAsync(1.1, 800);
                }
            });
        }
        else
        {
            // Fade out glow
            await Task.WhenAll(
                GlowEffect.FadeToAsync(0, 300),
                GlowEffect.ScaleToAsync(1.0, 300)
            );
        }
    }

    private async Task PlayAudioAsync(byte[] audioData)
    {
        try
        {
#if ANDROID
            await Task.Run(() =>
            {
                try
                {
                    Debug.WriteLine($"[HomePage] Playing {audioData.Length} bytes of audio");
                    
                    // Cartesia returns PCM16 at 16kHz (based on your TTS request)
                    int sampleRate = 16000;
                    var channelConfig = Android.Media.ChannelOut.Mono;
                    var audioFormat = Android.Media.Encoding.Pcm16bit;
                    
                    // Calculate buffer size
                    int bufferSize = Android.Media.AudioTrack.GetMinBufferSize(
                        sampleRate, 
                        channelConfig, 
                        audioFormat);
                    
                    if (bufferSize < 0)
                    {
                        Debug.WriteLine($"[HomePage] Failed to get buffer size: {bufferSize}");
                        return;
                    }
                    
                    // Create AudioTrack
                    var audioTrack = new Android.Media.AudioTrack(
                        Android.Media.Stream.Music,
                        sampleRate,
                        channelConfig,
                        audioFormat,
                        Math.Max(bufferSize, audioData.Length),
                        Android.Media.AudioTrackMode.Stream);
                    
                    if (audioTrack.State != Android.Media.AudioTrackState.Initialized)
                    {
                        Debug.WriteLine("[HomePage] AudioTrack failed to initialize");
                        audioTrack.Release();
                        return;
                    }
                    
                    // Start playback
                    audioTrack.Play();
                    Debug.WriteLine("[HomePage] AudioTrack started playing");
                    
                    // Write audio data in chunks
                    int offset = 0;
                    int chunkSize = bufferSize;
                    
                    while (offset < audioData.Length)
                    {
                        int bytesToWrite = Math.Min(chunkSize, audioData.Length - offset);
                        int bytesWritten = audioTrack.Write(audioData, offset, bytesToWrite);
                        
                        if (bytesWritten < 0)
                        {
                            Debug.WriteLine($"[HomePage] AudioTrack write error: {bytesWritten}");
                            break;
                        }
                        
                        offset += bytesWritten;
                    }
                    
                    Debug.WriteLine($"[HomePage] Wrote {offset} bytes to AudioTrack");
                    
                    // Wait for playback to finish
                    // Calculate duration: bytes / (sample_rate * 2 bytes per sample) * 1000 ms
                    int durationMs = (int)((audioData.Length / (float)(sampleRate * 2)) * 1000);
                    Thread.Sleep(durationMs + 100); // Add small buffer
                    
                    // Stop and release
                    audioTrack.Stop();
                    audioTrack.Release();
                    Debug.WriteLine("[HomePage] AudioTrack playback completed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HomePage] Error in audio playback: {ex}");
                }
            });
#else
            // Platform-specific playback not implemented for iOS/Windows yet
            Debug.WriteLine("[HomePage] Audio playback not implemented for this platform");
            await Task.Delay(2000); // Simulate playback
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomePage] Error playing audio: {ex}");
        }
    }
}
