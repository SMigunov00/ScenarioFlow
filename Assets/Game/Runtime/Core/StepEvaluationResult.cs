public enum ConditionOutcome
{
    Ignore,
    Progress,
    Success,
    Violation,
    ConfigurationError
}

public sealed class StepEvaluationResult
{
    public ConditionOutcome Outcome { get; }
    public int NextActionIndex { get; }
    public string Reason { get; }

    public StepEvaluationResult(ConditionOutcome outcome, int nextActionIndex, string reason = null)
    {
        Outcome = outcome;
        NextActionIndex = nextActionIndex;
        Reason = reason;
    }
}