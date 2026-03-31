namespace HarleyStore.Views;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private async void OnGetStartedClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//login");
    }
}