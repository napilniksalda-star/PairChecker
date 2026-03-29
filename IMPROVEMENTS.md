# Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ СЂРµРєРѕРјРµРЅРґР°С†РёРё РїРѕ СѓР»СѓС‡С€РµРЅРёСЋ PairChecker

## 1. Р‘РµР·РѕРїР°СЃРЅРѕСЃС‚СЊ

### 1.1 Р’Р°Р»РёРґР°С†РёСЏ РїСѓС‚РµР№
```csharp
public static class PathValidator
{
    public static bool IsValidPath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            return !string.IsNullOrWhiteSpace(fullPath);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsPathAccessible(string path)
    {
        try
        {
            var test = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
```

### 1.2 РћРіСЂР°РЅРёС‡РµРЅРёРµ СЂР°Р·РјРµСЂР° С„Р°Р№Р»РѕРІ
```csharp
public class CheckOptions
{
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100 MB
    public int MaxFilesCount { get; set; } = 10000;
}
```

## 2. РџСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚СЊ

### 2.1 РљСЌС€РёСЂРѕРІР°РЅРёРµ СЂРµР·СѓР»СЊС‚Р°С‚РѕРІ
```csharp
public class CachedFileChecker : FileChecker
{
    private readonly Dictionary<string, CheckResult> cache = new();

    public async Task<CheckResult> CheckFilesWithCacheAsync(
        string folder, string firstExt, string secondExt, bool recursive)
    {
        var key = $"{folder}|{firstExt}|{secondExt}|{recursive}";
        
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = await CheckFilesAsync(folder, firstExt, secondExt, recursive);
        cache[key] = result;
        return result;
    }
}
```

### 2.2 РђСЃРёРЅС…СЂРѕРЅРЅР°СЏ РѕР±СЂР°Р±РѕС‚РєР° Р±РѕР»СЊС€РёС… РїР°РїРѕРє
```csharp
private async Task<List<string>> CollectFilesAsync(
    string folder, string extension, SearchOption searchOption)
{
    return await Task.Run(() =>
    {
        return Directory.EnumerateFiles(folder, $"*{extension}", searchOption)
            .AsParallel()
            .ToList();
    });
}
```

## 3. Р Р°СЃС€РёСЂСЏРµРјРѕСЃС‚СЊ

### 3.1 РРЅС‚РµСЂС„РµР№СЃ РґР»СЏ СЌРєСЃРїРѕСЂС‚Р° РѕС‚С‡РµС‚РѕРІ
```csharp
public interface IReportExporter
{
    string FileExtension { get; }
    string Description { get; }
    Task ExportAsync(CheckResult result, string filePath);
}

public class TextReportExporter : IReportExporter
{
    public string FileExtension => ".txt";
    public string Description => "РўРµРєСЃС‚РѕРІС‹Р№ С„Р°Р№Р»";

    public async Task ExportAsync(CheckResult result, string filePath)
    {
        var report = FileChecker.GenerateReport(result, ".pdf", ".dxf");
        await File.WriteAllTextAsync(filePath, report);
    }
}

public class CsvReportExporter : IReportExporter
{
    public string FileExtension => ".csv";
    public string Description => "CSV С„Р°Р№Р»";

    public async Task ExportAsync(CheckResult result, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("РўРёРї,РџР°РїРєР°,Р¤Р°Р№Р»");
        
        foreach (var kvp in result.FirstWithoutPair)
        {
            foreach (var file in kvp.Value)
            {
                csv.AppendLine($"Р‘РµР· РїР°СЂС‹,{kvp.Key},{Path.GetFileName(file)}");
            }
        }
        
        await File.WriteAllTextAsync(filePath, csv.ToString());
    }
}
```

### 3.2 РџР»Р°РіРёРЅРЅР°СЏ СЃРёСЃС‚РµРјР° РґР»СЏ С„РѕСЂРјР°С‚РѕРІ
```csharp
public interface IFormatProvider
{
    string[] GetSupportedFormats();
    string GetFormatDescription(string format);
}

public class CadFormatProvider : IFormatProvider
{
    public string[] GetSupportedFormats() => new[] 
    { 
        ".dwg", ".dxf", ".step", ".stp", ".iges", 
        ".sldprt", ".sldasm", ".slddrw" 
    };

    public string GetFormatDescription(string format) => format switch
    {
        ".dwg" => "AutoCAD Drawing",
        ".dxf" => "Drawing Exchange Format",
        ".step" => "STEP 3D Model",
        _ => format
    };
}
```

