using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "ScenarioData", menuName = "ScenarioFlow/Scenario")]
public sealed class ScenarioData : ScriptableObject
{
    [field: SerializeField] public string ScenarioId { get; private set; }
    [field: SerializeField] public string ScenarioName { get; private set; }
    [field: SerializeField, TextArea] public string Description { get; private set; }
    [field: SerializeField] public List<StepData> Steps { get; private set; }
    [field: SerializeField] public List<ActivationBatchData> TrainingPlan { get; private set; } = new();
    [field: SerializeField] public List<ActivationBatchData> ExamPlan { get; private set; } = new();
    [field: SerializeField] public List<ExamGroupData> ExamGroups { get; private set; } = new();

    public IReadOnlyList<ActivationBatchData> GetActivationPlan(RunMode mode)
    {
        switch (mode)
        {
            case RunMode.Training:
                return TrainingPlan;
            case RunMode.Exam:
                return ExamPlan;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}

[Serializable]
public sealed class StepData
{
    [field: Tooltip("Id шага внутри сценария"), 
            SerializeField] public string Id { get; private set; }
    [field: SerializeField] public string Title { get; private set; }
    [field: SerializeField, TextArea] public string Instruction { get; private set; }
}

[Serializable]
public sealed class ActivationBatchData
{
    [field: SerializeField] public string Name { get; private set; }

    [field: Tooltip("Группа шагов которые активируются одновременно"), 
            SerializeField] public List<string> StepIds { get; private set; } = new();
}

[Serializable]
public sealed class ExamGroupData
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public List<string> StepIds { get; private set; } = new();
    [field: SerializeField] public int ViolationLimit { get; private set; }
}
