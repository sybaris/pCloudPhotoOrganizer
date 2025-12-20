using Microsoft.Extensions.Logging;
using pCloudPhotoOrganizer.Services;
using pCloudPhotoOrganizer.ViewModels;
using pCloudPhotoOrganizer.Views;
namespace pCloudPhotoOrganizer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<AppLogService>();
            builder.Services.AddSingleton<MediaStoreService>();
            builder.Services.AddSingleton<PCloudAuthService>();
            builder.Services.AddSingleton<PCloudFileService>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<MediaDeletionService>();
            builder.Services.AddSingleton<LocalExportService>();
            //builder.Services.AddSingleton<ThumbnailCacheService>();

            // ViewModels
            builder.Services.AddSingleton<GalleryViewModel>();
            //builder.Services.AddTransient<AlbumCreationViewModel>();
            //builder.Services.AddTransient<SettingsViewModel>();

            // Views
            builder.Services.AddSingleton<Views.GalleryPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AboutPage>();
            builder.Services.AddTransient<Views.LogsPage>();
#if ANDROID
            Console.WriteLine("This is Android specific code.");
#endif
            return builder.Build();
        }
    }
}
