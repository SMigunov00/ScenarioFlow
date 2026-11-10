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

[Serializable]
public sealed class RunSettings
{
    [field: SerializeField] public RunMode Mode { get; private set; } =  RunMode.Training;
    [field: SerializeField] public LoggerType LoggerType { get; private set; } = LoggerType.Console;
    [field: SerializeField] public AnalyzerType AnalyzerType { get; private set; } = AnalyzerType.TrainigScore;

    public RunSettings CreateCopy()
    {
        return new RunSettings
        {
            Mode = this.Mode,
            LoggerType = this.LoggerType,
            AnalyzerType = this.AnalyzerType,
        };
    }
    
    internal void SetAnalyzer(AnalyzerType analyzerType) =>  AnalyzerType = analyzerType;
}