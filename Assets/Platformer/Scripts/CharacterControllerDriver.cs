using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerDriver : MonoBehaviour
{
    [Header("Ground Config")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float groundAcceleration = 30f;   // units/sec^2 feel (higher = snappier)
    public float groundDeceleration = 40f;

    [Header("Air Config")]
    public float airAcceleration = 15f;
    public float airDeceleration = 5f;

    [Header("Jump")]
    public float apexHeight = 3f;   // height above takeoff point
    public float apexTime = 0.35f;  // time to reach the top
    public float fallGravityMultiplier = 2f; // faster fall when jump released

    [Header("Grounding")]
    public float groundedStickY = -2f;

    [Header("Head Hit / Blocks")]
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float headCastExtra = 0.2f;
    [SerializeField] private float headRadiusScale = 0.9f;

    private CharacterController _controller;
    Animator _animator;
    private float _xVelocity;
    private float _yVelocity;

    private Quaternion _facingRight;
    private Quaternion _facingLeft;

    private float _gravity;     // negative
    private float _jumpVelocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _facingRight = Quaternion.Euler(0f, 90f, 0f);
        _facingLeft = Quaternion.Euler(0f, -90f, 0f);

        // Kinematics: apexTime = time to apex (upward phase)
        _gravity = -2f * apexHeight / (apexTime * apexTime);
        _jumpVelocity = 2f * apexHeight / apexTime;
    }

    void Update()
    {
        // Input
        float direction = 0f;
        if (Keyboard.current.aKey.isPressed) direction -= 1f;
        if (Keyboard.current.dKey.isPressed) direction += 1f;

        bool jumpPressedThisFrame = Keyboard.current.spaceKey.wasPressedThisFrame;
        bool jumpHeld = Keyboard.current.spaceKey.isPressed;
        bool runHeld = Keyboard.current.leftShiftKey.isPressed;

        // Facing
        if (direction > 0f) transform.rotation = _facingRight;
        else if (direction < 0f) transform.rotation = _facingLeft;

        // Horizontal target speed
        float maxSpeed = runHeld ? runSpeed : walkSpeed;
        float targetX = direction * maxSpeed;

        bool grounded = _controller.isGrounded;

        // Horizontal accel/decel (ground vs air)
        float accel = grounded
            ? (Mathf.Abs(targetX) > Mathf.Abs(_xVelocity) ? groundAcceleration : groundDeceleration)
            : (Mathf.Abs(targetX) > Mathf.Abs(_xVelocity) ? airAcceleration : airDeceleration);

        _xVelocity = Mathf.MoveTowards(_xVelocity, targetX, accel * Time.deltaTime);

        // Jump
        if (grounded)
        {
            if (jumpPressedThisFrame && grounded){ 
                _yVelocity = _jumpVelocity;
                _animator.SetTrigger("Jump");
                }
            else if (_yVelocity < 0f)
                _yVelocity = groundedStickY;
        }

        // Gravity (always)
        float g = _gravity;

        // If rising but jump not held -> stronger gravity for short hop
        if (_yVelocity > 0f && !jumpHeld)
            g *= fallGravityMultiplier;

        _yVelocity += g * Time.deltaTime;

        // Move
        Vector3 delta = new Vector3(_xVelocity, _yVelocity, 0f) * Time.deltaTime;
        CollisionFlags flags = _controller.Move(delta);

        // Ceiling bonk
        if ((flags & CollisionFlags.Above) != 0 && _yVelocity > 0f)
        {
            HitBlockAbove();
            _yVelocity = 0f;
        }
        // Wall hit
        if ((flags & CollisionFlags.Sides) != 0)
            _xVelocity = 0f;
        _animator.SetFloat("Speed", Mathf.Abs(_xVelocity));
        _animator.SetBool("Grounded", _controller.isGrounded);
        _animator.SetFloat("YVelocity", _yVelocity);
    }

    private void HitBlockAbove()
    {
        // Controller capsule top (world)
        Vector3 centerWorld = transform.TransformPoint(_controller.center);
        float radius = _controller.radius * headRadiusScale;

        // top of the capsule sphere center
        Vector3 topSphereCenter = centerWorld + Vector3.up * ((_controller.height * 0.5f) - _controller.radius);

        // start a hair below to avoid starting overlapped
        Vector3 origin = topSphereCenter - Vector3.up * 0.02f;

        float castDist = headCastExtra + 0.05f;

        if (Physics.SphereCast(origin, radius, Vector3.up, out RaycastHit hit, castDist, hitMask, QueryTriggerInteraction.Ignore))
        {
            GameObject go = hit.collider.gameObject;

            if (go.CompareTag("Brick"))
            {
                var breakable = go.GetComponent<BreakableBrickScripted>();
                if (breakable != null) breakable.Break();
                else Destroy(go);
            }
            else if (go.CompareTag("Question"))
            {
                CoinUI.Instance?.AddCoins(1);
                go.GetComponent<AnimationScript>()?.PopCoin();
            }
        }
    }
}