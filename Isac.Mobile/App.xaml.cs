using Microsoft.Extensions.DependencyInjection;

namespace Isac.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Force dark theme
            Application.Current.UserAppTheme = AppTheme.Dark;

            MainPage = new AppShell();
        }
    }
}