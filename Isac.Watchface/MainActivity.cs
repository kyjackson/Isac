using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Util;

namespace Isac.Watchface
{
    [Activity(Label = "ISAC Watchface", MainLauncher = true, Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private TextView? _txtTime;
        private TextView? _txtDate;
        private TextView? _txtIndicator;
        private System.Timers.Timer? _timer;
        private bool _ambient;
        private AmbientModeReceiver? _ambientReceiver;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_face);

            _txtTime = FindViewById<TextView>(Resource.Id.txtTime);
            _txtDate = FindViewById<TextView>(Resource.Id.txtDate);
            _txtIndicator = FindViewById<TextView>(Resource.Id.txtIndicator);

            var root = FindViewById(Resource.Id.txtTime)!.RootView!;
            root.Click += (_, __) =>
            {
                if (_ambient) return; // ignore taps in ambient
                try
                {
                    var intent = new Intent();
                    intent.SetClassName(PackageName.Replace("Watchface", "Wear"), "Isac.Wear.MainActivity");
                    intent.AddFlags(ActivityFlags.NewTask);
                    StartActivity(intent);
                }
                catch
                {
                    Toast.MakeText(this, "ISAC app not found", ToastLength.Short).Show();
                }
            };

            RegisterAmbientReceiver();
            ScheduleTimer(1000);
            UpdateNow();
        }

        private void RegisterAmbientReceiver()
        {
            _ambientReceiver = new AmbientModeReceiver(this);
            var filter = new IntentFilter();
            // ACTION_AMBIENT_MODE_CHANGED is internal; fallback: listen to screen on/off to simulate ambient
            filter.AddAction(Intent.ActionScreenOff);
            filter.AddAction(Intent.ActionScreenOn);
            RegisterReceiver(_ambientReceiver, filter);
        }

        private void ScheduleTimer(double intervalMs)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += (_, __) => RunOnUiThread(UpdateNow);
            _timer.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _timer?.Stop();
            _timer?.Dispose();
            if (_ambientReceiver != null)
            {
                UnregisterReceiver(_ambientReceiver);
                _ambientReceiver.Dispose();
            }
        }

        internal void SetAmbient(bool ambient)
        {
            if (_ambient == ambient) return;
            _ambient = ambient;
            ScheduleTimer(_ambient ? 60_000 : 1000);
            UpdateNow();
        }

        private void UpdateNow()
        {
            var cal = Calendar.Instance;
            var hour = cal.Get(CalendarField.HourOfDay).ToString().PadLeft(2, '0');
            var minute = cal.Get(CalendarField.Minute).ToString().PadLeft(2, '0');
            _txtTime!.Text = $"{hour}:{minute}";
            if (_ambient)
            {
                _txtDate!.Text = string.Empty;
                _txtIndicator!.Text = string.Empty;
            }
            else
            {
                var date = $"{cal.Get(CalendarField.Year)}-{(cal.Get(CalendarField.Month)+1).ToString().PadLeft(2,'0')}-{cal.Get(CalendarField.DayOfMonth).ToString().PadLeft(2,'0')}";
                _txtDate!.Text = date;
                _txtIndicator!.Text = "ISAC";
            }
        }

        private class AmbientModeReceiver : BroadcastReceiver
        {
            private readonly MainActivity _activity;
            public AmbientModeReceiver(MainActivity activity) => _activity = activity;
            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent?.Action == Intent.ActionScreenOff)
                {
                    _activity.SetAmbient(true);
                }
                else if (intent?.Action == Intent.ActionScreenOn)
                {
                    _activity.SetAmbient(false);
                }
            }
        }
    }
}
