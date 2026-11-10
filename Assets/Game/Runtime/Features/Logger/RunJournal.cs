using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public sealed class RunJournal
{
    [field: SerializeField] public int SchemaVersion { get; private set; } = 1;

    [field: SerializeField] public string RunId { get; private set; }
    [field: SerializeField] public string ScenarioId { get; private set; }
    [field: SerializeField] public RunMode Mode { get; private set; }
    
    [field: SerializeField] private List<string> stepIds = new();
    [field: SerializeField] private List<RunEvent> events = new();
    
    public IReadOnlyList<string> StepIds => stepIds;
    public IReadOnlyList<RunEvent> Events => events;

    public RunJournal(RunState run, RunSettings settings)
    {
        RunId = run.RunId;
        ScenarioId = run.ScenarioId;
        Mode = settings.Mode;
        stepIds.AddRange(run.Steps.Keys);
    }

    public RunEvent Append(string type, string stepId = null, string result = null, int attemptNumber = 0, string reason = null)
    {
        var runEvent = new RunEvent
        {
            Sequence = events.Count + 1,
            Time = DateTime.UtcNow.ToString("O"),
            RunId = RunId,
            StepId = stepId,
            Type = type,
            Result = result,
            AttemptNumber = attemptNumber,
            Reason = reason
        };
        events.Add(runEvent);
        return runEvent;
    }
}