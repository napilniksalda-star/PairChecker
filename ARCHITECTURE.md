# Архитектура PairChecker

## Диаграмма классов

```
┌─────────────────────────────────────────────────────────────┐
│                         Program.cs                          │
│                     (Точка входа)                           │
└────────────────────────────┬────────────────────────────────┘
                             │
                             │ создает
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                        MainForm.cs                          │
│                      (UI Layer)                             │
├─────────────────────────────────────────────────────────────┤
│ - txtFolder: TextBox                                        │
│ - cmbFirstFormat: ComboBox                                  │
│ - cmbSecondFormat: ComboBox                                 │
│ - progressBar: ProgressBar                                  │
│ - txtLog: TextBox                                           │
│ - fileChecker: FileChecker                                  │
│ - settings: AppSettings                                     │
├─────────────────────────────────────────────────────────────┤
│ + BtnBrowseFolder_Click()                                   │
│ + BtnStartCheck_Click()                                     │
│ + BtnSaveReport_Click()                                     │
│ + OnFileCheckerProgress()                                   │
│ - LoadSettings()                                            │
│ - MainForm_FormClosing()                                    │
└──────────────┬──────────────────────┬───────────────────────┘
               │                      │
               │ использует           │ использует
               ▼                      ▼
┌──────────────────────────┐  ┌──────────────────────────┐
│    FileChecker.cs        │  │    AppSettings.cs        │
│  (Business Logic)        │  │  (Configuration)         │
├──────────────────────────┤  ├──────────────────────────┤
│ + CheckFilesAsync()      │  │ + LastFolder: string     │
│ + GenerateReport()       │  │ + LastFirstFormat: str   │
│ - PerformCheck()         │  │ + LastSecondFormat: str  │
│ - CollectFiles()         │  │ + LastRecursive: bool    │
│ - FindUnpairedFiles()    │  ├──────────────────────────┤
├──────────────────────────┤  │ + Load(): AppSettings    │
│ + ProgressChanged event  │  │ + Save(): void           │
└──────────────────────────┘  └──────────────────────────┘
               │
               │ возвращает
               ▼
┌──────────────────────────┐
│   CheckResult (class)    │
├──────────────────────────┤
│ + FirstWithoutPair       │
│ + SecondWithoutPair      │
│ + TotalFirstFiles        │
│ + TotalSecondFiles       │
│ + Errors                 │
└──────────────────────────┘
```

## Поток данных

```
Пользователь
    │
    │ 1. Выбирает папку и форматы
    ▼
MainForm
    │
    │ 2. Вызывает CheckFilesAsync()
    ▼
FileChecker
    │
    │ 3. Сканирует файлы
    │ 4. Анализирует пары
    │ 5. Отправляет события прогресса
    │
    ▼
CheckResult
    │
    │ 6. Возвращает результат
    ▼
MainForm
    │
    │ 7. Генерирует отчет
    │ 8. Отображает результаты
    ▼
Пользователь
```

## Слои приложения

```
┌─────────────────────────────────────────────┐
│         Presentation Layer (UI)             │
│                                             │
│  MainForm.cs - Windows Forms UI             │
│  - Отображение данных                       │
│  - Обработка событий пользователя           │
│  - Валидация ввода                          │
└──────────────────┬──────────────────────────┘
                   │
                   │ Events & Callbacks
                   │
┌──────────────────▼──────────────────────────┐
│       Business Logic Layer                  │
│                                             │
│  FileChecker.cs - Логика проверки           │
│  - Поиск файлов                             │
│  - Анализ пар                               │
│  - Генерация отчетов                        │
└──────────────────┬──────────────────────────┘
                   │
                   │ File System API
                   │
┌──────────────────▼──────────────────────────┐
│         Data Access Layer                   │
│                                             │
│  System.IO - Работа с файловой системой     │
│  - Directory.EnumerateFiles()               │
│  - File operations                          │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│      Configuration Layer                    │
│                                             │
│  AppSettings.cs - Настройки приложения      │
│  - Сохранение в JSON                        │
│  - Загрузка из JSON                         │
└─────────────────────────────────────────────┘
```

## Паттерны проектирования

### 1. **Separation of Concerns (SoC)**
- UI отделен от бизнес-логики
- Каждый класс имеет одну ответственность

### 2. **Observer Pattern**
```csharp
// FileChecker уведомляет MainForm о прогрессе
fileChecker.ProgressChanged += OnFileCheckerProgress;
```

### 3. **Data Transfer Object (DTO)**
```csharp
// CheckResult - объект для передачи данных
public class CheckResult { ... }
```

### 4. **Factory Method (потенциально)**
```csharp
// Можно добавить фабрику для создания разных типов проверок
public interface IFileCheckerFactory
{
    IFileChecker CreateChecker(CheckType type);
}
```

## Преимущества новой архитектуры

### ✅ Тестируемость
```
MainForm (сложно тестировать)
    ↓
FileChecker (легко тестировать)
    ↓
Unit Tests ✓
```

### ✅ Расширяемость
```
FileChecker
    ↓
IFileChecker (интерфейс)
    ↓
- BasicFileChecker
- AdvancedFileChecker
- CachedFileChecker
```

### ✅ Поддерживаемость
```
Изменение логики → FileChecker.cs
Изменение UI → MainForm.cs
Изменение настроек → AppSettings.cs
```

### ✅ Повторное использование
```
FileChecker можно использовать:
- В WinForms приложении
- В консольном приложении
- В веб-сервисе
- В библиотеке
```

## Зависимости

```
Program.cs
    └── MainForm.cs
            ├── FileChecker.cs
            │       └── CheckResult
            │       └── ProgressEventArgs
            └── AppSettings.cs

Внешние зависимости:
- System.Windows.Forms (UI)
- System.IO (File System)
- System.Text.Json (Settings)
- System.Threading.Tasks (Async)
```

## Жизненный цикл объектов

```
Application Start
    │
    ├─> Program.Main()
    │       │
    │       └─> new MainForm()
    │               │
    │               ├─> new FileChecker()
    │               │       └─> (живет весь цикл формы)
    │               │
    │               └─> AppSettings.Load()
    │                       └─> (загружается один раз)
    │
    ├─> User Action: "Start Check"
    │       │
    │       └─> fileChecker.CheckFilesAsync()
    │               │
    │               ├─> new CheckResult()
    │               │       └─> (возвращается в MainForm)
    │               │
    │               └─> ProgressChanged events
    │                       └─> (обрабатываются MainForm)
    │
    └─> Application Close
            │
            └─> settings.Save()
                    └─> (сохраняется в файл)
```

## Потоки выполнения

```
Main Thread (UI Thread)
    │
    ├─> MainForm UI Events
    │   - Button clicks
    │   - Text input
    │   - Progress updates
    │
    └─> Background Thread
            │
            └─> FileChecker.CheckFilesAsync()
                    │
                    ├─> File scanning
                    ├─> Analysis
                    └─> Report generation
                            │
                            └─> Invoke back to UI Thread
                                    └─> Update UI
```

---

Эта архитектура обеспечивает:
- 🎯 Четкое разделение ответственности
- 🧪 Высокую тестируемость
- 🔧 Легкую поддержку
- 📈 Возможность расширения
- 🚀 Хорошую производительность
