using System;
using System.Collections.Generic;

public sealed class TrainingModePolicy : IModePolicy
{
    public StepResolution Resolve(StepState step, ConditionOutcome outcome)
    {
        switch (outcome)
        {
            case ConditionOutcome.Success:
                return new StepResolution(StepStatus.Completed);

            case ConditionOutcome.Violation:
                return new StepResolution(step.Status, resetProgress: true);

            default:
                throw new ArgumentOutOfRangeException(nameof(outcome));
        }
    }

    public IReadOnlyList<string> GetStepsToSkip(RunState run) => Array.Empty<string>();
}