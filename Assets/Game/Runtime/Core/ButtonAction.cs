using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ButtonAction : ScenarioActionSource
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(Emit);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(Emit);
    }
}
