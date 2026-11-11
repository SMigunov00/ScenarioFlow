using UnityEngine;

public sealed class ScenarioAction
{
    public string StepId { get; }
    public string ActionId { get; }
    public string SourceId { get; }

    public ScenarioAction(string stepId, string actionId, string sourceId)
    {
        StepId = stepId;
        ActionId = actionId;
        SourceId = sourceId;
    }
}