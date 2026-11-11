using System;

public enum StepStatus
{
    Waiting,
    Active,
    Skipped,
    Failed,
    Completed
}
    
public sealed class StepState 
{
    public string StepId { get; }
    public StepStatus Status { get; private set; }
    public int AttemptsCount { get; private set; }
    public int ViolationsCount { get; private set; }
    public int ConditionProgress { get; private set; }
    public bool IsTerminal => Status == StepStatus.Completed || Status == StepStatus.Failed || Status == StepStatus.Skipped;
    
    public StepState(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException($"Заполните пустой Id шага {nameof(stepId)}");
        
        StepId = stepId;
        Status = StepStatus.Waiting;
        AttemptsCount = 0;
        ViolationsCount = 0;
        ConditionProgress = 0;
    }
    
    internal void Activate()
    {
        if (Status != StepStatus.Waiting)
        {
            throw new InvalidOperationException($"Нельзя активировать шаг {StepId}: {Status}");
        }

        Status = StepStatus.Active;
    }
    
    internal void ApplyAttempt(
        ConditionOutcome outcome,
        StepResolution resolution)
    {
        if (outcome != ConditionOutcome.Success &&
            outcome != ConditionOutcome.Violation)
        {
            throw new ArgumentException(
                "Результат не завершает попытку.",
                nameof(outcome));
        }

        if (IsTerminal)
        {
            throw new InvalidOperationException(
                $"Шаг {StepId} уже завершён.");
        }

        if (outcome == ConditionOutcome.Success &&
            Status != StepStatus.Active)
        {
            throw new InvalidOperationException(
                $"Неактивный шаг {StepId} нельзя выполнить.");
        }

        AttemptsCount++;

        if (outcome == ConditionOutcome.Violation)
            ViolationsCount++;

        Status = resolution.Status;

        if (resolution.ResetProgress)
            ConditionProgress = 0;
    }

    internal void Skip()
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException(
                $"Шаг {StepId} уже завершён.");
        }

        Status = StepStatus.Skipped;
    }
    
    internal void SetConditionProgress(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        ConditionProgress = value;
    }
}