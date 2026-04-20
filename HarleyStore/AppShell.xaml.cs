using System;
namespace HarleyStore
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Views.MyPublishedMotosPage), typeof(Views.MyPublishedMotosPage));
            Routing.RegisterRoute(nameof(Views.EditMotoPage), typeof(Views.EditMotoPage));

            Routing.RegisterRoute(nameof(Views.CategoriesPage), typeof(Views.CategoriesPage));
            Routing.RegisterRoute(nameof(Views.MotorcycleListPage), typeof(Views.MotorcycleListPage));
            Routing.RegisterRoute(nameof(Views.MotorcycleDetailPage), typeof(Views.MotorcycleDetailPage));
            Routing.RegisterRoute(nameof(Views.FavoritesPage), typeof(Views.FavoritesPage));
            Routing.RegisterRoute(nameof(Views.PublishMotoPage), typeof(Views.PublishMotoPage));
            Routing.RegisterRoute(nameof(Views.ProfilePage), typeof(Views.ProfilePage));
            Routing.RegisterRoute(nameof(Views.NotificationsPage), typeof(Views.NotificationsPage));
            Routing.RegisterRoute(nameof(Views.OfferPage), typeof(Views.OfferPage));
            Routing.RegisterRoute(nameof(Views.MyOffersPage), typeof(Views.MyOffersPage));
            Routing.RegisterRoute(nameof(Views.MyPaymentsPage), typeof(Views.MyPaymentsPage));
        }
    }
}