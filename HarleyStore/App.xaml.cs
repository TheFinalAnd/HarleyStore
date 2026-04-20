namespace HarleyStore
{
    /// <summary>
    /// Punto de entrada de la aplicación MAUI.
    /// </summary>
    /// <remarks>
    /// Esta clase configura la página raíz de la aplicación. Se mantiene mínima
    /// por intención: la composición y registro de servicios se realiza en
    /// <see cref="MauiProgram"/> y la navegación en <c>AppShell</c>.
    /// </remarks>
    public partial class App : Application
    {
        public App()
        {
            // Inicializa recursos XAML y establece la Shell como página principal.
            InitializeComponent();
            MainPage = new AppShell();
        }
    }
}