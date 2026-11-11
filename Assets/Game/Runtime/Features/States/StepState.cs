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
    
    internal void Complete()
    {
        if (Status != StepStatus.Active)
        {
            throw new InvalidOperationException($"Нельзя завершить шаг {StepId}: {Status}");
        }

        Status = StepStatus.Completed;
    }
    
    internal void SetConditionProgress(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        ConditionProgress = value;
    }
}