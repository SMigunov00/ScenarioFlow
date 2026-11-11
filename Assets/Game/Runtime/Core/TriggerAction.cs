using UnityEngine;

public sealed class TriggerAction : ScenarioActionSource
{
    [SerializeField] private Rigidbody _expectedTarget;

    private void OnTriggerEnter(Collider other)
    {
        if (_expectedTarget != null && other.attachedRigidbody != _expectedTarget) return;
        
        Emit();
    }
}