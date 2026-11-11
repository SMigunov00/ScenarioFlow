using UnityEngine;

public sealed class TrainingAnalyzer : IResultAnalyzer
{
    public AnalysisResult Analyze(RunJournal journal)
    {
        var result = new AnalysisResult
        {
            AnalyzerName = "Тренировочная оценка"
        };

        if (!JournalReader.TryRead(journal, result, out var steps))
            return result;
        
        result.MaximumScore = steps.Count * 10;

        int total = 0;

        foreach (var step in steps)
        {
            StepInfo stepInfo = step.Value;

            int? points = 0;
            int? successfulAttempt = null;

            if (stepInfo.HasSuccess)
            {
                if (stepInfo.SuccessfulAttemptNumber <= 0)
                {
                    points = null;

                    result.Errors.Add($"У шага {step.Key} отсутствует номер успешной попытки");
                }
                else
                {
                    successfulAttempt = stepInfo.SuccessfulAttemptNumber;

                    points = stepInfo.SuccessfulAttemptNumber == 1 ? 10 : stepInfo.SuccessfulAttemptNumber == 2 ? 5 : 0;
                }
            }
            else if (stepInfo.Status == StepStatus.Completed)
            {
                points = null;
                result.Errors.Add($"Шаг {step.Key} завершён, но запись успешной попытки отсутствует");
            }

            result.Steps.Add(new StepAnalysis
            {
                StepId = step.Key,
                Status = stepInfo.Status,
                SuccessfulAttemptNumber = successfulAttempt,
                Points = points
            });

            total += points ?? 0;
        }

        if (result.Errors.Count == 0)
            result.Score = total;

        return result;
    }
}