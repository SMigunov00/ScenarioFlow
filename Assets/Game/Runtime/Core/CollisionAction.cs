using UnityEngine;

public sealed class CollisionAction : ScenarioActionSource
{
    [SerializeField] private Rigidbody _expectedTarget;

    private void OnCollisionEnter(Collision collision)
    {
        if (_expectedTarget != null && collision.rigidbody != _expectedTarget) return;

        Emit();
    }
}
