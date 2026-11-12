using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ScenarioPresenter : MonoBehaviour
{
    [Header("Системы")] [SerializeField] private ScenarioRunner _runner;
    [SerializeField] private ScenarioAnalysisController _analysis;

    [Header("Текст")] [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private TextMeshProUGUI _tasksText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _journalText;
    [SerializeField] private TextMeshProUGUI _resultText;

    [Header("Кнопки")] [SerializeField] private Button _trainingButton;
    [SerializeField] private Button _examButton;
    [SerializeField] private Button _repeatButton;
    [SerializeField] private Button _recalculateButton;
    [SerializeField] private Button _trainingAnalyzerButton;
    [SerializeField] private Button _examAnalyzerButton;
    [SerializeField] private Button _loadJournalButton;

    [Header("Загрузка журнала")] 
    [SerializeField] private TMP_InputField _journalPathInput;

    [Header("Подготовка сцены перед запуском")] 
    [SerializeField] private UnityEvent _beforeRunStart = new ();
    
    [Header("История прохождений")]
    [SerializeField] private Button _previousRunButton;
    [SerializeField] private Button _nextRunButton;
    [SerializeField] private TextMeshProUGUI _historyText;

    private readonly List<string> _journalFiles = new();
    private int _journalIndex = -1;

    private RunJournal _displayedJournal;
    private AnalyzerType _selectedAnalyzer;
    private int _analyzedEventCount = -1;

    private void OnEnable()
    {
        if (_runner == null || _analysis == null || _stateText == null || _tasksText == null ||
            _messageText == null || _journalText == null || _resultText == null)
        {
            Debug.LogError($"Назначь ссылки ScenarioPresenter в Inspector {this}");
            enabled = false;
            return;
        }

        _runner.RunStarted += OnRunStarted;
        _runner.RunFinished += OnRunFinished;
        _runner.StateChanged += OnStateChanged;
        _runner.EventRecorded += OnEventRecorded;

        _analysis.AnalysisCompleted += OnAnalysisCompleted;

        _selectedAnalyzer = _runner.SelectedAnalyzer;
        _displayedJournal = _runner.HasRun ? _runner.CurrentJournal : null;

        RefreshState();
        RefreshJournal();
        ShowResult(_analysis.LastResult);
        RefreshHistory();
        
        if (!_runner.HasRun && _journalFiles.Count > 0)
            OpenJournal(_journalFiles[_journalFiles.Count - 1]);
    }

    private void OnDisable()
    {
        if (_runner != null)
        {
            _runner.RunStarted -= OnRunStarted;
            _runner.RunFinished -= OnRunFinished;
            _runner.StateChanged -= OnStateChanged;
            _runner.EventRecorded -= OnEventRecorded;
        }

        if (_analysis != null)
            _analysis.AnalysisCompleted -= OnAnalysisCompleted;
    }

    public void StartTraining()
    {
        StartRun(RunMode.Training);
    }

    public void StartExam()
    {
        StartRun(RunMode.Exam);
    }

    public void Repeat()
    {
        if (!_runner.HasRun || _runner.IsRunning)
            return;

        StartRun(_runner.CurrentMode);
    }

    private void StartRun(RunMode mode)
    {
        if (_runner.IsRunning)
            return;

        _beforeRunStart?.Invoke();

        if (!_runner.TryStartScenario(mode))
            _messageText.text = "Сценарий не запущен. Проверь настройки в Inspector и сообщение в Console.";
        
    }

    public void UseTrainingAnalyzer()
    {
        SelectAnalyzer(AnalyzerType.TrainigScore);
    }

    public void UseExamAnalyzer()
    {
        SelectAnalyzer(AnalyzerType.ExamScore);
    }

    private void SelectAnalyzer(AnalyzerType analyzerType)
    {
        _selectedAnalyzer = analyzerType;

        if (_displayedJournal == null) return;

        if (_displayedJournal == _runner.CurrentJournal)
            _runner.SelectAnalyzer(analyzerType);
        else
            _analysis.AnalyzeJournal(_displayedJournal, analyzerType);
        
    }

    public void Recalculate()
    {
        if (_displayedJournal == null) return;

        if (_displayedJournal == _runner.CurrentJournal)
            _runner.RequestAnalysis();
        else
            _analysis.AnalyzeJournal(_displayedJournal, _selectedAnalyzer);
    }

    public void LoadJournal()
    {
        if (_runner.IsRunning)
            return;

        string path = _journalPathInput != null ? _journalPathInput.text.Trim().Trim('"') : string.Empty;
        OpenJournal(path);
    }

    private void OpenJournal(string path)
    {
        if (!JsonRunLogger.TryLoad(path, out RunJournal loaded, out string error))
        {
            _messageText.text = $"Не удалось загрузить журнал: {error}";
            return;
        }

        path = Path.GetFullPath(path);

        _displayedJournal = loaded;
        _analyzedEventCount = -1;

        _journalIndex = _journalFiles.FindIndex(
            file => string.Equals(
                file, path, StringComparison.OrdinalIgnoreCase));

        _journalPathInput?.SetTextWithoutNotify(path);

        _messageText.text = $"Загружено прохождение {loaded.RunId}.";
        _resultText.text = "Оценка ещё не рассчитана.";

        RefreshState();
        RefreshJournal();
        RefreshHistoryControls();
        
        Recalculate();
    }

    private void OnRunStarted(ScenarioData scenario, RunSettings settings)
    {
        _displayedJournal = _runner.CurrentJournal;
        _selectedAnalyzer = settings.AnalyzerType;
        _analyzedEventCount = -1;

        _messageText.text = "Сценарий запущен.";
        _resultText.text = "Оценка ещё не рассчитана.";
        
        _journalIndex = -1;

        _journalPathInput?.SetTextWithoutNotify(_runner.CurrentJournalPath ?? string.Empty);

        RefreshHistoryControls();

        RefreshState();
        RefreshJournal();
    }

    private void OnRunFinished()
    {
        RefreshHistory();
        RefreshState();
        RefreshJournal();
    }

    private void OnStateChanged()
    {
        RefreshState();

        if (_runner.IsRunning && _displayedJournal == _runner.CurrentJournal &&
            _analyzedEventCount >= 0 && _displayedJournal.Events.Count != _analyzedEventCount)
        {
            _resultText.text = "После расчёта появились новые события. Нажмите «Пересчитать».";
        }
    }

    private void OnEventRecorded(RunEvent entry)
    {
        if (_displayedJournal == _runner.CurrentJournal)
            RefreshJournal();

        if (entry.Type == RunEventTypes.AttemptResolved)
        {
            _messageText.text = entry.Result == "Violation" ? $"Нарушение в шаге {entry.StepId}: {entry.Reason}" : $"Выполнен шаг {entry.StepId}.";
        }
        else if (entry.Type == RunEventTypes.ConditionEvaluated && entry.Result == "ConfigurationError")
        {
            _messageText.text = $"Ошибка настройки: {entry.Reason}";
        }
    }

    private void OnAnalysisCompleted(AnalysisResult result)
    {
        if (_displayedJournal == null || result.RunId != _displayedJournal.RunId) return;
        
        _analyzedEventCount = _displayedJournal.Events.Count;

        if (_displayedJournal == _runner.CurrentJournal)
            _selectedAnalyzer = _runner.SelectedAnalyzer;

        ShowResult(result);
    }

    private void RefreshState()
    {
        bool running = _runner.IsRunning;
        bool hasJournal = _displayedJournal != null;

        SetInteractable(_trainingButton, !running);
        SetInteractable(_examButton, !running);
        SetInteractable(_repeatButton, _runner.HasRun && !running);
        SetInteractable(_loadJournalButton, !running);

        SetInteractable(_recalculateButton, hasJournal);
        SetInteractable(_trainingAnalyzerButton, hasJournal);
        SetInteractable(_examAnalyzerButton, hasJournal);

        RefreshHistoryControls();

        if (_displayedJournal != null && _displayedJournal != _runner.CurrentJournal)
        {
            string archiveMode = _displayedJournal.Mode == RunMode.Training ? "Тренировка" : "Экзамен";

            _stateText.text = $"Архив: {archiveMode}";
            _tasksText.text = "Просмотр сохранённого прохождения.";
            return;
        }
        
        if (!_runner.HasRun)
        {
            _stateText.text = "Выбери режим прохождения.";
            _tasksText.text = "Сценарий ещё не запущен.";
            return;
        }

        string mode = _runner.CurrentMode == RunMode.Training ? "Тренировка" : "Экзамен";

        _stateText.text = $"Режим: {mode}\n Состояние: {(running ? "идёт прохождение" : "завершено")}";

        var text = new StringBuilder();

        if (_runner.Scenario != null)
        {
            foreach (StepData definition in _runner.Scenario.Steps)
            {
                if (!_runner.CurrentRun.TryGetStep(definition.Id, out StepState step) || step.Status != StepStatus.Active) continue;
                

                string title = string.IsNullOrWhiteSpace(definition.Title) ? definition.Id : definition.Title;

                text.AppendLine(title);
                text.AppendLine(definition.Instruction);
                text.AppendLine($"Попыток завершено: {step.AttemptsCount}. Нарушений: {step.ViolationsCount}.");
                text.AppendLine();
            }
        }

        _tasksText.text = text.Length > 0 ? text.ToString() : "Активных заданий нет.";
    }

    private void RefreshJournal()
    {
        if (_displayedJournal == null)
        {
            _journalText.text = "Журнал пока пуст.";
            return;
        }

        var text = new StringBuilder();

        text.AppendLine($"Прохождение: {_displayedJournal.RunId}");

        foreach (RunEvent entry in _displayedJournal.Events)
        {
            if (entry == null) continue;

            text.AppendLine($"{entry.Sequence}. {entry.Time}\n {entry.StepId} | {entry.Type} | {entry.Result}");

            if (!string.IsNullOrWhiteSpace(entry.Reason))
                text.AppendLine(entry.Reason);

            text.AppendLine();
        }

        _journalText.text = text.ToString();
    }
    
    public void RefreshHistory()
    {
        if (_runner.IsRunning) return;

        string selectedPath = _journalPathInput != null ? _journalPathInput.text : string.Empty;

        _journalFiles.Clear();
        _journalIndex = -1;

        try
        {
            string directory = _runner.JournalDirectory;

            if (Directory.Exists(directory))
            {
                string[] files = Directory.GetFiles(directory, "*.json")
                    .OrderBy(File.GetLastWriteTimeUtc)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray();

                foreach (string file in files)
                {
                    if (!JsonRunLogger.TryLoad(file, out RunJournal journal, out string error))
                    {
                        Debug.LogWarning($"Не удалось прочитать журнал {file}: {error}");
                        continue;
                    }

                    bool finished = journal.Events.Any(entry => entry != null && entry.Type == RunEventTypes.RunFinished);

                    if (finished)
                        _journalFiles.Add(file);
                }
            }

            _journalIndex = _journalFiles.FindIndex(path => string.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            _messageText.text = $"Не удалось прочитать историю: {exception.Message}";
        }

        RefreshHistoryControls();
    }
    
    public void PreviousRun() => SelectRun(-1);
    

    public void NextRun() => SelectRun(1);
    

    private void SelectRun(int direction)
    {
        if (_runner.IsRunning || _journalFiles.Count == 0)
            return;

        int index = _journalIndex < 0
            ? _journalFiles.Count - 1
            : _journalIndex + direction;

        if (index < 0 || index >= _journalFiles.Count)
            return;

        OpenJournal(_journalFiles[index]);
    }

    private void RefreshHistoryControls()
    {
        bool available = !_runner.IsRunning && _journalFiles.Count > 0;

        SetInteractable(_previousRunButton, available && (_journalIndex < 0 || _journalIndex > 0));

        SetInteractable(_nextRunButton, available && (_journalIndex < 0 || _journalIndex < _journalFiles.Count - 1));

        if (_historyText != null)
        {
            _historyText.text = _journalIndex >= 0 ? 
                $"Прохождение {_journalIndex + 1} / {_journalFiles.Count}" : 
                $"Сохранённых прохождений: {_journalFiles.Count}";
        }
    }

    private void ShowResult(AnalysisResult result)
    {
        if (result == null || _displayedJournal == null || result.RunId != _displayedJournal.RunId)
        {
            _resultText.text = "Оценка ещё не рассчитана.";
            return;
        }

        var text = new StringBuilder();

        text.AppendLine(result.AnalyzerName);
        text.AppendLine(result.IsFinal ? "Итоговая оценка" : "Промежуточная оценка");

        text.AppendLine(
            result.Score.HasValue ? $"{result.Score.Value}/{result.MaximumScore}" : "Недостаточно данных для расчёта.");

        foreach (StepAnalysis step in result.Steps)
        {
            text.Append($"\n{step.StepId}: {StatusLabel(step.Status)}");

            if (step.SuccessfulAttemptNumber.HasValue)
                text.Append($", попытка {step.SuccessfulAttemptNumber.Value}");
            

            if (step.Points.HasValue)
                text.Append($", баллы {step.Points.Value}");

            if (step.Penalty.HasValue)
                text.Append($", штраф {step.Penalty.Value}");
        }

        foreach (string warning in result.Warnings)
            text.AppendLine($"\nПредупреждение: {warning}");

        foreach (string error in result.Errors)
            text.AppendLine($"\nОшибка: {error}");

        _resultText.text = text.ToString();
    }

    private static string StatusLabel(StepStatus status)
    {
        switch (status)
        {
            case StepStatus.Waiting: return "Ожидает";
            case StepStatus.Active: return "Доступен";
            case StepStatus.Completed: return "Выполнен (Completed)";
            case StepStatus.Failed: return "Ошибка (Failed)";
            case StepStatus.Skipped: return "Пропущен (Skipped)";
            default: return status.ToString();
        }
    }

    private static void SetInteractable(Button button, bool value)
    {
        if (button != null)
            button.interactable = value;
    }
    
    
    public void DeleteSelectedJournal()
    {
        if (_runner.IsRunning)
        {
            _messageText.text = "Заверши прохождение перед удалением журнала.";
            return;
        }

        if (_displayedJournal == null || _journalIndex < 0 || _journalIndex >= _journalFiles.Count)
        {
            _messageText.text = "Выбери сохранённое прохождение кнопками истории.";
            return;
        }

        string path = _journalFiles[_journalIndex];
        string runId = _displayedJournal.RunId;
        
        if (!JsonRunLogger.TryLoad(path, out RunJournal journal, out string error))
        {
            _messageText.text = $"Не удалось прочитать журнал: {error}";
            return;
        }

        if (journal.RunId != runId)
        {
            _messageText.text = "Файл изменился. Обнови историю и выбери прохождение заново.";
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception exception)
        {
            _messageText.text = $"Не удалось удалить JSON: {exception.Message}";
            return;
        }
        
        if (_runner.CurrentJournal != null && _runner.CurrentJournal.RunId == runId)
            _runner.ClearFinishedRun();
        
        ClearJournalView();
        _messageText.text = "Прохождение удалено.";
    }
    
    private void ClearJournalView()
    {
        _displayedJournal = null;
        _journalIndex = -1;
        _analyzedEventCount = -1;

        _journalPathInput?.SetTextWithoutNotify(string.Empty);

        _analysis.ClearResult();

        RefreshHistory();
        RefreshState();
        RefreshJournal();
        ShowResult(null);
    }
}