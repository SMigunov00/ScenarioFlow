using System;
using System.Collections.Generic;

public enum RunStatus
{
    Running,
    Finished
}

public sealed class RunState
{
    private readonly Dictionary<string, StepState> steps = new ();

    public string RunId { get; }

    public string ScenarioId { get; }

    public RunStatus Status { get; private set; }

    public IReadOnlyDictionary<string, StepState> Steps => steps;

    public RunState(ScenarioData scenario)
    {
        if (scenario == null)
            throw new ArgumentNullException($"Нет сценария {nameof(scenario)}");
        

        RunId = Guid.NewGuid().ToString("N");
        ScenarioId = scenario.ScenarioId;

        foreach (StepData stepData in scenario.Steps)
        {
            if (stepData == null)
                throw new ArgumentException("В сценарии обнаружен пустой шаг");
            
            if (string.IsNullOrWhiteSpace(stepData.Id))
                throw new ArgumentException("У одного из шагов отсутствует Id");
            
            if (steps.ContainsKey(stepData.Id))
                throw new ArgumentException($"Повторяющийся ID шага: {stepData.Id}");
            
            steps.Add(stepData.Id, new StepState(stepData.Id));
        }

        Status = RunStatus.Running;
    }

    public bool TryGetStep(string stepId, out StepState stepState) => steps.TryGetValue(stepId, out stepState);
    
    public void Finish() => Status = RunStatus.Finished;
    
}