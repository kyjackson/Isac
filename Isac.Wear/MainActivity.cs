using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Isac.Core.Shared.Client;
using Isac.Core.Shared.Transport;
using Microsoft.Extensions.DependencyInjection;
using AudioStream = Android.Media.Stream;

namespace Isac.Wear
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : Activity
    {
        private const int SampleRate = 16000;
        private const ChannelIn ChannelConfigIn = ChannelIn.Mono;
        private const Encoding AudioEncoding = Encoding.Pcm16bit;

        private Button? _speakButton;
        private TextView? _status;
        private Button? _settingsButton;
        private AudioRecord? _recorder;
        private bool _recording;
        private MemoryStream? _buffer;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            _speakButton = FindViewById<Button>(Resource.Id.btnSpeak);
            _status = FindViewById<TextView>(Resource.Id.txtStatus);
            _settingsButton = FindViewById<Button>(Resource.Id.btnSettings);

            _speakButton!.Touch += OnSpeakTouch;
            _settingsButton!.Click += (s, e) => StartActivity(typeof(SettingsActivity));

            EnsurePermissions();
        }

        private void EnsurePermissions()
        {
            if (CheckSelfPermission(Manifest.Permission.RecordAudio) != Permission.Granted)
            {
                RequestPermissions(new[] { Manifest.Permission.RecordAudio }, 1001);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == 1001 && grantResults.Length > 0 && grantResults[0] != Permission.Granted)
            {
                Toast.MakeText(this, "Microphone permission required", ToastLength.Long).Show();
            }
        }

        private async void OnSpeakTouch(object? sender, View.TouchEventArgs e)
        {
            switch (e.Event?.Action)
            {
                case MotionEventActions.Down:
                    StartRecording();
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    await StopAndSendAsync();
                    break;
            }
        }

        private void StartRecording()
        {
            var minBuffer = AudioRecord.GetMinBufferSize(SampleRate, ChannelConfigIn, AudioEncoding);
            _recorder = new AudioRecord(AudioSource.Mic, SampleRate, ChannelConfigIn, AudioEncoding, minBuffer * 2);
            _buffer = new MemoryStream();
            _recording = true;
            _recorder.StartRecording();
            _status!.Text = "Listening…";

            Task.Run(() =>
            {
                var buf = new byte[minBuffer];
                while (_recording && _recorder?.RecordingState == RecordState.Recording)
                {
                    int read = _recorder.Read(buf, 0, buf.Length);
                    if (read > 0)
                    {
                        _buffer!.Write(buf, 0, read);
                    }
                }
            });
        }

        private async Task StopAndSendAsync()
        {
            if (_recorder == null || _buffer == null) return;

            _recording = false;
            _recorder.Stop();
            _recorder.Release();
            _recorder = null;

            var pcm = _buffer.ToArray();
            _buffer.Dispose();
            _buffer = null;

            _status!.Text = "Sending…";

            try
            {
                var wav = BuildWav(pcm, SampleRate, 1, 16);
                var baseUrl = GetSharedPreferences("isac", FileCreationMode.Private).GetString("api_base", string.Empty) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _status.Text = "Set API base URL";
                    return;
                }

                var services = new ServiceCollection();
                services.AddIsacClient(o => o.BaseUrl = baseUrl);
                using var provider = services.BuildServiceProvider();
                var client = provider.GetRequiredService<IIsacClient>();

                var resp = await client.SendQueryAsync(new QueryRequest(
                    UserId: "demo-user",
                    DeviceId: Android.OS.Build.Model ?? "wear",
                    AudioFormat: "audio/wav",
                    Payload: wav
                ));

                _status.Text = "Playing response…";
                PlayPcmWav(resp.AudioBytes);
                _status.Text = "Idle";
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        }

        private static byte[] BuildWav(byte[] pcm, int sampleRate, short channels, short bitsPerSample)
        {
            int dataSize = pcm.Length;
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write(channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * (bitsPerSample / 8));
            bw.Write((short)(channels * (bitsPerSample / 8)));
            bw.Write(bitsPerSample);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);
            bw.Write(pcm);
            bw.Flush();
            return ms.ToArray();
        }

        private void PlayPcmWav(byte[] wav)
        {
            int offset = 44; // assume standard header
            var track = new AudioTrack(
                AudioStream.Music,
                SampleRate,
                ChannelConfiguration.Mono,
                Encoding.Pcm16bit,
                wav.Length - offset,
                AudioTrackMode.Static);
            track.Write(wav, offset, wav.Length - offset);
            track.Play();
        }
    }
}
