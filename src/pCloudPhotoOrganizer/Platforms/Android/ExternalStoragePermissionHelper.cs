#if ANDROID
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Application = Microsoft.Maui.Controls.Application;
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;
using AndroidEnvironment = global::Android.OS.Environment;

namespace pCloudPhotoOrganizer.Platforms.Android;

public static class ExternalStoragePermissionHelper
{
    private const string PermissionMessage = "Pour exporter vers ce dossier, autorisez \"Accès à tous les fichiers\" pour l'application dans les paramètres Android.";

    public static async Task EnsureAllFilesAccessAsync(Func<Page?>? pageProvider = null)
    {
        if (AndroidEnvironment.IsExternalStorageManager)
            return;

        bool openSettings = await AskUserToOpenSettingsAsync(pageProvider);
        if (openSettings)
        {
            LaunchManageAllFilesIntent();
        }

        throw new UnauthorizedAccessException("L'application n'a pas l'autorisation \"Accès à tous les fichiers\". Activez-la dans les paramètres puis relancez l'export.");
    }

    private static async Task<bool> AskUserToOpenSettingsAsync(Func<Page?>? pageProvider)
    {
        try
        {
            var page = pageProvider?.Invoke() ?? Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null)
                return true;

            return await MainThread.InvokeOnMainThreadAsync(() =>
                page.DisplayAlert(
                    "Autorisation requise",
                    PermissionMessage,
                    "Ouvrir les paramètres",
                    "Annuler"));
        }
        catch
        {
            return true;
        }
    }

    private static void LaunchManageAllFilesIntent()
    {
        var context = AndroidApp.Context ?? throw new InvalidOperationException("Contexte Android indisponible.");

        try
        {
            var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
            intent.SetData(AndroidUri.Parse($"package:{context.PackageName}"));
            intent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch (ActivityNotFoundException)
        {
            var fallbackIntent = new Intent(Settings.ActionManageAllFilesAccessPermission);
            fallbackIntent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(fallbackIntent);
        }
    }
}
#endif
