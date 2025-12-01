using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace pCloudPhotoOrganizer.Models;

public class MediaItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string FilePath { get; set; } = string.Empty;

    public DateTime DateTaken { get; set; }

    public ImageSource? Thumbnail { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
