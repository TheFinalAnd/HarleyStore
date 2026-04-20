using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views;

public partial class NotificationsPage : ContentPage
{
    private readonly INotificationService _notificationService;

    public NotificationsPage()
    {
        InitializeComponent();
        _notificationService = ServiceHelper.GetService<INotificationService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var notis = await _notificationService.GetNotificacionesAsync();
        NotificacionesCollectionView.ItemsSource = notis;
    }
}
