using System.Text;

namespace PairChecker;

/// <summary>
/// Класс для проверки парных файлов
/// </summary>
public class FileChecker
{
    public event EventHandler<ProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Результат проверки файлов
    /// </summary>
    public class CheckResult
    {
        public Dictionary<string, List<string>> FirstWithoutPair { get; set; } = new();
        public Dictionary<string, List<string>> SecondWithoutPair { get; set; } = new();
        public int TotalFirstFiles { get; set; }
        public int TotalSecondFiles { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Выполнить проверку парных файлов
    /// </summary>
    public async Task<CheckResult> CheckFilesAsync(
        string folder, 
        string firstExt, 
        string secondExt, 
        bool recursive, 
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Папка не указана", nameof(folder));
        
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"Папка не найдена: {folder}");

        if (string.IsNullOrWhiteSpace(firstExt) || string.IsNullOrWhiteSpace(secondExt))
            throw new ArgumentException("Форматы файлов не указаны");

        return await Task.Run(() => PerformCheck(folder, firstExt, secondExt, recursive, token), token);
    }

    private CheckResult PerformCheck(string folder, string firstExt, string secondExt, bool recursive, CancellationToken token)
    {
        var result = new CheckResult();
        var filesFirst = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var filesSecond = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        OnProgressChanged(0, "Начало сканирования...");

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        // Поиск файлов первого формата
        OnProgressChanged(20, $"Поиск файлов {firstExt}...");
        CollectFiles(folder, firstExt, searchOption, filesFirst, result.Errors, token, firstExt, secondExt);

        // Поиск файлов второго формата
        OnProgressChanged(50, $"Поиск файлов {secondExt}...");
        CollectFiles(folder, secondExt, searchOption, filesSecond, result.Errors, token, firstExt, secondExt);

        result.TotalFirstFiles = filesFirst.Values.Sum(v => v.Count);
        result.TotalSecondFiles = filesSecond.Values.Sum(v => v.Count);

        // Анализ отсутствующих пар
        OnProgressChanged(70, "Анализ результатов...");
        FindUnpairedFiles(filesFirst, filesSecond, result.FirstWithoutPair);
        FindUnpairedFiles(filesSecond, filesFirst, result.SecondWithoutPair);

        OnProgressChanged(100, "Проверка завершена");

        return result;
    }

    private void CollectFiles(
        string folder, 
        string extension,
        SearchOption searchOption,
        Dictionary<string, List<string>> fileDict,
        List<string> errors,
        CancellationToken token,
        string firstExt,
        string secondExt)
    {
        try
        {
            var files = Directory.EnumerateFiles(folder, $"*{extension}", searchOption);
            
            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string fileName = Path.GetFileName(file);
                    if (IsServiceTemporaryFile(fileName))
                        continue;

                    string key = NormalizeFileKey(fileName, firstExt, secondExt);

                    if (!fileDict.ContainsKey(key))
                        fileDict[key] = new List<string>();
                    fileDict[key].Add(file);
                }
                catch (Exception ex)
                {
                    errors.Add($"Ошибка обработки файла {file}: {ex.Message}");
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            errors.Add($"Нет доступа к папке {folder}: {ex.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"Ошибка при поиске файлов {extension}: {ex.Message}");
        }
    }

    private static bool IsServiceTemporaryFile(string fileName)
    {
        return fileName.StartsWith('~') || fileName.StartsWith('$');
    }

    private static string NormalizeFileKey(string fileName, string firstExt, string secondExt)
    {
        string name = fileName.Trim();
        bool removed;
        do
        {
            removed = false;
            if (!string.IsNullOrWhiteSpace(firstExt) &&
                name.EndsWith(firstExt, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^firstExt.Length];
                removed = true;
            }

            if (!string.IsNullOrWhiteSpace(secondExt) &&
                name.EndsWith(secondExt, StringComparison.OrdinalIgnoreCase))
            {
                name = name[..^secondExt.Length];
                removed = true;
            }
        }
        while (removed);

        return name.Trim();
    }

    private void FindUnpairedFiles(
        Dictionary<string, List<string>> sourceFiles,
        Dictionary<string, List<string>> targetFiles,
        Dictionary<string, List<string>> unpairedFiles)
    {
        foreach (var kvp in sourceFiles)
        {
            if (!targetFiles.ContainsKey(kvp.Key))
            {
                string dir = Path.GetDirectoryName(kvp.Value[0]) ?? string.Empty;
                if (!unpairedFiles.ContainsKey(dir))
                    unpairedFiles[dir] = new List<string>();
                unpairedFiles[dir].AddRange(kvp.Value);
            }
        }
    }

    private void OnProgressChanged(int percentage, string message)
    {
        ProgressChanged?.Invoke(this, new ProgressEventArgs(percentage, message));
    }

    /// <summary>
    /// Генерация текстового отчета
    /// </summary>
    public static string GenerateReport(CheckResult result, string firstExt, string secondExt)
    {
        var report = new StringBuilder();
        report.AppendLine("=== ФАЙЛЫ БЕЗ ПАРЫ ===");
        report.AppendLine();

        // Файлы первого формата без пары
        report.AppendLine($"{firstExt.ToUpper()} без {secondExt.ToUpper()} пары:");
        AppendUnpairedFiles(report, result.FirstWithoutPair);
        report.AppendLine();

        // Файлы второго формата без пары
        report.AppendLine($"{secondExt.ToUpper()} без {firstExt.ToUpper()} пары:");
        AppendUnpairedFiles(report, result.SecondWithoutPair);

        // Ошибки
        if (result.Errors.Count > 0)
        {
            report.AppendLine("=== ОШИБКИ ===");
            foreach (var error in result.Errors)
            {
                report.AppendLine($"  {error}");
            }
            report.AppendLine();
        }

        // Статистика
        report.AppendLine("=== СТАТИСТИКА ===");
        report.AppendLine($"Всего файлов {firstExt}: {result.TotalFirstFiles}");
        report.AppendLine($"Всего файлов {secondExt}: {result.TotalSecondFiles}");
        report.AppendLine($"Файлов {firstExt} без пары: {result.FirstWithoutPair.Values.Sum(v => v.Count)}");
        report.AppendLine($"Файлов {secondExt} без пары: {result.SecondWithoutPair.Values.Sum(v => v.Count)}");

        return report.ToString();
    }

    private static void AppendUnpairedFiles(StringBuilder report, Dictionary<string, List<string>> unpairedFiles)
    {
        if (unpairedFiles.Count == 0)
        {
            report.AppendLine("  (отсутствуют)");
        }
        else
        {
            foreach (var folderGroup in unpairedFiles.Keys.OrderBy(k => k))
            {
                report.AppendLine($"  Папка: {folderGroup}");
                foreach (var file in unpairedFiles[folderGroup].OrderBy(f => f))
                {
                    report.AppendLine($"    {Path.GetFileNameWithoutExtension(file)}");
                }
                report.AppendLine();
            }
        }
    }
}

/// <summary>
/// Аргументы события прогресса
/// </summary>
public class ProgressEventArgs : EventArgs
{
    public int Percentage { get; }
    public string Message { get; }

    public ProgressEventArgs(int percentage, string message)
    {
        Percentage = percentage;
        Message = message;
    }
}
