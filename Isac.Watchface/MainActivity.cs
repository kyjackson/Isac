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

            UpdateNow();
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (_, __) => RunOnUiThread(UpdateNow);
            _timer.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _timer?.Stop();
            _timer?.Dispose();
        }

        private void UpdateNow()
        {
            var cal = Calendar.Instance;
            var hour = cal.Get(CalendarField.HourOfDay).ToString().PadLeft(2, '0');
            var minute = cal.Get(CalendarField.Minute).ToString().PadLeft(2, '0');
            var date = $"{cal.Get(CalendarField.Year)}-{(cal.Get(CalendarField.Month)+1).ToString().PadLeft(2,'0')}-{cal.Get(CalendarField.DayOfMonth).ToString().PadLeft(2,'0')}";
            _txtTime!.Text = $"{hour}:{minute}";
            _txtDate!.Text = date;
            _txtIndicator!.Text = "ISAC";
        }
    }
}
