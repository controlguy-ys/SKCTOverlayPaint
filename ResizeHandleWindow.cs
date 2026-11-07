using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SKCTOverlayPaint;

internal sealed class ResizeHandleWindow : NoActivateWindow
{
    private readonly OverlayController _controller;
    private bool _resizing;
    private NativeWindowStyles.ScreenPoint _dragOrigin;
    private double _widthAtDragStart;
    private double _heightAtDragStart;

    internal ResizeHandleWindow(OverlayController controller)
    {
        _controller = controller;
        Title = "SKCT Overlay Paint Resize Handle";
        Width = 30;
        Height = 30;
        Cursor = Cursors.SizeNWSE;

        var lines = new Canvas { Width = 30, Height = 30, IsHitTestVisible = false };
        AddLine(lines, 16, 24, 24, 16);
        AddLine(lines, 9, 24, 24, 9);
        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(235, 31, 41, 55)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(16, 146, 225)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = lines
        };

        MouseLeftButtonDown += BeginResize;
        MouseMove += Resize;
        MouseLeftButtonUp += EndResize;
        LostMouseCapture += (_, _) => _resizing = false;
    }

    private void BeginResize(object sender, MouseButtonEventArgs e)
    {
        if (!NativeWindowStyles.GetCursorPos(out _dragOrigin))
            return;

        _widthAtDragStart = _controller.CanvasWidth;
        _heightAtDragStart = _controller.CanvasHeight;
        _resizing = true;
        CaptureMouse();
        e.Handled = true;
    }

    private void Resize(object sender, MouseEventArgs e)
    {
        if (!_resizing || !NativeWindowStyles.GetCursorPos(out var current))
            return;

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        _controller.ResizeCanvas(
            _widthAtDragStart + (current.X - _dragOrigin.X) / dpi.DpiScaleX,
            _heightAtDragStart + (current.Y - _dragOrigin.Y) / dpi.DpiScaleY);
        e.Handled = true;
    }

    private void EndResize(object sender, MouseButtonEventArgs e)
    {
        _resizing = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        e.Handled = true;
    }

    private static void AddLine(Canvas canvas, double x1, double y1, double x2, double y2)
    {
        canvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        });
    }
}
