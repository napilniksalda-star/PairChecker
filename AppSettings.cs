using System.Text.Json;

namespace PairChecker;

/// <summary>
/// Настройки приложения
/// </summary>
public class AppSettings
{
    public string LastFirstFormat { get; set; } = ".pdf";
    public string LastSecondFormat { get; set; } = ".dxf";
    public bool LastRecursive { get; set; } = false;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PairChecker",
        "settings.json"
    );

    /// <summary>
    /// Загрузить настройки из файла
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Игнорируем ошибки загрузки настроек
        }

        return new AppSettings();
    }

    /// <summary>
    /// Сохранить настройки в файл
    /// </summary>
    public void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Игнорируем ошибки сохранения настроек
        }
    }
}
