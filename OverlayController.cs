using System.Windows;
using System.Windows.Media;

namespace SKCTOverlayPaint;

internal sealed class OverlayController
{
    private readonly OverlayCanvasWindow _canvas;
    private readonly ToolbarWindow _toolbar;
    private readonly ResizeHandleWindow _resizeHandle;
    private OverlayTool _tool = OverlayTool.Pen;
    private Color _color = Colors.Red;
    private double _penSize = 4;

    internal double CanvasLeft => _canvas.Left;
    internal double CanvasTop => _canvas.Top;
    internal double CanvasWidth => _canvas.Width;
    internal double CanvasHeight => _canvas.Height;

    internal OverlayController()
    {
        _canvas = new OverlayCanvasWindow();
        _toolbar = new ToolbarWindow(this);
        _resizeHandle = new ResizeHandleWindow(this);

        _canvas.LocationChanged += (_, _) => PositionControls();
        _canvas.SizeChanged += (_, _) => PositionControls();

        ResetToWorkingArea();
        _canvas.Show();
        _resizeHandle.Show();
        _toolbar.Show();
        PositionControls();
        SetTool(OverlayTool.Pen);
    }

    internal void SetTool(OverlayTool tool)
    {
        _tool = tool;
        _canvas.SetTool(tool);
        _toolbar.RefreshState(_tool, _penSize, _color);
    }

    internal void SetColor(Color color)
    {
        _color = color;
        _canvas.PenColor = color;
        SetTool(OverlayTool.Pen);
    }

    internal void AdjustPenSize(int amount)
    {
        _penSize = Math.Clamp(_penSize + amount, 1, 24);
        _canvas.PenThickness = _penSize;
        _toolbar.RefreshState(_tool, _penSize, _color);
    }

    internal void ClearInk() => _canvas.ClearInk();

    internal void MoveCanvas(double left, double top)
    {
        double virtualLeft = SystemParameters.VirtualScreenLeft;
        double virtualTop = SystemParameters.VirtualScreenTop;
        double virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        double virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;
        double visibleToolbarWidth = Math.Max(150, _toolbar.ActualWidth + 12);

        _canvas.Left = Math.Clamp(left, virtualLeft, Math.Max(virtualLeft, virtualRight - visibleToolbarWidth));
        _canvas.Top = Math.Clamp(top, virtualTop, Math.Max(virtualTop, virtualBottom - 54));
        PositionControls();
    }

    internal void ResizeCanvas(double width, double height)
    {
        double virtualRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        double virtualBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        double maxWidth = Math.Max(280, virtualRight - _canvas.Left);
        double maxHeight = Math.Max(200, virtualBottom - _canvas.Top);

        _canvas.Width = Math.Clamp(width, 280, maxWidth);
        _canvas.Height = Math.Clamp(height, 200, maxHeight);
        PositionControls();
    }

    internal void ResetToWorkingArea()
    {
        Rect work = SystemParameters.WorkArea;
        double margin = 24;
        _canvas.Left = work.Left + margin;
        _canvas.Top = work.Top + margin;
        _canvas.Width = Math.Max(280, work.Width - 2 * margin);
        _canvas.Height = Math.Max(200, work.Height - 2 * margin);
        PositionControls();
    }

    internal void Close()
    {
        _toolbar.Close();
        _resizeHandle.Close();
        _canvas.Close();
        Application.Current.Shutdown();
    }

    private void PositionControls()
    {
        _toolbar.Left = _canvas.Left + 10;
        _toolbar.Top = _canvas.Top + 10;
        _resizeHandle.Left = _canvas.Left + _canvas.Width - _resizeHandle.Width;
        _resizeHandle.Top = _canvas.Top + _canvas.Height - _resizeHandle.Height;
    }
}
