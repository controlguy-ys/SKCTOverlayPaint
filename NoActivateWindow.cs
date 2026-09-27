using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SKCTOverlayPaint;

internal abstract class NoActivateWindow : Window
{
    protected NoActivateWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Focusable = false;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        source.AddHook(WindowProcedure);
        NativeWindowStyles.PreventActivation(source.Handle);
    }

    private static nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeWindowStyles.WmMouseActivate)
        {
            handled = true;
            return NativeWindowStyles.MaNoActivate;
        }

        return 0;
    }
}

internal static class NativeWindowStyles
{
    internal const int WmMouseActivate = 0x0021;
    internal static readonly nint MaNoActivate = 3;

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExAppWindow = 0x00040000;
    private const long WsExNoActivate = 0x08000000;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenPoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out ScreenPoint point);

    internal static void PreventActivation(nint hwnd)
    {
        long style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        style = (style | WsExNoActivate | WsExToolWindow) & ~WsExAppWindow;
        ApplyStyle(hwnd, style);
    }

    internal static void SetClickThrough(Window window, bool enabled)
    {
        nint hwnd = new WindowInteropHelper(window).Handle;
        long style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        style = enabled ? style | WsExTransparent : style & ~WsExTransparent;
        ApplyStyle(hwnd, style);
    }

    private static void ApplyStyle(nint hwnd, long style)
    {
        SetWindowLongPtr(hwnd, GwlExStyle, new nint(style));
        SetWindowPos(hwnd, 0, 0, 0, 0, 0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }
}
