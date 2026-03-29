using System.Text;

namespace PairChecker;

/// <summary>
/// Главная форма приложения для проверки парных файлов
/// </summary>
public partial class MainForm : Form
{
    // Элементы интерфейса
    private Label lblFolder;
    private TextBox txtFolder;
    private Button btnBrowseFolder;
    private Label lblFirstFormat;
    private ComboBox cmbFirstFormat;
    private TextBox txtFirstFormat;
    private Label lblSecondFormat;
    private ComboBox cmbSecondFormat;
    private TextBox txtSecondFormat;
    private CheckBox chkRecursive;
    private Button btnStartCheck;
    private ProgressBar progressBar;
    private Label lblProgress;
    private TextBox txtLog;
    private Label lblStatistics;
    private Button btnSaveReport;

    // Данные для работы программы
    private CancellationTokenSource? cancellationTokenSource;
    private bool isProcessing;
    private readonly FileChecker fileChecker;
    private FileChecker.CheckResult? lastResult;
    private string lastFirstExt = string.Empty;
    private string lastSecondExt = string.Empty;
    private readonly AppSettings settings;

    // Предустановленные форматы файлов
    private readonly string[] presetFormats = new[]
    {
        ".pdf", ".dxf", ".dwg", ".doc", ".docx", 
        ".xlsx", ".xls", ".txt", ".jpg", ".png", 
        ".step", ".stp", ".iges", ".sldprt", ".sldasm", ".slddrw"
    };

    public MainForm()
    {
        fileChecker = new FileChecker();
        fileChecker.ProgressChanged += OnFileCheckerProgress;
        settings = AppSettings.Load();
        InitializeComponent();
        LoadSettings();
        this.FormClosing += MainForm_FormClosing;
    }

