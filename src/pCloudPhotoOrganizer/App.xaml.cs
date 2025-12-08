// Test Codex auto-apply
#if WINDOWS
using pCloudPhotoOrganizer.Platforms.Windows;
#endif

using pCloudPhotoOrganizer.Services;
#if ANDROID
using Microsoft.Maui.ApplicationModel;
using pCloudPhotoOrganizer.Platforms.Android;
#endif

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

            // Si aucun dossier n'est configure, on ouvre directement la page Parametres
            if (!_settings.AreFoldersConfigured())
            {
                // Route "settings" definie dans AppShell.xaml
                (MainPage as Shell)?.GoToAsync("//settings");
            }

#if ANDROID
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await MediaPermissionHelper.EnsureStartupPermissionsAsync(() => MainPage);
                }
                catch (Exception)
                {
                    // The helper already shows a friendly message and logs details.
                }
            });
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
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


