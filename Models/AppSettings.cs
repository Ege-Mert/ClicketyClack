using System.IO;
using System.Text.Json;

namespace ClicketyClack;

public class AppSettings
{
    public bool ClickEffectEnabled { get; set; } = true;
    public int ClickEffectSize { get; set; } = 125;
    public int ClickEffectThickness { get; set; } = 2;
    public bool PixelatedCircles { get; set; }
    public string LeftClickColor { get; set; } = "#FFFF0000";
    public string RightClickColor { get; set; } = "#FF0000FF";

    public bool HighlightEnabled { get; set; } = true;
    public int HighlightSize { get; set; } = 250;
    public string HighlightColor { get; set; } = "#FFFFFF00";

    public bool LeftClickSound { get; set; } = true;
    public string LeftClickSoundPath { get; set; } = "";
    public bool RightClickSound { get; set; } = true;
    public string RightClickSoundPath { get; set; } = "";

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClicketyClack");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }
}
