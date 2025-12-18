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
        private AppShell? _rootShell;
        private Window? _rootWindow;
        private bool _startupPermissionsRequested;

        public App(SettingsService settings)
        {
            InitializeComponent();
            _settings = settings;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            _rootShell = shell;

            var window = new Window(shell);
            _rootWindow = window;

            InitializeRootNavigation(shell);
            InitializePlatformHooks(window);

#if ANDROID
            RequestAndroidStartupPermissions();
#endif

            return window;
        }

        private void InitializeRootNavigation(Shell shell)
        {
            if (_settings.AreFoldersConfigured())
                return;

            shell.Dispatcher.Dispatch(() => _ = shell.GoToAsync("//settings"));
        }

        private void InitializePlatformHooks(Window window)
        {
#if WINDOWS
            window.Created += (s, e) => window.SetMobilePortraitSize();
#endif
        }

#if ANDROID
        private void RequestAndroidStartupPermissions()
        {
            if (_startupPermissionsRequested)
                return;

            _startupPermissionsRequested = true;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MediaPermissionHelper.EnsureStartupPermissionsAsync(GetActivePage);
            });
        }
#endif

        private Page? GetActivePage()
            => _rootWindow?.Page ?? Windows.FirstOrDefault()?.Page ?? _rootShell;
    }
}


