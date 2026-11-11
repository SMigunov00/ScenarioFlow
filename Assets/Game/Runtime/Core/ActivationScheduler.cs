using System;
using System.Collections.Generic;

public sealed class ActivationScheduler
{
    private readonly List<string[]> batches = new();

    public ActivationScheduler(IReadOnlyList<ActivationBatchData> activationBatchData, RunState runState)
    {
        if (activationBatchData == null || activationBatchData.Count == 0)
            throw new ArgumentException("План активации пуст");
        
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        
        for (int i = 0; i < activationBatchData.Count; i++)
        {
            ActivationBatchData batchData = activationBatchData[i];

            if (batchData == null || batchData.StepIds == null || batchData.StepIds.Count == 0)
                throw new ArgumentException($"Пакет данных {i + 1} пуст");
            

            foreach (string stepId in batchData.StepIds)
            {
                if (string.IsNullOrWhiteSpace(stepId) || !runState.Steps.ContainsKey(stepId))
                    throw new ArgumentException($"Пакет данных {i + 1} содержит неизвестный шаг: {stepId}");
                
                if (!usedIds.Add(stepId))
                    throw new ArgumentException($"Шаг {stepId} указан в плане несколько раз");
            }

            batches.Add(batchData.StepIds.ToArray());
        }

        if (usedIds.Count != runState.Steps.Count)
            throw new ArgumentException("План должен содержать каждый шаг только один раз");
    }

    public IReadOnlyList<StepState> GetStepsToActivate(RunState runState)
    {
        var result = new List<StepState>();

        foreach (string[] batch in batches)
        {
            bool batchFinished = true;

            foreach (string stepId in batch)
            {
                if (!runState.Steps[stepId].IsTerminal)
                {
                    batchFinished = false;
                    break;
                }
            }

            if (batchFinished) continue;

            foreach (string stepId in batch)
            {
                StepState step = runState.Steps[stepId];

                if (step.Status == StepStatus.Waiting)
                    result.Add(step);
            }
            
            return result;
        }

        return result;
    }
}