namespace Isac.Mobile
{
    public partial class AppShell : Shell
    {
        private DateTime _lastBackPress = DateTime.MinValue;
        private const int DoubleBackPressDelay = 2000; // 2 seconds

        public AppShell()
        {
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
            // Let Shell handle normal back navigation
            // Only intercept on home page for double-press to exit
            var currentRoute = CurrentState?.Location?.OriginalString;

            if (currentRoute != null && currentRoute.Contains("HomePage"))
            {
                if ((DateTime.Now - _lastBackPress).TotalMilliseconds < DoubleBackPressDelay)
                {
                    // Double press detected, allow app to close
                    return base.OnBackButtonPressed();
                }
                else
                {
                    _lastBackPress = DateTime.Now;
                    // Use MainThread to avoid disposed service provider
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Exit", "Press back again to exit", "OK");
                    });
                    return true; // Prevent exit on first press
                }
            }

            return base.OnBackButtonPressed();
        }
    }
}
