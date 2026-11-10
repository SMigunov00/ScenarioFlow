using System;
using UnityEngine;

[Serializable]
public sealed class RunEvent
{
    public int Sequence;
    public string Time;
    public string RunId;
    public string StepId;
    public string Type;
    public string Result;
    public int AttemptNumber;
    public string Reason;
}

public static class RunEventTypes
{
    public const string RunStarted = "RunStarted";
    public const string RunFinished = "RunFinished";
    public const string StepActivated = "StepActivated";
    public const string StepProgressed = "StepProgressed";
    public const string AttemptResolved = "AttemptResolved";
    public const string StepStatusChanged = "StepStatusChanged";
}

