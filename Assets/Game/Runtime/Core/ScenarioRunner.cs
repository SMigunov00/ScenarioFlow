using System;
using UnityEngine;

public class ScenarioRunner : MonoBehaviour
{
    [SerializeField] private ScenarioData _scenarioData;
    [SerializeField] private RunSettings _defaultSettings = new RunSettings();

    private RunSettings _runtimeSettings;
    public bool IsRunning { get; private set; }

    public bool HasRun => _runtimeSettings != null;
    public AnalyzerType SelectedAnalyzer => _runtimeSettings != null ? _runtimeSettings.AnalyzerType : _defaultSettings.AnalyzerType;

    public event Action<ScenarioData, RunSettings> RunStarted;
    public event Action RunFinished;
    public event Action<AnalyzerType> AnalysisRequested;

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

        _runtimeSettings = _defaultSettings.CreateCopy();
        IsRunning = true;
        Debug.Log($"Запуск:{_scenarioData.ScenarioName}\n" +
                  $"Режим: {_runtimeSettings.Mode}\n" +
                  $"Логгер: {_runtimeSettings.LoggerType}\n" +
                  $"Анализатор: {_runtimeSettings.AnalyzerType}\n" +
                  $"{this}");

        RunStarted?.Invoke(_scenarioData, _runtimeSettings.CreateCopy());
    }

    public void FinishScenario()
    {
        if (!IsRunning) return;
        IsRunning = false;
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
            Debug.LogWarning($"Отсутсвуют прохождения для анализа {this}");
            return;
        }
        AnalysisRequested?.Invoke(_runtimeSettings.AnalyzerType);
    }
}