## 4. РЈР»СѓС‡С€РµРЅРёРµ UI

### 4.1 Drag & Drop РґР»СЏ РїР°РїРѕРє
```csharp
private void InitializeDragDrop()
{
    txtFolder.AllowDrop = true;
    txtFolder.DragEnter += (s, e) =>
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    };
    
    txtFolder.DragDrop += (s, e) =>
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            var path = files[0];
            if (Directory.Exists(path))
            {
                txtFolder.Text = path;
            }
        }
    };
}
```

### 4.2 РўРµРјРЅР°СЏ С‚РµРјР°
```csharp
public class ThemeManager
{
    public static void ApplyDarkTheme(Form form)
    {
        form.BackColor = Color.FromArgb(30, 30, 30);
        form.ForeColor = Color.White;

        foreach (Control control in form.Controls)
        {
            ApplyDarkThemeToControl(control);
        }
    }

    private static void ApplyDarkThemeToControl(Control control)
    {
        if (control is TextBox textBox)
        {
            textBox.BackColor = Color.FromArgb(45, 45, 45);
            textBox.ForeColor = Color.White;
        }
        else if (control is Button button)
        {
            button.BackColor = Color.FromArgb(60, 60, 60);
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
        }
        // ... РґСЂСѓРіРёРµ РєРѕРЅС‚СЂРѕР»С‹
    }
}
```

### 4.3 РСЃС‚РѕСЂРёСЏ РїСЂРѕРІРµСЂРѕРє
```csharp
public class CheckHistory
{
    public DateTime Timestamp { get; set; }
    public string Folder { get; set; } = string.Empty;
    public string FirstFormat { get; set; } = string.Empty;
    public string SecondFormat { get; set; } = string.Empty;
    public int UnpairedCount { get; set; }
}

public class HistoryManager
{
    private const int MaxHistoryItems = 10;
    private readonly List<CheckHistory> history = new();

    public void AddHistory(CheckHistory item)
    {
        history.Insert(0, item);
        if (history.Count > MaxHistoryItems)
        {
            history.RemoveAt(history.Count - 1);
        }
    }

    public List<CheckHistory> GetHistory() => history.ToList();
}
```

## 5. Р›РѕРіРёСЂРѕРІР°РЅРёРµ Рё РґРёР°РіРЅРѕСЃС‚РёРєР°

### 5.1 РЎС‚СЂСѓРєС‚СѓСЂРёСЂРѕРІР°РЅРЅРѕРµ Р»РѕРіРёСЂРѕРІР°РЅРёРµ
```csharp
public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
}

public class FileLogger : ILogger
{
    private readonly string logPath;

    public FileLogger(string logPath)
    {
        this.logPath = logPath;
    }

    public void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message}: {ex}" : message;
        WriteLog("ERROR", fullMessage);
    }

    private void WriteLog(string level, string message)
    {
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        File.AppendAllText(logPath, logMessage + Environment.NewLine);
    }
}
```

### 5.2 РњРµС‚СЂРёРєРё РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚Рё
```csharp
public class PerformanceMetrics
{
    public TimeSpan ScanDuration { get; set; }
    public int FilesScanned { get; set; }
    public long BytesProcessed { get; set; }
    public double FilesPerSecond => FilesScanned / ScanDuration.TotalSeconds;
}
```

## 6. РљРѕРЅС„РёРіСѓСЂР°С†РёСЏ

### 6.1 Р Р°СЃС€РёСЂРµРЅРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё
```csharp
public class AdvancedSettings
{
    // РџСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚СЊ
    public int MaxParallelTasks { get; set; } = 4;
    public int BufferSize { get; set; } = 8192;
    
    // Р¤РёР»СЊС‚СЂС‹
    public List<string> ExcludeFolders { get; set; } = new() { "bin", "obj", ".git" };
    public List<string> ExcludePatterns { get; set; } = new() { "*.tmp", "~*" };
    
    // UI
    public bool UseDarkTheme { get; set; } = false;
    public string Language { get; set; } = "ru-RU";
    
    // РћС‚С‡РµС‚С‹
    public string DefaultReportFormat { get; set; } = "txt";
    public bool AutoSaveReport { get; set; } = false;
}
```

