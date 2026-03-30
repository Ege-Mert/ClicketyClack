using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPF;

public partial class MainWindow : Window
{
    private readonly int[] _sizes = [50, 75, 100, 125, 150, 200, 250, 300];
    private readonly int[] _thicknesses = [1, 2, 3, 4, 5, 6, 8, 10];

    public MainWindow()
    {
        InitializeComponent();

        foreach (var s in _sizes)
        {
            cmbClickSize.Items.Add(s);
            cmbHighlightSize.Items.Add(s);
        }
        foreach (var t in _thicknesses)
            cmbThickness.Items.Add(t);
    }

    public void LoadFromSettings(AppSettings settings)
    {
        chkClickEffect.IsChecked = settings.ClickEffectEnabled;
        cmbClickSize.SelectedItem = _sizes.Contains(settings.ClickEffectSize) ? settings.ClickEffectSize : 100;
        cmbThickness.SelectedItem = _thicknesses.Contains(settings.ClickEffectThickness) ? settings.ClickEffectThickness : 3;
        chkPixelated.IsChecked = settings.PixelatedCircles;
        SetBorderColor(brdLeftColor, settings.LeftClickColor);
        SetBorderColor(brdRightColor, settings.RightClickColor);

        chkHighlight.IsChecked = settings.HighlightEnabled;
        cmbHighlightSize.SelectedItem = _sizes.Contains(settings.HighlightSize) ? settings.HighlightSize : 100;
        SetBorderColor(brdHighlightColor, settings.HighlightColor);

        chkLeftSound.IsChecked = settings.LeftClickSound;
        txtLeftSoundPath.Text = settings.LeftClickSoundPath;
        chkRightSound.IsChecked = settings.RightClickSound;
        txtRightSoundPath.Text = settings.RightClickSoundPath;

        UpdatePreview();
    }

    public AppSettings ToSettings()
    {
        return new AppSettings
        {
            ClickEffectEnabled = chkClickEffect.IsChecked == true,
            ClickEffectSize = cmbClickSize.SelectedItem as int? ?? 100,
            ClickEffectThickness = cmbThickness.SelectedItem as int? ?? 3,
            PixelatedCircles = chkPixelated.IsChecked == true,
            LeftClickColor = GetBorderColor(brdLeftColor),
            RightClickColor = GetBorderColor(brdRightColor),
            HighlightEnabled = chkHighlight.IsChecked == true,
            HighlightSize = cmbHighlightSize.SelectedItem as int? ?? 100,
            HighlightColor = GetBorderColor(brdHighlightColor),
            LeftClickSound = chkLeftSound.IsChecked == true,
            LeftClickSoundPath = txtLeftSoundPath.Text,
            RightClickSound = chkRightSound.IsChecked == true,
            RightClickSoundPath = txtRightSoundPath.Text
        };
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) ApplyLive();
    }

    private void OnComboChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) ApplyLive();
    }

    private void ApplyLive()
    {
        UpdatePreview();
        var settings = ToSettings();
        settings.Save();
        ((App)Application.Current).ApplySettings(settings);
    }

    private void UpdatePreview()
    {
        previewCanvas.Children.Clear();
        double cx = 75, cy = 75;

        // Highlight circle
        if (chkHighlight.IsChecked == true)
        {
            int size = cmbHighlightSize.SelectedItem as int? ?? 100;
            double d = 50.0 * size / 100.0;
            var color = GetColorFromBorder(brdHighlightColor);

            var hl = new Ellipse
            {
                Width = d,
                Height = d,
                Fill = new SolidColorBrush(Color.FromArgb(90, color.R, color.G, color.B))
            };
            Canvas.SetLeft(hl, cx - d / 2);
            Canvas.SetTop(hl, cy - d / 2);
            previewCanvas.Children.Add(hl);
        }

        // Click effect sample (left click)
        if (chkClickEffect.IsChecked == true)
        {
            int size = cmbClickSize.SelectedItem as int? ?? 100;
            double d = 34.0 * size / 100.0;
            var color = GetColorFromBorder(brdLeftColor);

            int thickness = cmbThickness.SelectedItem as int? ?? 3;
            var ce = new Ellipse
            {
                Width = d,
                Height = d,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness
            };
            if (chkPixelated.IsChecked == true)
                RenderOptions.SetEdgeMode(ce, EdgeMode.Aliased);
            Canvas.SetLeft(ce, cx - d / 2);
            Canvas.SetTop(ce, cy - d / 2);
            previewCanvas.Children.Add(ce);
        }

        // Cursor arrow
        var cursor = new Path
        {
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Data = Geometry.Parse("M 0,0 L 0,17 L 4,13 L 7,19 L 9,18 L 6,12 L 11,12 Z")
        };
        Canvas.SetLeft(cursor, cx);
        Canvas.SetTop(cursor, cy);
        previewCanvas.Children.Add(cursor);
    }

    private void PickColor(System.Windows.Controls.Border border)
    {
        var current = GetColorFromBorder(border);
        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = dialog.Color;
            border.Background = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
            ApplyLive();
        }
    }

    private void LeftColor_Click(object sender, MouseButtonEventArgs e) => PickColor(brdLeftColor);
    private void RightColor_Click(object sender, MouseButtonEventArgs e) => PickColor(brdRightColor);
    private void HighlightColor_Click(object sender, MouseButtonEventArgs e) => PickColor(brdHighlightColor);

    private void BrowseLeftSound_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseSoundFile();
        if (path != null)
        {
            txtLeftSoundPath.Text = path;
            ApplyLive();
        }
    }

    private void BrowseRightSound_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseSoundFile();
        if (path != null)
        {
            txtRightSoundPath.Text = path;
            ApplyLive();
        }
    }

    private void DefaultLeftSound_Click(object sender, RoutedEventArgs e)
    {
        txtLeftSoundPath.Text = "";
        ApplyLive();
    }

    private void DefaultRightSound_Click(object sender, RoutedEventArgs e)
    {
        txtRightSoundPath.Text = "";
        ApplyLive();
    }

    private static string? BrowseSoundFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*",
            Title = "Select click sound"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }


    private static void SetBorderColor(System.Windows.Controls.Border border, string hex)
    {
        try { border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { border.Background = Brushes.Gray; }
    }

    private static string GetBorderColor(System.Windows.Controls.Border border)
    {
        return ((SolidColorBrush)border.Background).Color.ToString();
    }

    private static Color GetColorFromBorder(System.Windows.Controls.Border border)
    {
        return ((SolidColorBrush)border.Background).Color;
    }
}
