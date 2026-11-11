using UnityEngine;

public sealed class ScenarioStateFlag : MonoBehaviour
{
    [field: SerializeField] public string StateId {get; private set; }
    [field: SerializeField] public bool Value {get; private set; }
    
    public void SetValue(bool value) => Value = value;
}