    /// <summary>
    /// Инициализация всех элементов формы
    /// </summary>
    private void InitializeComponent()
    {
        this.Text = "PairChecker - Проверка парных файлов";
        this.Size = new Size(700, 550);
        this.MinimumSize = new Size(700, 550);
        this.MaximumSize = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        int margin = 10;
        int currentY = margin;
        int labelWidth = 100;

        // Папка для проверки
        lblFolder = new Label
        {
            Text = "Папка проверки:",
            Location = new Point(margin, currentY + 5),
            Size = new Size(labelWidth, 20)
        };
        this.Controls.Add(lblFolder);

        txtFolder = new TextBox
        {
            Location = new Point(lblFolder.Right + 5, currentY),
            Size = new Size(440, 25),
            ReadOnly = true,
            BackColor = SystemColors.Window
        };
        this.Controls.Add(txtFolder);

        btnBrowseFolder = new Button
        {
            Text = "Обзор",
            Location = new Point(txtFolder.Right + 5, currentY),
            Size = new Size(80, 25)
        };
        btnBrowseFolder.Click += BtnBrowseFolder_Click;
        this.Controls.Add(btnBrowseFolder);

        currentY += 35;

        // Первый формат
        lblFirstFormat = new Label
        {
            Text = "Первый формат:",
            Location = new Point(margin, currentY + 5),
            Size = new Size(labelWidth, 20)
        };
        this.Controls.Add(lblFirstFormat);

        cmbFirstFormat = new ComboBox
        {
            Location = new Point(lblFirstFormat.Right + 5, currentY),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbFirstFormat.Items.AddRange(presetFormats);
        cmbFirstFormat.SelectedIndex = 0;
        this.Controls.Add(cmbFirstFormat);

        txtFirstFormat = new TextBox
        {
            Location = new Point(cmbFirstFormat.Right + 10, currentY),
            Size = new Size(100, 25),
            PlaceholderText = "или свой"
        };
        this.Controls.Add(txtFirstFormat);

        currentY += 35;

        // Второй формат
        lblSecondFormat = new Label
        {
            Text = "Второй формат:",
            Location = new Point(margin, currentY + 5),
            Size = new Size(labelWidth, 20)
        };
        this.Controls.Add(lblSecondFormat);

        cmbSecondFormat = new ComboBox
        {
            Location = new Point(lblSecondFormat.Right + 5, currentY),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbSecondFormat.Items.AddRange(presetFormats);
        cmbSecondFormat.SelectedIndex = 1;
        this.Controls.Add(cmbSecondFormat);

        txtSecondFormat = new TextBox
        {
            Location = new Point(cmbSecondFormat.Right + 10, currentY),
            Size = new Size(100, 25),
            PlaceholderText = "или свой"
        };
        this.Controls.Add(txtSecondFormat);

        currentY += 35;

        // Рекурсивный поиск
        chkRecursive = new CheckBox
        {
            Text = "Включить рекурсивный поиск (искать в подпапках)",
            Location = new Point(margin, currentY),
            AutoSize = true,
            Checked = false
        };
        this.Controls.Add(chkRecursive);

        currentY += 30;

        // Кнопка запуска проверки
        btnStartCheck = new Button
        {
            Text = "Запустить проверку",
            Location = new Point(margin, currentY),
            Size = new Size(160, 30),
            Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
        };
        btnStartCheck.Click += BtnStartCheck_Click;
        this.Controls.Add(btnStartCheck);

        // Кнопка сохранения отчета
        btnSaveReport = new Button
        {
            Text = "Сохранить отчет",
            Location = new Point(btnStartCheck.Right + 10, currentY),
            Size = new Size(140, 30),
            Enabled = false
        };
        btnSaveReport.Click += BtnSaveReport_Click;
        this.Controls.Add(btnSaveReport);

        currentY += 40;

        // Прогресс бар
        progressBar = new ProgressBar
        {
            Location = new Point(margin, currentY),
            Size = new Size(this.ClientSize.Width - margin * 2, 20),
            Style = ProgressBarStyle.Continuous
        };
        this.Controls.Add(progressBar);

        currentY += 25;

        // Метка прогресса
        lblProgress = new Label
        {
            Text = "Готов к работе",
            Location = new Point(margin, currentY),
            Size = new Size(this.ClientSize.Width - margin * 2, 20),
            ForeColor = Color.Blue
        };
        this.Controls.Add(lblProgress);

        currentY += 25;

        // Лог результатов
        txtLog = new TextBox
        {
            Location = new Point(margin, currentY),
            Size = new Size(this.ClientSize.Width - margin * 2, 240),
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            Font = new Font("Consolas", 8.5f)
        };
        this.Controls.Add(txtLog);

        currentY += 245;

        // Статистика
        lblStatistics = new Label
        {
            Text = "Статистика будет здесь после проверки",
            Location = new Point(margin, currentY),
            Size = new Size(this.ClientSize.Width - margin * 2, 20)
        };
        this.Controls.Add(lblStatistics);

        InitializeFolderDragDrop();
    }

    /// <summary>
    /// Загрузка сохраненных настроек
    /// </summary>
    private void LoadSettings()
    {
        txtFolder.Text = string.Empty;

        // Установка форматов
        int firstIndex = Array.IndexOf(presetFormats, settings.LastFirstFormat);
        if (firstIndex >= 0)
        {
            cmbFirstFormat.SelectedIndex = firstIndex;
        }

        int secondIndex = Array.IndexOf(presetFormats, settings.LastSecondFormat);
        if (secondIndex >= 0)
        {
            cmbSecondFormat.SelectedIndex = secondIndex;
        }

        chkRecursive.Checked = settings.LastRecursive;
    }

    /// <summary>
    /// Сохранение настроек при закрытии формы
    /// </summary>
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        settings.LastFirstFormat = GetFormat(txtFirstFormat.Text, cmbFirstFormat.SelectedItem?.ToString());
        settings.LastSecondFormat = GetFormat(txtSecondFormat.Text, cmbSecondFormat.SelectedItem?.ToString());
        settings.LastRecursive = chkRecursive.Checked;
        settings.Save();
    }

    /// <summary>
    /// Обработчик выбора папки
    /// </summary>
    private void InitializeFolderDragDrop()
    {
        RegisterFolderDropTarget(this);
    }

    private void RegisterFolderDropTarget(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += FolderDrop_DragEnter;
        control.DragDrop += FolderDrop_DragDrop;

        foreach (Control child in control.Controls)
        {
            RegisterFolderDropTarget(child);
        }
    }

    private void FolderDrop_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = TryGetDroppedFolder(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void FolderDrop_DragDrop(object? sender, DragEventArgs e)
    {
        if (TryGetDroppedFolder(e, out string folderPath))
        {
            txtFolder.Text = folderPath;
        }
    }

    private static bool TryGetDroppedFolder(DragEventArgs e, out string folderPath)
    {
        folderPath = string.Empty;

        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] droppedItems || droppedItems.Length == 0)
        {
            return false;
        }

        foreach (string droppedItem in droppedItems)
        {
            if (Directory.Exists(droppedItem))
            {
                folderPath = droppedItem;
                return true;
            }
        }

        return false;
    }

