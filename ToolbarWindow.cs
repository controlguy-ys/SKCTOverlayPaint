using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SKCTOverlayPaint;

internal sealed class ToolbarWindow : NoActivateWindow
{
    private static readonly Color[] Palette =
    [
        Colors.Red,
        Color.FromRgb(0, 122, 255),
        Color.FromRgb(16, 185, 129),
        Colors.Black
    ];

    private readonly OverlayController _controller;
    private readonly Border _penButton;
    private readonly Border _eraserButton;
    private readonly Border _passButton;
    private readonly TextBlock _sizeLabel;
    private readonly Dictionary<Color, Border> _swatches = [];
    private bool _moving;
    private NativeWindowStyles.ScreenPoint _dragOrigin;
    private double _canvasLeftAtDragStart;
    private double _canvasTopAtDragStart;

    internal ToolbarWindow(OverlayController controller)
    {
        _controller = controller;
        Title = "SKCT Overlay Paint Controls";
        SizeToContent = SizeToContent.WidthAndHeight;

        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var frame = new Border
        {
            Background = Brush("#F21F2937"),
            BorderBrush = Brush("#526273"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(5),
            Child = row
        };
        Content = frame;

        var header = new Border
        {
            Background = Brush("#101827"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 0, 5, 0),
            Cursor = Cursors.SizeAll,
            Child = Label("⋮⋮  이동", 13, FontWeights.SemiBold)
        };
        header.MouseLeftButtonDown += BeginMove;
        header.MouseMove += Move;
        header.MouseLeftButtonUp += EndMove;
        header.LostMouseCapture += (_, _) => _moving = false;
        row.Children.Add(header);

        _penButton = AddButton(row, "펜", () => _controller.SetTool(OverlayTool.Pen));
        _eraserButton = AddButton(row, "지우개", () => _controller.SetTool(OverlayTool.Eraser));
        _passButton = AddButton(row, "통과", () => _controller.SetTool(OverlayTool.PassThrough));
        AddButton(row, "전체 지우기", _controller.ClearInk);

        AddButton(row, "−", () => _controller.AdjustPenSize(-1), 28);
        var sizeFrame = new Border
        {
            Background = Brush("#111827"),
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(2),
            Padding = new Thickness(7, 7, 7, 7)
        };
        _sizeLabel = Label("4px", 12, FontWeights.Normal);
        sizeFrame.Child = _sizeLabel;
        row.Children.Add(sizeFrame);
        AddButton(row, "+", () => _controller.AdjustPenSize(1), 28);

        foreach (Color color in Palette)
        {
            var swatch = new Border
            {
                Width = 22,
                Height = 22,
                Margin = new Thickness(3, 0, 3, 0),
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(color),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(2),
                Cursor = Cursors.Hand
            };
            swatch.MouseLeftButtonDown += (_, e) =>
            {
                _controller.SetColor(color);
                e.Handled = true;
            };
            _swatches[color] = swatch;
            row.Children.Add(swatch);
        }

        AddButton(row, "화면 맞춤", _controller.ResetToWorkingArea);
        AddButton(row, "닫기", _controller.Close);
    }

    internal void RefreshState(OverlayTool tool, double size, Color color)
    {
        _penButton.Background = Brush(tool == OverlayTool.Pen ? "#0078D4" : "#374151");
        _eraserButton.Background = Brush(tool == OverlayTool.Eraser ? "#0078D4" : "#374151");
        _passButton.Background = Brush(tool == OverlayTool.PassThrough ? "#0078D4" : "#374151");
        _sizeLabel.Text = $"{size:0}px";
        foreach ((Color swatchColor, Border border) in _swatches)
            border.BorderBrush = swatchColor == color ? Brushes.White : Brushes.Transparent;
    }

    private void BeginMove(object sender, MouseButtonEventArgs e)
    {
        if (!NativeWindowStyles.GetCursorPos(out _dragOrigin))
            return;

        _canvasLeftAtDragStart = _controller.CanvasLeft;
        _canvasTopAtDragStart = _controller.CanvasTop;
        _moving = true;
        ((UIElement)sender).CaptureMouse();
        e.Handled = true;
    }

    private void Move(object sender, MouseEventArgs e)
    {
        if (!_moving || !NativeWindowStyles.GetCursorPos(out var current))
            return;

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        _controller.MoveCanvas(
            _canvasLeftAtDragStart + (current.X - _dragOrigin.X) / dpi.DpiScaleX,
            _canvasTopAtDragStart + (current.Y - _dragOrigin.Y) / dpi.DpiScaleY);
        e.Handled = true;
    }

    private void EndMove(object sender, MouseButtonEventArgs e)
    {
        _moving = false;
        if (((UIElement)sender).IsMouseCaptured)
            ((UIElement)sender).ReleaseMouseCapture();
        e.Handled = true;
    }

    private static Border AddButton(Panel row, string caption, Action action, double? width = null)
    {
        var button = new Border
        {
            Background = Brush("#374151"),
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(2),
            Padding = new Thickness(9, 7, 9, 7),
            Cursor = Cursors.Hand,
            Child = Label(caption, 12, FontWeights.Medium)
        };
        if (width is not null)
            button.Width = width.Value;
        button.MouseLeftButtonDown += (_, e) =>
        {
            action();
            e.Handled = true;
        };
        row.Children.Add(button);
        return button;
    }

    private static TextBlock Label(string text, double fontSize, FontWeight weight) => new()
    {
        Text = text,
        Foreground = Brushes.White,
        FontFamily = new FontFamily("Malgun Gothic"),
        FontSize = fontSize,
        FontWeight = weight,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        IsHitTestVisible = false
    };

    private static Brush Brush(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
