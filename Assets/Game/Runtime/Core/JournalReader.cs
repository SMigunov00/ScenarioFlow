using System;
using System.Collections.Generic;

internal sealed class StepInfo
{
    public StepStatus Status = StepStatus.Waiting;

    public bool HasSuccess;
    public int SuccessfulAttemptNumber;
}

internal static class JournalReader
{
    public static bool TryRead(RunJournal journal, AnalysisResult result, out Dictionary<string, StepInfo> steps)
    {
        steps = new Dictionary<string, StepInfo>();

        if (journal == null || journal.SchemaVersion != 1 || string.IsNullOrWhiteSpace(journal.RunId))
        {
            result.Errors.Add("Журнал отсутствует или имеет неподдерживаемый формат");
            return false;
        }

        result.RunId = journal.RunId;

        if (journal.StepIds == null || journal.StepIds.Count == 0 || journal.Events == null)
        {
            result.Errors.Add("В журнале отсутствует список шагов или событий");
            return false;
        }

        foreach (string stepId in journal.StepIds)
        {
            if (string.IsNullOrWhiteSpace(stepId) || steps.ContainsKey(stepId))
            {
                result.Errors.Add("Список шагов содержит пустой или повторяющийся Id");
                return false;
            }

            steps.Add(stepId, new StepInfo());
        }
        
        foreach (RunEvent runEvent in journal.Events)
        {
            if (runEvent == null)
            {
                result.Warnings.Add("Пустая запись пропущена");
                continue;
            }

            if (!string.IsNullOrEmpty(runEvent.RunId) && runEvent.RunId != journal.RunId)
            {
                result.Warnings.Add("Запись другого прохождения пропущена");
                continue;
            }

            if (runEvent.Type == RunEventTypes.RunFinished)
            {
                if (runEvent.Result == "Finished")
                    result.IsFinal = true;

                continue;
            }

            bool relevant = runEvent.Type == RunEventTypes.StepActivated || 
                            runEvent.Type == RunEventTypes.StepStatusChanged || 
                            runEvent.Type == RunEventTypes.AttemptResolved;
            
            if (!relevant) continue;

            if (string.IsNullOrEmpty(runEvent.StepId) || !steps.TryGetValue(runEvent.StepId, out StepInfo stepInfo))
            {
                result.Errors.Add($"Запись относится к неизвестному шагу: {runEvent.StepId}.");
                continue;
            }

            switch (runEvent.Type)
            {
                case RunEventTypes.StepActivated:
                    stepInfo.Status = StepStatus.Active;
                    break;

                case RunEventTypes.StepStatusChanged:
                    if (Enum.TryParse(runEvent.Result, out StepStatus status) && Enum.IsDefined(typeof(StepStatus), status))
                    {
                        stepInfo.Status = status;
                    }
                    else
                    {
                        result.Warnings.Add($"Неизвестный статус шага {runEvent.StepId}: {runEvent.Result}");
                    }
                    
                    break;

                case RunEventTypes.AttemptResolved:
                    if (runEvent.Result == "Success")
                    {
                        if (!stepInfo.HasSuccess)
                        {
                            stepInfo.HasSuccess = true;
                            stepInfo.SuccessfulAttemptNumber = runEvent.AttemptNumber;
                        }
                        else
                        {
                            result.Warnings.Add($"Повторный успех шага {runEvent.StepId} не учитывается повторно");
                        }
                        
                        stepInfo.Status = StepStatus.Completed;
                    }

                    break;
            }
        }

        foreach (var pair in steps)
        {
            StepInfo stepInfo = pair.Value;

            bool terminal = stepInfo.Status == StepStatus.Completed ||
                            stepInfo.Status == StepStatus.Failed ||
                            stepInfo.Status == StepStatus.Skipped;

            if (result.IsFinal && !terminal)
                result.Errors.Add($"У шага {pair.Key} отсутствует конечный статус");
            

            if (stepInfo.HasSuccess && stepInfo.Status != StepStatus.Completed)
                result.Errors.Add($"Противоречивый результат шага {pair.Key}");
            
        }

        if (!result.IsFinal)
            result.Warnings.Add("Промежуточная оценка: прохождение ещё не завершено");
        
        return result.Errors.Count == 0;
    }
}