using System;
using System.Collections.Generic;

[Serializable]
public sealed class StepConditionData
{
    public List<string> ActionSequence = new List<string>();

    public bool UnexpectedActionIsViolation = true;

    public bool RequireState;
    public string RequiredStateId;
    public bool RequiredStateValue = true;
}