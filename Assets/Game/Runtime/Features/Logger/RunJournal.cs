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

    public RunJournal(RunState state, RunSettings settings)
    {
        RunId = state.RunId;
        ScenarioId = state.ScenarioId;
        Mode = settings.Mode;
        stepIds.AddRange(state.Steps.Keys);
    }

    public RunEvent Append(RunEvent runEvent)
    {
        if (runEvent == null)
            throw new ArgumentNullException(nameof(runEvent));

        runEvent.Sequence = events.Count + 1;
        runEvent.Time = DateTime.UtcNow.ToString("O");
        runEvent.RunId = RunId;

        events.Add(runEvent);

        return runEvent;
    }
    
    public RunJournal CreateJournal()
    {
        string json = JsonUtility.ToJson(this);
        return JsonUtility.FromJson<RunJournal>(json);
    }
}