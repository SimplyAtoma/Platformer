using UnityEngine;
using UnityEngine.InputSystem;

public class CameraScroll : MonoBehaviour
{
    public float moveSpeed = 5f;

    public PlayerInputActions inputActions;
    private Vector2 moveInput;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        inputActions.Camera.ScrollRight.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Camera.ScrollRight.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
