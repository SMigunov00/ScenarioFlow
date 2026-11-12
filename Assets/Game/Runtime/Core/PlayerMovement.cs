using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;
    [SerializeField] private ScenarioRunner _runner;

    private Rigidbody _body;
    private Vector2 _moveInput;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
    }

    private void FixedUpdate()
    {
        if (_body.isKinematic) return;

        Vector2 input = _moveInput;
        
        if (_runner != null && !_runner.IsRunning)
            input = Vector2.zero;

        Vector3 velocity = _body.linearVelocity;

        velocity.x = input.x * _moveSpeed;
        velocity.z = input.y * _moveSpeed;
        
        _body.linearVelocity = velocity;
    }

    private void OnDisable()
    {
        _moveInput = Vector2.zero;
        if (_body == null || _body.isKinematic) return;

        Vector3 velocity = _body.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        _body.linearVelocity = velocity;
    }
}