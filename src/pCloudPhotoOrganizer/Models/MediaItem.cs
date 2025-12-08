using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace pCloudPhotoOrganizer.Models;

public class MediaItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string FilePath { get; set; } = string.Empty;

    public Uri? ContentUri { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long? Length { get; set; }

    public bool HasPersistablePermission { get; set; }

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

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FileName))
                return FileName;

            if (!string.IsNullOrWhiteSpace(FilePath))
                return Path.GetFileName(FilePath);

            if (ContentUri is not null)
                return ContentUri.Segments.LastOrDefault()?.Trim('/') ?? string.Empty;

            return string.Empty;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
