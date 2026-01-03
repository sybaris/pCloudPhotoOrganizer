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
using System.Diagnostics;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Services;
using pCloudPhotoOrganizer.ViewModels;

namespace pCloudPhotoOrganizer.Views;

public partial class GalleryPage : ContentPage
{
    private readonly GalleryViewModel _vm;
    private readonly PCloudFileService _fileService;
    private readonly SettingsService _settings;
    private readonly LocalExportService _localExportService;
    private readonly MediaDeletionService _deletionService;
    private readonly AppLogService _logService;

    public GalleryViewModel ViewModel => _vm;

    public GalleryPage(GalleryViewModel vm, PCloudFileService fileService, PCloudAuthService authService, SettingsService settings, LocalExportService localExportService, MediaDeletionService deletionService, AppLogService logService)
    {
        _vm = vm;
        InitializeComponent();
        _fileService = fileService;
        _settings = settings;
        _localExportService = localExportService;
        _deletionService = deletionService;
        _logService = logService;
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
            await DisplayAlert("Sélection", "Sélectionnez au moins une photo ou vidéo avant de faire un transfert.", "OK");
            await _logService.LogInfo("Tentative de transfert sans sélection.");
            return;
        }

        var selectionSnapshot = _vm.SelectedItems.ToList();

        var selection = await AlbumNameDialogPopup(selectionSnapshot);
        if (selection == null)
            return;

            var exportMode = _settings.GetExportMode();

