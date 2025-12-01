using System.Collections.ObjectModel;
using System.Windows.Input;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer.ViewModels;

public class GalleryViewModel : BaseViewModel
{
    private readonly MediaStoreService _mediaService;

    public GalleryViewModel(MediaStoreService mediaService)
    {
        _mediaService = mediaService;
        RefreshCommand = new Command(async () => await LoadAsync());
        ToggleSelectCommand = new Command<MediaItem>(ToggleSelect);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<MediaGroup> Groups { get; } = new();

    public ObservableCollection<MediaItem> SelectedItems { get; } = new();

    public bool IsEmpty => Groups.Count == 0;

    public Command RefreshCommand { get; }

    public ICommand ToggleSelectCommand { get; }

    private void ToggleSelect(MediaItem? item)
    {
        if (item is null)
            return;

        item.IsSelected = !item.IsSelected;

        if (item.IsSelected)
        {
            if (!SelectedItems.Contains(item))
                SelectedItems.Add(item);
        }
        else
        {
            SelectedItems.Remove(item);
        }

        // Si plus tard tu veux notifier sur IsEmpty / autre, on pourra le faire ici
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;

        Groups.Clear();
        SelectedItems.Clear();

        var medias = await _mediaService.GetAllMediaAsync();

        var groups = medias
            .GroupBy(x => x.DateTaken.Date)
            .OrderByDescending(g => g.Key);

        foreach (var g in groups)
        {
            // On s’assure que chaque item est désélectionné au chargement
            foreach (var item in g)
                item.IsSelected = false;

            Groups.Add(new MediaGroup(
                title: g.Key.ToString("yyyy-MM-dd"),
                date: g.Key,
                items: g.ToList()));
        }

        OnPropertyChanged(nameof(IsEmpty));

        IsLoading = false;
    }
}
