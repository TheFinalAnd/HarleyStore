using Microsoft.Maui.Controls.Platform;

namespace HarleyStore
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            Password.Clicked += Mail_Clicked;
            SignUp.Clicked += SignUp_Clicked;
        }

        private async void SignUp_Clicked(object? sender, EventArgs e)
        {
            if (EmailEntry.Text == "test@test.com" && PasswordEntry.Text == "123")
            {
                await Navigation.PushAsync(new Home());
            }
            else {
                await DisplayAlert("Error", "Acceso no autorizado", "OK");
            }
        }

        private async void Mail_Clicked(object? sender, EventArgs e)
        {
            if (PasswordEntry.IsPassword == false)
            {
                PasswordEntry.Placeholder = "************";
                PasswordEntry.IsPassword = true;
                Password.Source = "padlock_unlock.png";
            }
            else {
                PasswordEntry.IsPassword = false;
                PasswordEntry.Placeholder = "Ingrese contraseña";
                Password.Source = "padlock.png";
            }
        }
    }
}
