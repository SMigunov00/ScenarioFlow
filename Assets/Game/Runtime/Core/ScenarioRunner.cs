using System;
using UnityEngine;
using System.Collections.Generic;

public sealed class ScenarioRunner : MonoBehaviour
{
    [SerializeField] private ScenarioData _scenarioData;
    [SerializeField] private RunSettings _defaultSettings = new();

    private RunSettings _runtimeSettings;
    public RunState CurrentRun { get; private set; }

    public bool IsRunning => CurrentRun != null && CurrentRun.Status == RunStatus.Running;
    public bool HasRun => CurrentRun != null;
    
    public AnalyzerType SelectedAnalyzer => _runtimeSettings != null ? _runtimeSettings.AnalyzerType : _defaultSettings.AnalyzerType;

    public event Action<ScenarioData, RunSettings> RunStarted;
    public event Action RunFinished;
    public event Action<AnalyzerType> AnalysisRequested;
    
    private IRunLogger logger;
    [SerializeField] private RunJournal _currentJournal;
    
    [SerializeField] private List<ScenarioActionSource> _actionSources = new();
    private readonly HashSet<ScenarioActionSource> _subscribedSources = new();
    
    
    private void OnEnable()
    {
        foreach (ScenarioActionSource source in _actionSources)
        {
            if (source == null || !_subscribedSources.Add(source)) continue;
            
            source.ActionRaised += HandleAction;
        }
    }

    private void OnDisable()
    {
        foreach (ScenarioActionSource source in _subscribedSources)
        {
            if (source != null)
                source.ActionRaised -= HandleAction;
        }

        _subscribedSources.Clear();
    }
    
    public void StartScenario()
    {
        if (IsRunning)
        {
            Debug.LogWarning($"Сценарий уже запущен {this}");
            return;
        }

        if (_scenarioData == null)
        {
            Debug.LogError($"Не назначен ScenarioData {this}");
            return;
        }

        if (_scenarioData.Steps.Count == 0)
        {
            Debug.LogError($"У сценария отсутствуют шаги {this}");
            return;
        }

        if (_defaultSettings == null)
        {
            Debug.LogError($"Не заданы настройки запуска {this}");
            return;
        }

        RunState newRun;
        try
        {
            newRun = new RunState(_scenarioData);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"Не удалось запустить сценарий: {exception.Message} {this}");
            return;
        }

        _runtimeSettings = _defaultSettings.CreateCopy();
        CurrentRun = newRun;
        Debug.Log($"Запуск:{_scenarioData.ScenarioName}\n" +
                  $"Режим: {_runtimeSettings.Mode}\n" +
                  $"Логгер: {_runtimeSettings.LoggerType}\n" +
                  $"Анализатор: {_runtimeSettings.AnalyzerType}\n" +
                  $"{this}");

        logger = CreateLogger(_runtimeSettings.LoggerType);
        _currentJournal = new RunJournal(CurrentRun, _runtimeSettings);

        RecordEvent(RunEventTypes.RunStarted, result: RunStatus.Running.ToString());
        RunStarted?.Invoke(_scenarioData, _runtimeSettings.CreateCopy());
    }
   
   private static IRunLogger CreateLogger(LoggerType loggerType)
   {
       switch (loggerType)
       {
           case LoggerType.Console:
               return new ConsoleRunLogger();
           case LoggerType.Json:
               return new JsonRunLogger();
           default:
               throw new ArgumentOutOfRangeException(nameof(loggerType));
       }
   }
    
    public void FinishScenario()
    {
        if (!IsRunning) return;

        CurrentRun.Finish();
        RecordEvent(RunEventTypes.RunFinished, result: RunStatus.Finished.ToString());
        RunFinished?.Invoke();
        RequestAnalysis();
    }

    public void SelectAnalyzer(AnalyzerType analyzerType)
    {
        if (!HasRun)
        {
            Debug.LogWarning($"Сначала запустите сценарий {this}");
            return;
        }
        _runtimeSettings.SetAnalyzer(analyzerType);
        RequestAnalysis();
    }

    public void RequestAnalysis()
    {
        if (!HasRun)
        {
            Debug.LogWarning($"Отсутствуют прохождения для анализа {this}");
            return;
        }
        AnalysisRequested?.Invoke(_runtimeSettings.AnalyzerType);
    }
    
    private void HandleAction(ScenarioAction action)
    {
        if (!IsRunning) return;

        if (!CurrentRun.TryGetStep(action.StepId, out _))
        {
            Debug.LogError($"Источник {action.SourceId} отправил действие для неизвестного шага {action.StepId} {this}");
            return;
        }

        RecordEvent(new RunEvent
        {
            Type = RunEventTypes.ActionReceived,

            StepId = action.StepId,
            ActionId = action.ActionId,
            SourceId = action.SourceId,

            Result = "Received"
        });
    }
    
    private void RecordEvent(string type, string stepId = null, string result = null, int attemptNumber = 0, string reason = null)
    {
        RecordEvent(new RunEvent
        {
            Type = type,
            StepId = stepId,
            Result = result,
            AttemptNumber = attemptNumber,
            Reason = reason
        });
    }

    private void RecordEvent(RunEvent runEvent)
    {
        _currentJournal.Append(runEvent);

        try
        {
            logger.Write(_currentJournal, runEvent);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Не удалось вывести или сохранить событие: {exception.Message} {this}");
        }
    }
}