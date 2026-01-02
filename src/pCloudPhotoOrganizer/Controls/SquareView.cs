using Microsoft.Maui.Controls;

namespace pCloudPhotoOrganizer.Controls;

public class SquareView : ContentView
{
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0 && Math.Abs(HeightRequest - width) > double.Epsilon)
        {
            HeightRequest = width;
        }
    }
}
