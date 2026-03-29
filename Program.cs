namespace PairChecker;

/// <summary>
/// Точка входа приложения
/// </summary>
static class Program
{
    /// <summary>
    /// Главный метод запуска приложения
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Включаем визуальные стили Windows
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // Запускаем главную форму
        Application.Run(new MainForm());
    }
}