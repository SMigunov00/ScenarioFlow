using System.Collections.Generic;
using UnityEngine;

public interface IConditionContext
{
    bool TryGetBool(string stateId, out bool value);
}

public sealed class ScenarioContext : MonoBehaviour, IConditionContext
{
    [SerializeField] private List<ScenarioStateFlag> _flags = new();

    public bool TryGetBool(string stateId, out bool value)
    {
        value = false;
        bool found = false;

        foreach (ScenarioStateFlag flag in _flags)
        {
            if (flag == null || flag.StateId != stateId)
                continue;
            
            if (found)
                return false;

            found = true;
            value = flag.Value;
        }

        return found;
    }
}
