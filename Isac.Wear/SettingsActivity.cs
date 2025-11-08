using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Preferences;

namespace Isac.Wear;

[Activity(Label = "Settings")]
public class SettingsActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        layout.SetPadding(32, 32, 32, 32);

        var txt = new TextView(this) { Text = "API Base URL" };
        var edit = new EditText(this) { Hint = "https://..." };
        var btn = new Button(this) { Text = "Save" };

        var prefs = GetSharedPreferences("isac", FileCreationMode.Private);
        edit.Text = prefs.GetString("api_base", string.Empty);

        btn.Click += (s, e) =>
        {
            var url = edit.Text?.Trim() ?? string.Empty;
            using var editor = prefs.Edit();
            editor.PutString("api_base", url);
            editor.Commit();
            Toast.MakeText(this, "Saved", ToastLength.Short).Show();
            Finish();
        };

        layout.AddView(txt);
        layout.AddView(edit);
        layout.AddView(btn);
        SetContentView(layout);
    }
}
