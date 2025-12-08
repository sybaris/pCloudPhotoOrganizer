/*
WEB-DAV LIMITATIONS OF PCLOUD (MANDATORY FOR ALL CODE):
- PROPFIND is NOT supported
- MKCOL is NOT supported
- COPY is NOT supported
- MOVE is NOT supported
- PROPPATCH is NOT supported
- Directory existence CANNOT be checked via WebDAV
- Folder listing CANNOT be done via WebDAV
- The ONLY supported WebDAV verbs are:
  OPTIONS, GET, HEAD, POST, PUT, DELETE, TRACE, PATCH
- Upload MUST be done using PUT or POST only
- Directory creation MUST be done via pCloud public API:
     https://eapi.pcloud.com/createfolderifnotexists
- Folder existence MUST be checked using pCloud API:
     https://eapi.pcloud.com/listfolder
- The final file upload should use direct WebDAV PUT:
     https://ewebdav.pcloud.com/<remote-path>/<filename>
------------------------------------------------------------
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Services;
using pCloudPhotoOrganizer.ViewModels;

namespace pCloudPhotoOrganizer.Views;

public partial class GalleryPage : ContentPage
{
    private readonly GalleryViewModel _vm;
    private readonly PCloudFileService _fileService;
    private readonly SettingsService _settings;
    private readonly MediaDeletionService _deletionService;

    public GalleryPage(GalleryViewModel vm, PCloudFileService fileService, PCloudAuthService authService, SettingsService settings, MediaDeletionService deletionService)
    {
        InitializeComponent();
        _vm = vm;
        _fileService = fileService;
        _settings = settings;
        _deletionService = deletionService;
        BindingContext = _vm;
        _ = authService; // kept for DI compatibility
        Debug.WriteLine($"[GalleryPage] ctor, VM instance={_vm.GetHashCode()}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("[GalleryPage] OnAppearing");

        if (BindingContext is GalleryViewModel vm)
        {
            await vm.LoadAsync();
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Debug.WriteLine($"[GalleryPage] BindingContext changed to {BindingContext?.GetType().Name} instance={BindingContext?.GetHashCode()}");
    }

    private async void OnSendToPCloudClicked(object sender, EventArgs e)
    {
        if (_vm.SelectedItems is null || _vm.SelectedItems.Count == 0)
        {
            await DisplayAlert("pCloud", "Sélectionnez au moins une photo avant d'envoyer vers pCloud.", "OK");
            return;
        }

        // Déterminer la date de base pour le nom de dossier
        var distinctDates = _vm.SelectedItems
            .Select(i => i.DateTaken.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        DateTime baseDate = distinctDates.Count == 1
            ? distinctDates[0]          // une seule date → on la prend
            : distinctDates.First();    // plusieurs dates → on prend la plus ancienne

        string suggestedName = $"{baseDate:yyyy.MM.dd} - ";

        // Ouvrir une page modale avec un Entry et le curseur à la fin du texte
        var tcs = new TaskCompletionSource<PCloudAlbumSelection?>();
        var popup = new PCloudAlbumNamePage(suggestedName, tcs);
        await Navigation.PushModalAsync(popup);

        var selection = await tcs.Task;

        if (selection is null || string.IsNullOrWhiteSpace(selection.AlbumName))
        {
            // Annulé ou vide → on ne fait rien
            return;
        }


        // POC : on affiche simplement ce qui serait envoyé
        var (user, password, root) = await GetPCloudCredentialsAsync();
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("pCloud", "Renseignez vos identifiants pCloud dans les paramètres.", "OK");
            return;
        }

        string targetPath = CombinePaths(string.IsNullOrWhiteSpace(root) ? "/" : root, selection.AlbumName);

        _vm.IsUploading = true;
        _vm.UploadStatus = "Préparation de l'upload...";

        var itemsToDelete = new List<MediaItem>();

        try
        {
            await _fileService.EnsureFolderExistsAsync(user!, password!, targetPath);

            int total = _vm.SelectedItems.Count;
            int index = 0;

            foreach (var item in _vm.SelectedItems.ToList())
            {
                index++;
                _vm.UploadStatus = $"Upload {index}/{total} : {item.DisplayName}";

                var progress = new Progress<double>(p =>
                {
                    _vm.UploadStatus = $"Upload {index}/{total} : {item.DisplayName} ({p:P0})";
                });

                await _fileService.UploadAsync(user!, password!, targetPath, item, progress);

                if (selection.MoveFiles)
                {
                    itemsToDelete.Add(item);
                }
            }

            _vm.UploadStatus = selection.MoveFiles ? "Déplacement terminé" : "Upload terminé";
            await DisplayAlert("pCloud", "Envoi terminé !", "OK");
        }
        catch (PCloudAuthenticationException)
        {
            _vm.UploadStatus = "Identifiants pCloud invalides";
            await DisplayAlert("pCloud", "Identifiants pCloud invalides. Vérifiez votre login/mot de passe.", "OK");
        }
        catch (PCloudUploadException ex)
        {
            _vm.UploadStatus = "Echec de l'upload vers pCloud";
            await DisplayAlert("pCloud", $"Échec du téléversement (HTTP {(int)ex.StatusCode}) : {ex.ResponseBody}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("pCloud", $"Erreur durant l'envoi : {ex.Message}", "OK");
        }
        finally
        {
            if (selection.MoveFiles && itemsToDelete.Count > 0)
            {
                await _deletionService.DeleteAsync(itemsToDelete);
            }

            _vm.IsUploading = false;
        }
    }

    private async Task<(string? user, string? password, string? root)> GetPCloudCredentialsAsync()
    {
        var user = await _settings.GetPCloudUsernameAsync();
        var password = await _settings.GetPCloudPasswordAsync();
        var root = _settings.GetPCloudRootFolder();
        return (user, password, root);
    }

    private static string CombinePaths(string root, string album)
    {
        root = string.IsNullOrWhiteSpace(root) ? "/" : root.TrimEnd('/');
        album = album.Trim();
        if (!root.StartsWith("/"))
            root = "/" + root;
        return root == "/" ? $"/{album}" : $"{root}/{album}";
    }
}