    private void BtnBrowseFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку для проверки",
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrEmpty(txtFolder.Text) && Directory.Exists(txtFolder.Text))
        {
            dialog.SelectedPath = txtFolder.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtFolder.Text = dialog.SelectedPath;
        }
    }

    /// <summary>
    /// Обработчик запуска/остановки проверки
    /// </summary>
    private async void BtnStartCheck_Click(object? sender, EventArgs e)
    {
        if (isProcessing)
        {
            cancellationTokenSource?.Cancel();
            return;
        }

        // Проверка наличия папки
        if (string.IsNullOrWhiteSpace(txtFolder.Text))
        {
            MessageBox.Show("Выберите папку для проверки", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(txtFolder.Text))
        {
            MessageBox.Show("Указанная папка не существует", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Получение форматов
        string firstExt = GetFormat(txtFirstFormat.Text, cmbFirstFormat.SelectedItem?.ToString());
        string secondExt = GetFormat(txtSecondFormat.Text, cmbSecondFormat.SelectedItem?.ToString());

        if (string.IsNullOrEmpty(firstExt) || string.IsNullOrEmpty(secondExt))
        {
            MessageBox.Show("Укажите оба формата файлов", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (firstExt.Equals(secondExt, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Форматы должны быть разными", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await StartCheckAsync(firstExt, secondExt, chkRecursive.Checked);
    }

    /// <summary>
    /// Получение формата файла с обработкой
    /// </summary>
    private string GetFormat(string customFormat, string? selectedFormat)
    {
        if (!string.IsNullOrWhiteSpace(customFormat))
        {
            string format = customFormat.Trim();
            if (!format.StartsWith("."))
                format = "." + format;
            return format;
        }

        return selectedFormat ?? string.Empty;
    }

    /// <summary>
    /// Запуск асинхронной проверки файлов
    /// </summary>
    private async Task StartCheckAsync(string firstExt, string secondExt, bool recursive)
    {
        isProcessing = true;
        btnStartCheck.Text = "Остановить";
        btnSaveReport.Enabled = false;
        txtLog.Clear();
        lblStatistics.Text = "Обработка...";
        progressBar.Value = 0;
        SetControlsEnabled(false);

        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        try
        {
            lastFirstExt = firstExt;
            lastSecondExt = secondExt;
            lastResult = await fileChecker.CheckFilesAsync(txtFolder.Text, firstExt, secondExt, recursive, token);
            
            // Формирование и отображение отчета
            string report = FileChecker.GenerateReport(lastResult, firstExt, secondExt);
            SetLogText(report);

            // Обновление статистики
            string stats = $"Файлов {firstExt}: {lastResult.TotalFirstFiles} | " +
                           $"Файлов {secondExt}: {lastResult.TotalSecondFiles} | " +
                           $"Без пары {firstExt}: {lastResult.FirstWithoutPair.Values.Sum(v => v.Count)} | " +
                           $"Без пары {secondExt}: {lastResult.SecondWithoutPair.Values.Sum(v => v.Count)}";
            UpdateStatistics(stats);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Операция отменена пользователем");
            lblProgress.Text = "Отменено";
        }
        catch (Exception ex)
        {
            AppendLog($"Ошибка: {ex.Message}");
            lblProgress.Text = "Ошибка выполнения";
            MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            isProcessing = false;
            btnStartCheck.Text = "Запустить проверку";
            btnSaveReport.Enabled = true;
            SetControlsEnabled(true);
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Обработчик события прогресса от FileChecker
    /// </summary>
    private void OnFileCheckerProgress(object? sender, ProgressEventArgs e)
    {
        UpdateProgress(e.Percentage, e.Message);
    }

    /// <summary>
    /// Обновление прогресса в UI потоке
    /// </summary>
    private void UpdateProgress(int value, string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateProgress(value, text)));
            return;
        }

        progressBar.Value = Math.Min(value, 100);
        lblProgress.Text = text;
    }

    /// <summary>
    /// Добавление текста в лог
    /// </summary>
    private void AppendLog(string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => AppendLog(text)));
            return;
        }

        txtLog.AppendText(text + Environment.NewLine);
    }

    /// <summary>
    /// Установка всего текста лога
    /// </summary>
    private void SetLogText(string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetLogText(text)));
            return;
        }

        txtLog.Text = text;
    }

    /// <summary>
    /// Обновление статистики
    /// </summary>
    private void UpdateStatistics(string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateStatistics(text)));
            return;
        }

        lblStatistics.Text = text;
    }

    /// <summary>
    /// Блокировка/разблокировка элементов управления
    /// </summary>
    private void SetControlsEnabled(bool enabled)
    {
        btnBrowseFolder.Enabled = enabled;
        cmbFirstFormat.Enabled = enabled;
        txtFirstFormat.Enabled = enabled;
        cmbSecondFormat.Enabled = enabled;
        txtSecondFormat.Enabled = enabled;
        chkRecursive.Enabled = enabled;
    }

    /// <summary>
    /// Сохранение отчета в файл
    /// </summary>
    private void BtnSaveReport_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtLog.Text))
        {
            MessageBox.Show("Нет данных для сохранения", "Информация", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
            DefaultExt = "txt",
            FileName = $"PairChecker_Report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                File.WriteAllText(dialog.FileName, txtLog.Text, Encoding.UTF8);
                MessageBox.Show("Отчет успешно сохранен", "Успех", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
