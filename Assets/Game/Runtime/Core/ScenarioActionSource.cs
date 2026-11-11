using System;
using UnityEngine;

public abstract class ScenarioActionSource : MonoBehaviour
{
    [SerializeField] private string _stepId;
    [SerializeField] private string _actionId;
    [SerializeField] private string _sourceId;

    public event Action<ScenarioAction> ActionRaised;

    protected void Emit()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;

        if (string.IsNullOrWhiteSpace(_stepId) || string.IsNullOrWhiteSpace(_actionId))
        {
            Debug.LogError($"У источника не заполнены StepId или ActionId {this}");
            return;
        }

        string resolvedSourceId = string.IsNullOrWhiteSpace(_sourceId) ? gameObject.name : _sourceId;
        var action = new ScenarioAction(_stepId, _actionId, resolvedSourceId);
        ActionRaised?.Invoke(action);
    }
}

