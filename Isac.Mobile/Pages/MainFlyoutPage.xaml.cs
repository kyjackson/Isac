using Microsoft.Maui.Controls;

namespace Isac.Mobile.Pages
{
    public partial class MainFlyoutPage : FlyoutPage
    {
        private readonly IServiceProvider _services;

        public MainFlyoutPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            
            // Set the default page
            var homePage = _services.GetRequiredService<HomePage>();
            Detail = new NavigationPage(homePage);

            UpdateSelection(HomeButton);
        }

        private void OnHomeClicked(object sender, EventArgs e)
        {
            var homePage = _services.GetRequiredService<HomePage>();
            Detail = new NavigationPage(homePage);
            
            UpdateSelection(HomeButton);
            IsPresented = false;
        }

        private void OnEnrollClicked(object sender, EventArgs e)
        {
            var enrollPage = _services.GetRequiredService<EnrollmentPage>();
            Detail = new NavigationPage(enrollPage);

            UpdateSelection(EnrollButton);
            IsPresented = false;
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            var settingsPage = _services.GetRequiredService<SettingsPage>();
            Detail = new NavigationPage(settingsPage);

            UpdateSelection(SettingsButton);
            IsPresented = false;
        }

        private void UpdateSelection(Border selectedButton)
        {
            // Reset all buttons to transparent
            HomeButton.BackgroundColor = Colors.Transparent;
            EnrollButton.BackgroundColor = Colors.Transparent;
            SettingsButton.BackgroundColor = Colors.Transparent;

            // Set selected button background
            selectedButton.BackgroundColor = Color.FromHex("#3C2D2E");
        }
    }
}