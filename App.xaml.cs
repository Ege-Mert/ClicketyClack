using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClicketyClack;

public partial class App : Application
{
    private MouseHook _mouseHook = null!;
    private OverlayWindow _overlay = null!;
    private MainWindow _settingsWindow = null!;
    private AppSettings _settings = null!;
    private string? _leftClickSoundFile;
    private string? _rightClickSoundFile;
    private SoundPlayer? _leftWavePlayer;
    private SoundPlayer? _rightWavePlayer;
    private string? _leftMediaSoundPath;
    private string? _rightMediaSoundPath;
    private int _pendingMouseX;
    private int _pendingMouseY;
    private int _mouseMoveQueued;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = AppSettings.Load();

        _leftClickSoundFile = ExtractEmbeddedSound("ClicketyClack.Assets.Sounds.left_click.wav");
        _rightClickSoundFile = ExtractEmbeddedSound("ClicketyClack.Assets.Sounds.right_click.wav");
        UpdateSoundPlayback();

        _overlay = new OverlayWindow(_settings);
        _overlay.Show();

        _settingsWindow = new MainWindow();
        _settingsWindow.LoadFromSettings(_settings);
        _settingsWindow.Closed += (_, _) =>
        {
            _mouseHook?.Dispose();
            _overlay?.Close();
            Environment.Exit(0);
        };
        MainWindow = _settingsWindow;

        _mouseHook = new MouseHook();
        _mouseHook.MouseMove += OnMouseMove;
        _mouseHook.LeftButtonDown += OnLeftDown;
        _mouseHook.RightButtonDown += OnRightDown;
        _mouseHook.Start();

        _settingsWindow.Show();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        UpdateSoundPlayback();
        _overlay.UpdateSettings(settings);
    }

    private void OnMouseMove(int x, int y)
    {
        Volatile.Write(ref _pendingMouseX, x);
        Volatile.Write(ref _pendingMouseY, y);

        if (Interlocked.CompareExchange(ref _mouseMoveQueued, 1, 0) != 0)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Render, ProcessPendingMouseMove);
    }

    private void OnLeftDown(int x, int y)
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            () => _overlay.ShowClickEffect(x, y, isLeft: true));

        if (_settings.LeftClickSound)
            PlaySound(_leftWavePlayer, _leftMediaSoundPath);
    }

    private void OnRightDown(int x, int y)
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            () => _overlay.ShowClickEffect(x, y, isLeft: false));

        if (_settings.RightClickSound)
            PlaySound(_rightWavePlayer, _rightMediaSoundPath);
    }

    private void ProcessPendingMouseMove()
    {
        while (true)
        {
            var x = Volatile.Read(ref _pendingMouseX);
            var y = Volatile.Read(ref _pendingMouseY);

            _overlay.OnMouseMove(x, y);
            Interlocked.Exchange(ref _mouseMoveQueued, 0);

            if (x == Volatile.Read(ref _pendingMouseX) &&
                y == Volatile.Read(ref _pendingMouseY))
                return;

            if (Interlocked.CompareExchange(ref _mouseMoveQueued, 1, 0) != 0)
                return;
        }
    }

    private void UpdateSoundPlayback()
    {
        var leftSoundPath = ResolveSoundPath(_settings.LeftClickSoundPath, _leftClickSoundFile);
        var rightSoundPath = ResolveSoundPath(_settings.RightClickSoundPath, _rightClickSoundFile);

        _leftWavePlayer = CreateWavePlayer(leftSoundPath, out _leftMediaSoundPath);
        _rightWavePlayer = CreateWavePlayer(rightSoundPath, out _rightMediaSoundPath);
    }

    private static SoundPlayer? CreateWavePlayer(string? path, out string? mediaPath)
    {
        mediaPath = null;
        if (path == null)
            return null;

        if (!string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase))
        {
            mediaPath = path;
            return null;
        }

        try
        {
            var player = new SoundPlayer(path);
            player.Load();
            return player;
        }
        catch
        {
            mediaPath = path;
            return null;
        }
    }

    private void PlaySound(SoundPlayer? wavePlayer, string? mediaPath)
    {
        if (wavePlayer != null)
        {
            try
            {
                wavePlayer.Play();
            }
            catch { }
            return;
        }

        if (mediaPath == null)
            return;

        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () => PlayMediaSound(mediaPath));
    }

    private static void PlayMediaSound(string path)
    {
        try
        {
            var player = new MediaPlayer();
            player.Open(new Uri(path));
            player.Play();
            player.MediaEnded += (_, _) => player.Close();
            player.MediaFailed += (_, _) => player.Close();
        }
        catch { }
    }

    private static string? ResolveSoundPath(string customPath, string? defaultSoundFile)
    {
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            return customPath;

        return defaultSoundFile;
    }

    private static string? ExtractEmbeddedSound(string resourceName)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            var tempDir = Path.Combine(Path.GetTempPath(), "ClicketyClack");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, resourceName);

            using (var fs = File.Create(tempFile))
                stream.CopyTo(fs);

            return tempFile;
        }
        catch { return null; }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mouseHook?.Dispose();
        _overlay?.Close();
        base.OnExit(e);
        Environment.Exit(0);
    }
}