            if (exportMode == ExportMode.Local)
                await ExportLocallyAsync(selectionSnapshot, selection);
            else
                await ExportToPCloudAsync(selectionSnapshot, selection);
         DeselectItems(selectionSnapshot);
    }

    private async Task ExportLocallyAsync(List<MediaItem> selectedItems, PCloudAlbumSelection selection)
    {
        bool moveFiles = selection.MoveFiles;
        _vm.IsUploading = true;
        _vm.UploadStatus = "Préparation de l'export local...";
        var batchStopwatch = Stopwatch.StartNew();

        try
        {
            await _logService.LogInfo($"Début d'export local de {selectedItems.Count} fichier(s) (déplacement={moveFiles}).");
#if ANDROID
            await LocalExportService.EnsureAllFilesAccessAsync();
#endif
            var configuredFolder = _settings.GetLocalExportPath();
            if (string.IsNullOrWhiteSpace(configuredFolder))
                throw new InvalidOperationException("Aucun dossier local n'est configuré dans les paramètres.");

            var destinationFolder = _localExportService.EnsureDestinationFolderExists(configuredFolder, selection.AlbumName);

            int total = selectedItems.Count;
            int index = 0;

            foreach (var item in selectedItems)
            {
                index++;
                var itemLabel = item.DisplayName ?? item.FileName ?? item.FilePath ?? "media";
                _vm.UploadStatus = $"Export local {index}/{total} : {itemLabel}";

                var fileStopwatch = Stopwatch.StartNew();
                try
                {
                    await _localExportService.CopyAsync(item, destinationFolder);

                    fileStopwatch.Stop();

                    await _logService.LogOperation($"{(moveFiles ? "Déplacement" : "Copie")} locale  '{itemLabel}' en {fileStopwatch.Elapsed.TotalSeconds:F2}s.");
                }
                catch (Exception ex)
                {
                    fileStopwatch.Stop();
                    await _logService.LogError(ex, $"Erreur durant l'export local de '{itemLabel}'.");
                    throw;
                }
            }
            if (moveFiles)
            {
                await _localExportService.DeleteAsync(selectedItems);
            }

            batchStopwatch.Stop();
            await _logService.LogOperation($"Export local terminé ({total} fichier(s)) en {batchStopwatch.Elapsed.TotalSeconds:F2}s.");
            _vm.UploadStatus = moveFiles ? "Déplacement local terminé" : "Copie locale terminée";
            await DisplayAlert("Export local", moveFiles ? "Déplacement local terminé !" : "Copie locale terminée !", "OK");
        }
        catch (InvalidOperationException ex)
        {
            batchStopwatch.Stop();
            _vm.UploadStatus = "Configuration locale invalide";
            await _logService.LogError(ex, "Paramètres d'export local invalides.");
            await DisplayAlert("Export local", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            batchStopwatch.Stop();
            _vm.UploadStatus = "Echec de l'export local";
            await _logService.LogError(ex, "Erreur inattendue durant l'export local.");
            await DisplayAlert("Export local", $"Erreur durant l'export local : {ex.Message}", "OK");
        }
        finally
        {
            _vm.IsUploading = false;
        }
    }

    private async Task ExportToPCloudAsync(List<MediaItem> selectedItems, PCloudAlbumSelection selection)
    {
        var (user, password, root) = await GetPCloudCredentialsAsync();
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("pCloud", "Renseignez vos identifiants pCloud dans les paramètres.", "OK");
            await _logService.LogInfo("Envoi vers pCloud interrompu : identifiants manquants.");
            return;
        }

        string targetPath = CombinePaths(string.IsNullOrWhiteSpace(root) ? "/" : root, selection.AlbumName);

        _vm.IsUploading = true;
        _vm.UploadStatus = "Préparation de l'upload...";

        var itemsToDelete = new List<MediaItem>();
        var batchStopwatch = Stopwatch.StartNew();
        await _logService.LogInfo($"Début d'envoi de {selectedItems.Count} fichier(s) vers '{targetPath}' (déplacement={selection.MoveFiles}).");

        try
        {
            await _fileService.EnsureFolderExistsAsync(user!, password!, targetPath);

            int total = selectedItems.Count;
            int index = 0;

            foreach (var item in selectedItems)
            {
                index++;
                _vm.UploadStatus = $"Upload {index}/{total} : {item.DisplayName}";

                var progress = new Progress<double>(p =>
                {
                    _vm.UploadStatus = $"Upload {index}/{total} : {item.DisplayName} ({p:P0})";
                });

                var fileStopwatch = Stopwatch.StartNew();
                try
                {
                    await _fileService.UploadAsync(user!, password!, targetPath, item, progress);
                    fileStopwatch.Stop();
                    await _logService.LogOperation($"Copie '{item.DisplayName ?? item.FileName ?? item.FilePath}' -> '{targetPath}' en {fileStopwatch.Elapsed.TotalSeconds:F2}s.");
                }
                catch (Exception ex)
                {
                    fileStopwatch.Stop();
                    await _logService.LogError(ex, $"Erreur durant l'upload de '{item.DisplayName ?? item.FileName ?? item.FilePath}' vers '{targetPath}'.");
                    throw;
                }

                if (selection.MoveFiles)
                {
                    itemsToDelete.Add(item);
                }
            }

            batchStopwatch.Stop();
            await _logService.LogOperation($"Copié {total} fichier(s) vers '{targetPath}' en {batchStopwatch.Elapsed.TotalSeconds:F2}s.");

            _vm.UploadStatus = selection.MoveFiles ? "Déplacement terminé" : "Upload terminé";
            await DisplayAlert("pCloud", "Envoi terminé !", "OK");
        }
        catch (PCloudAuthenticationException ex)
        {
            batchStopwatch.Stop();
            _vm.UploadStatus = "Identifiants pCloud invalides";
            await _logService.LogError(ex, "Identifiants pCloud invalides (WebDAV ou API).");
            await DisplayAlert("pCloud", "Identifiants pCloud invalides. Vérifiez votre login/mot de passe.", "OK");
        }
        catch (PCloudUploadException ex)
        {
            batchStopwatch.Stop();
            _vm.UploadStatus = "Echec de l'upload vers pCloud";
            await _logService.LogError(ex, $"Erreur HTTP {(int)ex.StatusCode} durant l'upload vers '{targetPath}'.");
            await DisplayAlert("pCloud", $"Échec du téléversement (HTTP {(int)ex.StatusCode}) : {ex.ResponseBody}", "OK");
        }
        catch (Exception ex)
        {
            batchStopwatch.Stop();
            await _logService.LogError(ex, "Erreur inattendue durant l'envoi vers pCloud.");
            await DisplayAlert("pCloud", $"Erreur durant l'envoi : {ex.Message}", "OK");
        }
        finally
        {
            if (selection.MoveFiles && itemsToDelete.Count > 0)
            {
                var deleteStopwatch = Stopwatch.StartNew();
                await _deletionService.DeleteAsync(itemsToDelete);
                deleteStopwatch.Stop();
                await _logService.LogOperation($"Suppression locale de {itemsToDelete.Count} fichier(s) en {deleteStopwatch.Elapsed.TotalSeconds:F2}s.");
            }

            _vm.IsUploading = false;
        }
    }

    private async Task<PCloudAlbumSelection?> AlbumNameDialogPopup(List<MediaItem> selectedItems)
    {
        var distinctDates = selectedItems
            .Select(i => i.DateTaken.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        DateTime baseDate = distinctDates.Count == 1
            ? distinctDates[0]
            : distinctDates.First();

        string suggestedName = $"{baseDate:yyyy.MM.dd} - ";
        var defaultMoveMode = _settings.GetDefaultMoveMode();

        var tcs = new TaskCompletionSource<PCloudAlbumSelection?>();
        var popup = new AlbumNameDialog(suggestedName, defaultMoveMode, tcs);
        await Navigation.PushModalAsync(popup);

        PCloudAlbumSelection? selection = await tcs.Task;
        if (selection is null || string.IsNullOrWhiteSpace(selection.AlbumName))
        {
            await _logService.LogInfo("Transfert annulé depuis la boîte de dialogue d'album.");
            return null;
        }

        return selection;
    }

    private async Task<(string? user, string? password, string? root)> GetPCloudCredentialsAsync()
    {
        var user = await _settings.GetPCloudUsernameAsync();
        var password = await _settings.GetPCloudPasswordAsync();
        var root = _settings.GetPCloudRootFolder();
        return (user, password, root);
    }

    private void DeselectItems(IEnumerable<MediaItem> items)
    {
        foreach (var item in items)
            item.IsSelected = false;
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
