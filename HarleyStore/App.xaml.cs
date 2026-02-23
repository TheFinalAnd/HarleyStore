using Microsoft.Extensions.DependencyInjection;

namespace HarleyStore
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Page? MainPage = new NavigationPage(new MainPage());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}