using System.Collections.Generic;

public readonly struct StepResolution
{
    public StepStatus Status { get; }
    public bool ResetProgress { get; }

    public StepResolution(StepStatus status, bool resetProgress = false)
    {
        Status = status;
        ResetProgress = resetProgress;
    }
}

public interface IModePolicy
{
    StepResolution Resolve(StepState step, ConditionOutcome outcome);

    IReadOnlyList<string> GetStepsToSkip(RunState run);
}
