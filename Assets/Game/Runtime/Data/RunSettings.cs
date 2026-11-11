using System;
using UnityEngine;

public enum RunMode
{
    Training,
    Exam
}

public enum LoggerType
{
    Console,
    Json
}

public enum AnalyzerType
{
    TrainigScore,
    ExamScore
}

public enum OutOfOrderBehavior
{
    RecordViolation,
    IgnoreAction
}

[Serializable]
public sealed class RunSettings
{
    [field: SerializeField] public RunMode Mode { get; private set; } =  RunMode.Training;
    [field: SerializeField] public LoggerType LoggerType { get; private set; } = LoggerType.Console;
    [field: SerializeField] public AnalyzerType AnalyzerType { get; private set; } = AnalyzerType.TrainigScore;
    [field: SerializeField] public OutOfOrderBehavior OutOfOrderBehavior { get; private set; } = OutOfOrderBehavior.RecordViolation;

    public RunSettings CreateCopy()
    {
        return new RunSettings
        {
            Mode = this.Mode,
            LoggerType = this.LoggerType,
            AnalyzerType = this.AnalyzerType,
            OutOfOrderBehavior = this.OutOfOrderBehavior
        };
    }
    
    internal void SetAnalyzer(AnalyzerType analyzerType) =>  AnalyzerType = analyzerType;
}