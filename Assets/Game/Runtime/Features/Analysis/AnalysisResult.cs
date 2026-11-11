using System.Collections.Generic;

public interface IResultAnalyzer
{
    AnalysisResult Analyze(RunJournal journal);
}

public sealed class AnalysisResult
{
    public string AnalyzerName;
    public string RunId;
    public bool IsFinal;
    public int? Score;
    public int MaximumScore;

    public List<StepAnalysis> Steps = new();
    public List<string> Warnings = new();
    public List<string> Errors = new ();
}

public sealed class StepAnalysis
{
    public string StepId;
    public StepStatus Status;
    public int? SuccessfulAttemptNumber;
    public int? Points;
    public int? Penalty;
}
