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
    [field: SerializeField] public RunJournal CurrentJournal { get; private set; }

    [SerializeField] private List<ScenarioActionSource> _actionSources = new();
    private readonly HashSet<ScenarioActionSource> _subscribedSources = new();

    [SerializeField] private ScenarioContext _conditionContext;
    private readonly ConditionEvaluator _conditionEvaluator = new();

    private ActivationScheduler _activationScheduler;
    private readonly Queue<ScenarioAction> _pendingActions = new();

    private bool _isProcessingActions;
    
    private IModePolicy _modePolicy;

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
        
        if (_scenarioData == null || _scenarioData.Steps.Count == 0 || _defaultSettings == null)
        {
            Debug.LogError($"Не назначен сценарий, отсутствуют шаги или настройки запуска {this}");
            return;
        }
        RunSettings newSettings = _defaultSettings.CreateCopy();
        RunState runState;
        ActivationScheduler activationScheduler;
        IRunLogger newLogger;
        IModePolicy newModePolicy;

        try
        {
            runState = new RunState(_scenarioData);
            activationScheduler = new ActivationScheduler(_scenarioData.GetActivationPlan(newSettings.Mode), runState);
            newLogger = CreateLogger(newSettings.LoggerType);
            newModePolicy = CreateModePolicy(newSettings.Mode, runState);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"Не удалось запустить сценарий: {exception.Message} {this}");
            return;
        }
        _pendingActions.Clear();
        _runtimeSettings = newSettings;
        CurrentRun = runState;
        _activationScheduler = activationScheduler;
        logger = newLogger;
        _modePolicy = newModePolicy;
        
        CurrentJournal = new RunJournal(CurrentRun, _runtimeSettings);
        RecordEvent(RunEventTypes.RunStarted, result: RunStatus.Running.ToString());
        RefreshActivation();
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

    private void RefreshActivation()
    {
        if (!IsRunning) return;
        
        while (true)
        {
            IReadOnlyList<string> stepsToSkip = _modePolicy.GetStepsToSkip(CurrentRun);

            if (stepsToSkip.Count == 0) break;

            foreach (string stepId in stepsToSkip)
            {
                StepState step = CurrentRun.Steps[stepId];
                step.Skip();
                RecordEvent(RunEventTypes.StepStatusChanged, 
                    stepId: stepId, 
                    result: StepStatus.Skipped.ToString(),
                    reason: "Достигнут порог нарушений группы");
            }
        }
        
        IReadOnlyList<StepState> stepsToActivate = _activationScheduler.GetStepsToActivate(CurrentRun);

        foreach (StepState step in stepsToActivate)
        {
            step.Activate();
            RecordEvent(RunEventTypes.StepActivated, stepId: step.StepId, result: StepStatus.Active.ToString());
        }

        if (CurrentRun.AllStepsTerminal)
            FinishScenario();
    }

    public void FinishScenario()
    {
        if (!IsRunning) return;

        if (!CurrentRun.AllStepsTerminal)
        {
            Debug.LogWarning($"В сценарии остались незавершенные шаги {this}");
            return;
        }

        CurrentRun.Finish();
        _pendingActions.Clear();
        RecordEvent(RunEventTypes.RunFinished, result: RunStatus.Finished.ToString());
        RunFinished?.Invoke();
        RequestAnalysis();
    }

    public void SelectAnalyzer(AnalyzerType analyzerType)
    {
        if (!HasRun)
        {
            Debug.LogWarning($"Сначала необходимо запустить сценарий {this}");
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
        if (!IsRunning || action == null) return;

        _pendingActions.Enqueue(action);

        if (_isProcessingActions) return;

        _isProcessingActions = true;

        try
        {
            while (IsRunning && _pendingActions.Count > 0)
                ProcessAction(_pendingActions.Dequeue());
        }
        finally
        {
            _isProcessingActions = false;
            if (!IsRunning)
                _pendingActions.Clear();
        }
    }

    private void ProcessAction(ScenarioAction action)
    {
        if (!CurrentRun.TryGetStep(action.StepId, out StepState step))
        {
            Debug.LogError($"Неизвестный шаг: {action.StepId} {this}");
            return;
        }

        if (step.IsTerminal) return;

        if (step.Status == StepStatus.Waiting && _runtimeSettings.OutOfOrderBehavior == OutOfOrderBehavior.IgnoreAction) return;

        RecordEvent(new RunEvent
        {
            Type = RunEventTypes.ActionReceived,
            StepId = action.StepId,
            ActionId = action.ActionId,
            SourceId = action.SourceId,
            Result = "Received"
        });

        if (step.Status == StepStatus.Waiting)
        {
            ReportViolation(action, "Шаг ещё недоступен");
            return;
        }

        StepData definition = FindStepDefinition(action.StepId);
        StepEvaluationResult evaluation = _conditionEvaluator.Evaluate(definition?.Condition, step.ConditionProgress, action, _conditionContext);
        
        if (evaluation.Outcome != ConditionOutcome.ConfigurationError)
            step.SetConditionProgress(evaluation.NextActionIndex);
        
        RecordEvent(new RunEvent
        {
            Type = RunEventTypes.ConditionEvaluated,
            StepId = action.StepId,
            ActionId = action.ActionId,
            SourceId = action.SourceId,
            Result = evaluation.Outcome.ToString(),
            Reason = evaluation.Reason
        });

        switch (evaluation.Outcome)
        {
            case ConditionOutcome.Success:
            case ConditionOutcome.Violation:
                ResolveAttempt(action, step, evaluation.Outcome, evaluation.Reason);
                break;

            case ConditionOutcome.ConfigurationError:
                Debug.LogError(evaluation.Reason, this);
                break;
        }
    }

    private void ReportViolation(ScenarioAction action, string reason)
    {
        if (!CurrentRun.TryGetStep(action.StepId, out StepState step)) return;

        ResolveAttempt(action, step, ConditionOutcome.Violation, reason);
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
        CurrentJournal.Append(runEvent);

        try
        {
            logger.Write(CurrentJournal, runEvent);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Не удалось вывести или сохранить событие: {exception.Message} {this}");
        }
    }

    private StepData FindStepDefinition(string stepId)
    {
        foreach (StepData step in _scenarioData.Steps)
        {
            if (step != null && step.Id == stepId)
                return step;
        }

        return null;
    }
    
    private IModePolicy CreateModePolicy(RunMode mode, RunState run)
    {
        switch (mode)
        {
            case RunMode.Training:
                return new TrainingModePolicy();

            case RunMode.Exam:
                return new ExamModePolicy(
                    _scenarioData.ExamGroups,
                    _scenarioData.GetActivationPlan(mode),
                    run);

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
    
    private void ResolveAttempt(ScenarioAction action, StepState step, ConditionOutcome outcome, string reason)
    {
        StepResolution resolution = _modePolicy.Resolve(step, outcome);
        StepStatus previousStatus = step.Status;
        step.ApplyAttempt(outcome, resolution);
        RecordEvent(RunEventTypes.AttemptResolved, stepId: step.StepId, result: outcome.ToString(), attemptNumber: step.AttemptsCount, reason: reason);
       
        if (step.Status != previousStatus)
            RecordEvent(RunEventTypes.StepStatusChanged, stepId: step.StepId, result: step.Status.ToString());
        
        RefreshActivation();
    }
}