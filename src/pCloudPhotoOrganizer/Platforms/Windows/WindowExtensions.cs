using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace pCloudPhotoOrganizer.Platforms.Windows;

public static class WindowExtensions
{
    public static void SetMobilePortraitSize(this Window window)
    {
        var mauiWindow = window.Handler.PlatformView as Microsoft.UI.Xaml.Window;

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mauiWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(900, 1600)); // Smartphone-style
    }
}
