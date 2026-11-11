using System;

public sealed class ExamAnalyzer : IResultAnalyzer
{
    public AnalysisResult Analyze(RunJournal journal)
    {
        var result = new AnalysisResult
        {
            AnalyzerName = "Экзаменационная оценка",
            MaximumScore = 100
        };

        if (!JournalReader.TryRead(journal, result, out var steps))
            return result;
        
        int totalPenalty = 0;

        foreach (var pair in steps)
        {
            StepInfo stepInfo = pair.Value;

            int penalty = stepInfo.Status == StepStatus.Failed ? 10 : stepInfo.Status == StepStatus.Skipped ? 5 : 0;

            totalPenalty += penalty;

            result.Steps.Add(new StepAnalysis
            {
                StepId = pair.Key,
                Status = stepInfo.Status,
                Penalty = penalty
            });
        }

        result.Score = Math.Max(0, 100 - totalPenalty);

        return result;
    }
}