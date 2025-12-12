using System;
#if ANDROID
using Microsoft.Maui.ApplicationModel;
#endif

namespace pCloudPhotoOrganizer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private void OnQuitClicked(object sender, EventArgs e)
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity is not null)
            {
                activity.FinishAffinity();
                return;
            }
#endif

#if WINDOWS
            Application.Current?.Quit();
#else
            Application.Current?.Quit();
#endif
        }
    }
}
