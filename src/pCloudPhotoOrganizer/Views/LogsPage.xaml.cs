using System;
using System.Threading.Tasks;
using pCloudPhotoOrganizer.Services;

namespace pCloudPhotoOrganizer.Views;

public partial class LogsPage : ContentPage
{
    private readonly AppLogService _logService;

    public LogsPage(AppLogService logService)
    {
        InitializeComponent();
        _logService = logService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshLogsAsync();
    }

    private async Task RefreshLogsAsync()
    {
        LogsStatusLabel.Text = "Chargement...";
        LogsEditor.Text = string.Empty;

        try
        {
            var content = await _logService.GetCurrentLogContentAsync();
            LogsEditor.Text = string.IsNullOrWhiteSpace(content)
                ? "Aucun log pour aujourd'hui."
                : content;
            LogsStatusLabel.Text = $"Dernière mise à jour : {DateTime.Now:HH:mm:ss}";
        }
        catch
        {
            LogsStatusLabel.Text = "Impossible de charger les logs.";
        }
    }

    private async void OnRefreshLogsClicked(object sender, EventArgs e)
    {
        await RefreshLogsAsync();
    }

    private async void OnClearLogsClicked(object sender, EventArgs e)
    {
        await _logService.ClearCurrentLogAsync();
        await RefreshLogsAsync();
    }
}
