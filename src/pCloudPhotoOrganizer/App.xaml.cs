// Test Codex auto-apply
#if WINDOWS
using pCloudPhotoOrganizer.Platforms.Windows;
#endif

using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer
{
    public partial class App : Application
    {
        private readonly SettingsService _settings;

        public App(SettingsService settings)
        {
            InitializeComponent();
            _settings = settings;

            MainPage = new AppShell();

            // Si aucun dossier n'est configuré, on ouvre directement la page Paramètres
            if (!_settings.AreFoldersConfigured())
            {
                // Route "settings" définie dans AppShell.xaml
                (MainPage as Shell)?.GoToAsync("//settings");
            }
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

#if WINDOWS
            window.Created += (s, e) =>
            {
                window.SetMobilePortraitSize();
            };
#endif

            return window;
        }
    }
}


