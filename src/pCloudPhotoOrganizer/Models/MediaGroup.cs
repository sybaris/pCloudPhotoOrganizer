using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace pCloudPhotoOrganizer.Models;

public class MediaGroup : ObservableCollection<MediaItem>
{
    private bool _isAllSelected;

    public string Title { get; }
    public DateTime Date { get; }

    /// <summary>
    /// True si toutes les photos du groupe sont sélectionnées.
    /// </summary>
    public bool IsAllSelected
    {
        get => _isAllSelected;
        private set
        {
            if (_isAllSelected == value)
                return;

            _isAllSelected = value;
            // ObservableCollection expose déjà OnPropertyChanged
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsAllSelected)));
        }
    }

    public MediaGroup(string title, DateTime date, IEnumerable<MediaItem> items)
        : base(items)
    {
        Title = title;
        Date = date;

        // On s'abonne aux changements d'état des items déjà présents
        foreach (var item in this)
        {
            HookItem(item);
        }

        UpdateIsAllSelected();
    }

    protected override void InsertItem(int index, MediaItem item)
    {
        base.InsertItem(index, item);
        HookItem(item);
        UpdateIsAllSelected();
    }

    protected override void RemoveItem(int index)
    {
        var item = this[index];
        UnhookItem(item);
        base.RemoveItem(index);
        UpdateIsAllSelected();
    }

    protected override void ClearItems()
    {
        foreach (var item in this.ToList())
        {
            UnhookItem(item);
        }

        base.ClearItems();
        UpdateIsAllSelected();
    }

    private void HookItem(MediaItem item)
    {
        if (item != null)
            item.PropertyChanged += OnItemPropertyChanged;
    }

    private void UnhookItem(MediaItem item)
    {
        if (item != null)
            item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaItem.IsSelected))
        {
            UpdateIsAllSelected();
        }
    }

    private void UpdateIsAllSelected()
    {
        if (Count == 0)
        {
            IsAllSelected = false;
        }
        else
        {
            IsAllSelected = this.All(i => i.IsSelected);
        }
    }
}
