using UnityEngine;
using UnityEngine.Events;

public sealed class TestUnityEventEmitter : MonoBehaviour
{
    [SerializeField] private UnityEvent onAction = new();

    [ContextMenu("Тест/Вызвать UnityEvent")]
    public void InvokeAction()
    {
        if (Application.isPlaying)
            onAction.Invoke();
    }
}
