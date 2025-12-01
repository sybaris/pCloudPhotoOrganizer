using System.Threading.Tasks;

namespace pCloudPhotoOrganizer.Views;

public class PCloudAlbumNamePage : ContentPage
{
    private readonly Entry _entry;
    private readonly TaskCompletionSource<string?> _tcs;

    public PCloudAlbumNamePage(string suggestedName, TaskCompletionSource<string?> tcs)
    {
        _tcs = tcs;

        Title = "Nouvel album pCloud";

        _entry = new Entry
        {
            Text = suggestedName,
            HorizontalOptions = LayoutOptions.Fill,
            Keyboard = Keyboard.Text,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        var okButton = new Button
        {
            Text = "OK",
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 10, 0, 0)
        };
        okButton.Clicked += async (s, e) =>
        {
            var text = _entry.Text ?? string.Empty;
            await Navigation.PopModalAsync();
            _tcs.TrySetResult(text);
        };

        var cancelButton = new Button
        {
            Text = "Annuler",
            HorizontalOptions = LayoutOptions.Start
        };
        cancelButton.Clicked += async (s, e) =>
        {
            await Navigation.PopModalAsync();
            _tcs.TrySetResult(null);
        };

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(20, 40),
            Spacing = 15,
            Children =
            {
                new Label
                {
                    Text = "Nom du dossier :",
                    FontAttributes = FontAttributes.Bold
                },
                _entry,
                new HorizontalStackLayout
                {
                    HorizontalOptions = LayoutOptions.EndAndExpand,
                    Spacing = 10,
                    Children =
                    {
                        cancelButton,
                        okButton
                    }
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Focus + curseur à la fin du texte
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _entry.Focus();
            if (!string.IsNullOrEmpty(_entry.Text))
            {
                _entry.CursorPosition = _entry.Text.Length;
                _entry.SelectionLength = 0;
            }
        });
    }
}
