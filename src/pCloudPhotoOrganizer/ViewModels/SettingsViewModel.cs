using System.Collections.ObjectModel;

namespace pCloudPhotoOrganizer.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    public ObservableCollection<string> Folders { get; } = new();

    private string? _selectedFolder;
    public string? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                OnPropertyChanged(nameof(CanRemoveFolder));
            }
        }
    }

    public bool CanRemoveFolder => !string.IsNullOrEmpty(SelectedFolder);
}
