using Microsoft.Extensions.DependencyInjection;

namespace Isac.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Replace Shell with custom FlyoutPage implementation to avoid ShellFlyout crash
            var services = IPlatformApplication.Current?.Services ?? throw new InvalidOperationException("Service provider not available");
            return new Window(new Isac.Mobile.Pages.MainFlyoutPage(services));
        }
    }
}