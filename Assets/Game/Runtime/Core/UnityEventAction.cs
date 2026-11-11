public sealed class UnityEventAction : ScenarioActionSource
{
    public void RaiseAction() => Emit();
}