## 7. РРЅС‚РµСЂРЅР°С†РёРѕРЅР°Р»РёР·Р°С†РёСЏ

### 7.1 РџРѕРґРґРµСЂР¶РєР° РЅРµСЃРєРѕР»СЊРєРёС… СЏР·С‹РєРѕРІ
```csharp
public class Localization
{
    private readonly Dictionary<string, Dictionary<string, string>> translations = new()
    {
        ["ru-RU"] = new()
        {
            ["AppTitle"] = "PairChecker - РџСЂРѕРІРµСЂРєР° РїР°СЂРЅС‹С… С„Р°Р№Р»РѕРІ",
            ["SelectFolder"] = "Р’С‹Р±РµСЂРёС‚Рµ РїР°РїРєСѓ",
            ["StartCheck"] = "Р—Р°РїСѓСЃС‚РёС‚СЊ РїСЂРѕРІРµСЂРєСѓ"
        },
        ["en-US"] = new()
        {
            ["AppTitle"] = "PairChecker - Paired Files Checker",
            ["SelectFolder"] = "Select folder",
            ["StartCheck"] = "Start check"
        }
    };

    public string GetString(string key, string culture = "ru-RU")
    {
        if (translations.TryGetValue(culture, out var dict) &&
            dict.TryGetValue(key, out var value))
        {
            return value;
        }
        return key;
    }
}
```

## 8. РђРІС‚РѕРјР°С‚РёР·Р°С†РёСЏ

### 8.1 РљРѕРјР°РЅРґРЅР°СЏ СЃС‚СЂРѕРєР°
```csharp
public class CommandLineOptions
{
    public string? Folder { get; set; }
    public string? FirstFormat { get; set; }
    public string? SecondFormat { get; set; }
    public bool Recursive { get; set; }
    public string? OutputFile { get; set; }
    public bool Silent { get; set; }
}

// РСЃРїРѕР»СЊР·РѕРІР°РЅРёРµ:
// PairChecker.exe --folder ".\\Docs" --first .pdf --second .dxf --recursive --output report.txt
```

### 8.2 РџР°РєРµС‚РЅР°СЏ РѕР±СЂР°Р±РѕС‚РєР°
```csharp
public class BatchProcessor
{
    public async Task ProcessBatchAsync(List<BatchJob> jobs)
    {
        foreach (var job in jobs)
        {
            var checker = new FileChecker();
            var result = await checker.CheckFilesAsync(
                job.Folder, job.FirstFormat, job.SecondFormat, job.Recursive);
            
            var report = FileChecker.GenerateReport(result, job.FirstFormat, job.SecondFormat);
            await File.WriteAllTextAsync(job.OutputFile, report);
        }
    }
}
```

## Р—Р°РєР»СЋС‡РµРЅРёРµ

Р­С‚Рё СѓР»СѓС‡С€РµРЅРёСЏ РїРѕРјРѕРіСѓС‚ СЃРґРµР»Р°С‚СЊ РїСЂРёР»РѕР¶РµРЅРёРµ:
- вњ… Р‘РѕР»РµРµ Р±РµР·РѕРїР°СЃРЅС‹Рј
- вњ… Р‘РѕР»РµРµ РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅС‹Рј
- вњ… Р‘РѕР»РµРµ СЂР°СЃС€РёСЂСЏРµРјС‹Рј
- вњ… Р‘РѕР»РµРµ СѓРґРѕР±РЅС‹Рј РґР»СЏ РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№
- вњ… Р‘РѕР»РµРµ РїСЂРѕС„РµСЃСЃРёРѕРЅР°Р»СЊРЅС‹Рј

Р РµРєРѕРјРµРЅРґСѓРµС‚СЃСЏ РІРЅРµРґСЂСЏС‚СЊ СѓР»СѓС‡С€РµРЅРёСЏ РїРѕСЃС‚РµРїРµРЅРЅРѕ, РЅР°С‡РёРЅР°СЏ СЃ РЅР°РёР±РѕР»РµРµ РєСЂРёС‚РёС‡РЅС‹С… РґР»СЏ РІР°С€РµРіРѕ СЃР»СѓС‡Р°СЏ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ.

