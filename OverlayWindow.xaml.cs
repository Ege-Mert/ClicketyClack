using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WPF;

public partial class OverlayWindow : Window
{
    private const int MaxActiveClickEffects = 24;
    private Ellipse? _highlightEllipse;
    private AppSettings _settings;
    private double _fromDeviceM11 = 1.0;
    private double _fromDeviceM22 = 1.0;
    private System.Windows.Media.Brush _leftClickBrush = Brushes.Red;
    private System.Windows.Media.Brush _rightClickBrush = Brushes.Blue;
    private readonly List<Ellipse> _activeClickEffects = [];

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    public OverlayWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        PositionOverlay();

        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Make window click-through
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW);

        // Get DPI transform for coordinate conversion (physical pixels -> DIPs)
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _fromDeviceM11 = source.CompositionTarget.TransformFromDevice.M11;
            _fromDeviceM22 = source.CompositionTarget.TransformFromDevice.M22;
        }

        RebuildHighlight();
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        RebuildHighlight();
    }

    private void RebuildHighlight()
    {
        UpdateClickEffectBrushes();

        if (_highlightEllipse != null)
        {
            EffectCanvas.Children.Remove(_highlightEllipse);
            _highlightEllipse = null;
        }

        if (!_settings.HighlightEnabled) return;

        double size = 50.0 * _settings.HighlightSize / 100.0;
        var color = (Color)ColorConverter.ConvertFromString(_settings.HighlightColor);

        _highlightEllipse = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromArgb(90, color.R, color.G, color.B)),
            IsHitTestVisible = false
        };
        EffectCanvas.Children.Insert(0, _highlightEllipse);
    }

    public void OnMouseMove(int physX, int physY)
    {
        if (_highlightEllipse == null) return;

        double x = physX * _fromDeviceM11 - Left;
        double y = physY * _fromDeviceM22 - Top;

        Canvas.SetLeft(_highlightEllipse, x - _highlightEllipse.Width / 2);
        Canvas.SetTop(_highlightEllipse, y - _highlightEllipse.Height / 2);
    }

    public void ShowClickEffect(int physX, int physY, bool isLeft)
    {
        if (!_settings.ClickEffectEnabled) return;

        double x = physX * _fromDeviceM11 - Left;
        double y = physY * _fromDeviceM22 - Top;

        double baseSize = 34.0 * _settings.ClickEffectSize / 100.0;
        var stroke = isLeft ? _leftClickBrush : _rightClickBrush;

        var ellipse = new Ellipse
        {
            Width = baseSize,
            Height = baseSize,
            Fill = Brushes.Transparent,
            Stroke = stroke,
            StrokeThickness = _settings.ClickEffectThickness,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
            IsHitTestVisible = false
        };
        if (_settings.PixelatedCircles)
            RenderOptions.SetEdgeMode(ellipse, EdgeMode.Aliased);

        Canvas.SetLeft(ellipse, x - baseSize / 2);
        Canvas.SetTop(ellipse, y - baseSize / 2);
        TrimOldClickEffects();
        _activeClickEffects.Add(ellipse);
        EffectCanvas.Children.Add(ellipse);

        var duration = TimeSpan.FromMilliseconds(400);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        var scaleX = new DoubleAnimation(1, 3.0, duration) { EasingFunction = ease };
        var scaleY = new DoubleAnimation(1, 3.0, duration) { EasingFunction = ease };
        var fade = new DoubleAnimation(1, 0, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        fade.Completed += (_, _) => RemoveClickEffect(ellipse);

        var transform = (ScaleTransform)ellipse.RenderTransform;
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        ellipse.BeginAnimation(OpacityProperty, fade);
    }

    public void RepositionToVirtualScreen()
    {
        PositionOverlay();
    }

    private void PositionOverlay()
    {
        // Inset by 1px on each edge so the auto-hide taskbar trigger zone is not blocked
        Left = SystemParameters.VirtualScreenLeft + 1;
        Top = SystemParameters.VirtualScreenTop + 1;
        Width = SystemParameters.VirtualScreenWidth - 2;
        Height = SystemParameters.VirtualScreenHeight - 2;
    }

    private void UpdateClickEffectBrushes()
    {
        _leftClickBrush = CreateClickBrush(_settings.LeftClickColor, Colors.Red);
        _rightClickBrush = CreateClickBrush(_settings.RightClickColor, Colors.Blue);
    }

    private static System.Windows.Media.Brush CreateClickBrush(string colorText, Color fallback)
    {
        var brush = new SolidColorBrush(ParseColor(colorText, fallback));
        brush.Freeze();
        return brush;
    }

    private static Color ParseColor(string colorText, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(colorText);
        }
        catch
        {
            return fallback;
        }
    }

    private void TrimOldClickEffects()
    {
        while (_activeClickEffects.Count >= MaxActiveClickEffects)
            RemoveClickEffect(_activeClickEffects[0]);
    }

    private void RemoveClickEffect(Ellipse ellipse)
    {
        ellipse.BeginAnimation(OpacityProperty, null);

        if (ellipse.RenderTransform is ScaleTransform transform)
        {
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        EffectCanvas.Children.Remove(ellipse);
        _activeClickEffects.Remove(ellipse);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}
