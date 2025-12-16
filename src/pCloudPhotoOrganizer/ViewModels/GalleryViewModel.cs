using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using pCloudPhotoOrganizer.Models;
using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer.ViewModels;

public class GalleryViewModel : BaseViewModel
{
    private readonly MediaStoreService _mediaService;

    public GalleryViewModel(MediaStoreService mediaService)
    {
        Debug.WriteLine($"[GalleryViewModel] ctor instance={GetHashCode()}");
        _mediaService = mediaService;
        RefreshCommand = new Command(async () => await LoadAsync(force: true));
        ToggleSelectCommand = new Command<MediaItem>(ToggleSelect);
        ToggleGroupSelectCommand = new Command<MediaGroup>(ToggleGroupSelection);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _isUploading;
    public bool IsUploading
    {
        get => _isUploading;
        set => SetProperty(ref _isUploading, value);
    }

    private string _uploadStatus = string.Empty;
    public string UploadStatus
    {
        get => _uploadStatus;
        set => SetProperty(ref _uploadStatus, value);
    }

    public ObservableCollection<MediaGroup> Groups { get; } = new();

    public ObservableCollection<MediaItem> SelectedItems { get; } = new();

    public bool IsEmpty => Groups.Count == 0;

    public Command RefreshCommand { get; }

    public ICommand ToggleSelectCommand { get; }
    public ICommand ToggleGroupSelectCommand { get; }

    private bool _hasLoadedOnce;

    private void ToggleSelect(MediaItem? item)
    {
        if (item is null)
            return;

        item.IsSelected = !item.IsSelected;
        // SelectedItems is synchronized via OnItemSelectionChanged handlers
    }

    private void ToggleGroupSelection(MediaGroup? group)
    {
        if (group is null || group.Count == 0)
            return;

        var newState = !group.All(i => i.IsSelected);
        foreach (var item in group)
        {
            item.IsSelected = newState;
        }
    }

    public async Task LoadAsync(bool force = false)
    {
        if (IsLoading)
            return;

        if (_hasLoadedOnce && !force)
        {
            Debug.WriteLine("[GalleryViewModel] LoadAsync skipped (already loaded, not forced).");
            return;
        }

        Debug.WriteLine($"[GalleryViewModel] LoadAsync start (force={force})");
        IsLoading = true;
        try
        {
            UnhookAllGroups();
            Groups.Clear();
            SelectedItems.Clear();

            var medias = await _mediaService.GetAllMediaAsync();

            var groups = medias
                .GroupBy(x => x.DateTaken.Date)
                .OrderByDescending(g => g.Key);

            foreach (var g in groups)
            {
                // On s'assure que chaque item est désélectionné au chargement
                foreach (var item in g)
                    item.IsSelected = false;

                var group = new MediaGroup(
                    title: g.Key.ToString("yyyy-MM-dd"),
                    date: g.Key,
                    items: g.ToList());

                HookGroup(group);
                Groups.Add(group);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsLoading = false;
        }

        _hasLoadedOnce = true;
        Debug.WriteLine("[GalleryViewModel] LoadAsync end");
    }

    private void HookGroup(MediaGroup group)
    {
        group.CollectionChanged += OnGroupCollectionChanged;
        foreach (var item in group)
            HookItem(item);
    }

    private void UnhookGroup(MediaGroup group)
    {
        group.CollectionChanged -= OnGroupCollectionChanged;
        foreach (var item in group)
            UnhookItem(item);
    }

    private void OnGroupCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<MediaItem>())
                UnhookItem(item);
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<MediaItem>())
                HookItem(item);
        }
    }

    private void HookItem(MediaItem item)
    {
        item.PropertyChanged += OnItemSelectionChanged;
    }

    private void UnhookItem(MediaItem item)
    {
        item.PropertyChanged -= OnItemSelectionChanged;
    }

    private void OnItemSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MediaItem.IsSelected) || sender is not MediaItem item)
            return;

        if (item.IsSelected)
        {
            if (!SelectedItems.Contains(item))
                SelectedItems.Add(item);
        }
        else
        {
            SelectedItems.Remove(item);
        }

        Debug.WriteLine($"[GalleryViewModel] SelectedItems count={SelectedItems.Count} (item={item.DisplayName}, selected={item.IsSelected})");
    }

    private void UnhookAllGroups()
    {
        foreach (var group in Groups.ToList())
            UnhookGroup(group);
    }
}
