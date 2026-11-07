using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SKCTOverlayPaint;

internal enum OverlayTool
{
    Pen,
    Eraser,
    PassThrough
}

internal sealed class OverlayCanvasWindow : NoActivateWindow
{
    private readonly Grid _surface;
    private readonly Canvas _inkLayer;
    private List<InkStroke> _strokes = [];
    private InkStroke? _activeStroke;
    private Point _lastPointer;
    private bool _drawing;

    internal OverlayTool Tool { get; private set; } = OverlayTool.Pen;
    internal Color PenColor { get; set; } = Colors.Red;
    internal double PenThickness { get; set; } = 4;

    internal OverlayCanvasWindow()
    {
        Title = "SKCT Overlay Paint Canvas";
        MinWidth = 280;
        MinHeight = 200;

        // Alpha 1 lets the canvas receive mouse input while remaining visually transparent.
        _surface = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
            ClipToBounds = true,
            Cursor = Cursors.Cross
        };
        _inkLayer = new Canvas { IsHitTestVisible = false };
        _surface.Children.Add(_inkLayer);
        _surface.Children.Add(new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(160, 16, 146, 225)),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        });
        Content = _surface;

        _surface.MouseLeftButtonDown += OnMouseDown;
        _surface.MouseMove += OnMouseMove;
        _surface.MouseLeftButtonUp += OnMouseUp;
        _surface.LostMouseCapture += (_, _) => EndPointer();
    }

    internal void SetTool(OverlayTool tool)
    {
        EndPointer();
        Tool = tool;
        NativeWindowStyles.SetClickThrough(this, tool == OverlayTool.PassThrough);
        _surface.Cursor = tool == OverlayTool.Eraser ? Cursors.Cross : Cursors.Pen;
    }

    internal void ClearInk()
    {
        EndPointer();
        _strokes.Clear();
        _inkLayer.Children.Clear();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Tool == OverlayTool.PassThrough)
            return;

        e.Handled = true;
        _drawing = true;
        _lastPointer = Clamp(e.GetPosition(_surface));
        _surface.CaptureMouse();

        if (Tool == OverlayTool.Pen)
        {
            _activeStroke = new InkStroke(PenColor, PenThickness, [_lastPointer]);
            _strokes.Add(_activeStroke);
            _inkLayer.Children.Add(_activeStroke.Shape);
        }
        else
        {
            EraseAt(_lastPointer);
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_drawing || e.LeftButton != MouseButtonState.Pressed)
            return;

        Point point = Clamp(e.GetPosition(_surface));
        if (Tool == OverlayTool.Pen)
        {
            _activeStroke?.AppendTo(point);
        }
        else if (Tool == OverlayTool.Eraser)
        {
            double radius = EraserRadius;
            double distance = (point - _lastPointer).Length;
            int steps = Math.Max(1, (int)Math.Ceiling(distance / Math.Max(3, radius / 2)));
            for (int i = 1; i <= steps; i++)
                EraseAt(_lastPointer + (point - _lastPointer) * (i / (double)steps));
        }

        _lastPointer = point;
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_drawing)
            return;

        if (Tool == OverlayTool.Pen)
            _activeStroke?.AppendTo(Clamp(e.GetPosition(_surface)));

        EndPointer();
        e.Handled = true;
    }

    private void EndPointer()
    {
        _drawing = false;
        _activeStroke = null;
        if (_surface.IsMouseCaptured)
            _surface.ReleaseMouseCapture();
    }

    private Point Clamp(Point point) => new(
        Math.Clamp(point.X, 0, Math.Max(0, ActualWidth)),
        Math.Clamp(point.Y, 0, Math.Max(0, ActualHeight)));

    private double EraserRadius => Math.Max(12, PenThickness * 3);

    private void EraseAt(Point center)
    {
        double radiusSquared = EraserRadius * EraserRadius;
        List<InkStroke> updated = [];
        bool changed = false;

        foreach (InkStroke stroke in _strokes)
        {
            List<List<Point>> fragments = [];
            List<Point> current = [];
            bool erasedThisStroke = false;

            foreach (Point point in stroke.Points)
            {
                Vector delta = point - center;
                if (delta.LengthSquared <= radiusSquared)
                {
                    erasedThisStroke = true;
                    if (current.Count > 0)
                    {
                        fragments.Add(current);
                        current = [];
                    }
                }
                else
                {
                    current.Add(point);
                }
            }

            if (!erasedThisStroke)
            {
                updated.Add(stroke);
                continue;
            }

            changed = true;
            if (current.Count > 0)
                fragments.Add(current);
            foreach (List<Point> fragment in fragments)
                updated.Add(new InkStroke(stroke.Color, stroke.Thickness, fragment));
        }

        if (!changed)
            return;

        _strokes = updated;
        _inkLayer.Children.Clear();
        foreach (InkStroke stroke in _strokes)
            _inkLayer.Children.Add(stroke.Shape);
    }

    private sealed class InkStroke
    {
        internal Color Color { get; }
        internal double Thickness { get; }
        internal List<Point> Points { get; }
        internal Polyline Shape { get; }

        internal InkStroke(Color color, double thickness, IEnumerable<Point> points)
        {
            Color = color;
            Thickness = thickness;
            Points = [.. points];
            if (Points.Count == 1)
                Points.Add(new Point(Points[0].X + 0.01, Points[0].Y));

            Shape = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false,
                Points = new PointCollection(Points)
            };
        }

        internal void AppendTo(Point end)
        {
            Point start = Points[^1];
            double distance = (end - start).Length;
            if (distance < 0.5)
                return;

            int steps = (int)Math.Ceiling(distance / 2);
            for (int i = 1; i <= steps; i++)
            {
                Point point = start + (end - start) * (i / (double)steps);
                Points.Add(point);
                Shape.Points.Add(point);
            }
        }
    }
}
