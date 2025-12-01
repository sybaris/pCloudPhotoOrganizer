using System.Linq;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.ViewModels;

namespace pCloudPhotoOrganizer.Views;

public partial class GalleryPage : ContentPage
{
    public GalleryPage(GalleryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is GalleryViewModel vm)
        {
            await vm.LoadAsync();
        }
    }

    private void OnGroupSelectionTapped(object sender, TappedEventArgs e)
    {
        if (BindingContext is not GalleryViewModel vm)
            return;

        if (sender is not Frame frame)
            return;

        // Le BindingContext du header = MediaGroup (groupe de photos par date)
        if (frame.BindingContext is not MediaGroup group)
            return;

        // MediaGroup hérite généralement de IEnumerable<MediaItem>
        // → on peut itérer directement dessus
        var items = group.OfType<MediaItem>().ToList();
        if (!items.Any())
            return;

        // Est-ce que toutes les photos de ce groupe sont déjà sélectionnées ?
        bool allSelected = items.All(i => i.IsSelected);
        bool newState = !allSelected; // si toutes sélectionnées → on désélectionne tout, sinon on sélectionne tout

        foreach (var item in items)
        {
            if (item.IsSelected == newState)
                continue;

            item.IsSelected = newState;

            if (newState)
            {
                if (!vm.SelectedItems.Contains(item))
                    vm.SelectedItems.Add(item);
            }
            else
            {
                vm.SelectedItems.Remove(item);
            }
        }

        // Mise à jour visuelle du rond de la date
        if (newState)
        {
            frame.BackgroundColor = Colors.DodgerBlue;
            frame.BorderColor = Colors.DodgerBlue;
            if (frame.Content is Label lbl)
            {
                lbl.Text = "✓";
                lbl.TextColor = Colors.White;
            }
        }
        else
        {
            frame.BackgroundColor = Color.FromArgb("#80FFFFFF");
            frame.BorderColor = Colors.LightGray;
            if (frame.Content is Label lbl)
            {
                lbl.Text = string.Empty;
                lbl.TextColor = Colors.Transparent;
            }
        }
    }

    private async void OnSendToPCloudClicked(object sender, EventArgs e)
    {
        if (BindingContext is not GalleryViewModel vm)
            return;

        if (vm.SelectedItems is null || vm.SelectedItems.Count == 0)
        {
            await DisplayAlert("pCloud", "Sélectionnez au moins une photo avant d'envoyer vers pCloud.", "OK");
            return;
        }

        // Déterminer la date de base pour le nom de dossier
        var distinctDates = vm.SelectedItems
            .Select(i => i.DateTaken.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        DateTime baseDate = distinctDates.Count == 1
            ? distinctDates[0]          // une seule date → on la prend
            : distinctDates.First();    // plusieurs dates → on prend la plus ancienne

        string suggestedName = $"{baseDate:yyyy.MM.dd} - ";

        // Ouvrir une page modale avec un Entry et le curseur à la fin du texte
        var tcs = new TaskCompletionSource<string?>();
        var popup = new PCloudAlbumNamePage(suggestedName, tcs);
        await Navigation.PushModalAsync(popup);

        string? albumName = await tcs.Task;

        if (string.IsNullOrWhiteSpace(albumName))
        {
            // Annulé ou vide → on ne fait rien
            return;
        }


        // POC : on affiche simplement ce qui serait envoyé
        int count = vm.SelectedItems.Count;
        string message = $"(POC) {count} élément(s) seraient envoyés vers pCloud dans le dossier :\n\n{albumName}";

        await DisplayAlert("pCloud (POC)", message, "OK");

        // 🚧 Étape suivante : appeler un service PCloudFileService pour uploader réellement
        // await _pcloudFileService.UploadAsync(albumName, vm.SelectedItems);
    }
}
