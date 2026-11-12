using UnityEngine;

public class ObjectResetter : MonoBehaviour
{
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private Rigidbody _body;
    private ScenarioStateFlag _flag;

    private bool _initialFlagValue;
    private bool _initialKinematic;
    private bool _initialUseGravity;

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        _body = GetComponent<Rigidbody>();
        _flag = GetComponent<ScenarioStateFlag>();

        if (_body != null)
        {
            _initialKinematic = _body.isKinematic;
            _initialUseGravity = _body.useGravity;
        }

        if (_flag != null)
            _initialFlagValue = _flag.Value;
    }

    public void ResetObject()
    {
        if (_body != null)
        {
            _body.isKinematic = _initialKinematic;
            _body.useGravity = _initialUseGravity;

            _body.position = _initialPosition;
            _body.rotation = _initialRotation;

            if (!_body.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                _body.linearVelocity = Vector3.zero;
#else
                body.velocity = Vector3.zero;
#endif
                _body.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            transform.SetPositionAndRotation(_initialPosition, _initialRotation);
        }

        if (_flag != null)
            _flag.SetValue(_initialFlagValue);

        Physics.SyncTransforms();
    }
}
