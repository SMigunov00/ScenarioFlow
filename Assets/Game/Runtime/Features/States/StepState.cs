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

    public StepState(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException($"Заполните пустой Id шага {nameof(stepId)}");
        
        StepId = stepId;
        Status = StepStatus.Waiting;
        AttemptsCount = 0;
        ViolationsCount = 0;
    }
}