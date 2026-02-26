using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerDriver : MonoBehaviour
{
    [Header("Ground Config")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float groundAcceleration = 30f;
    public float groundDeceleration = 40f;

    [Header("Air Config")]
    public float airAcceleration = 15f;
    public float airDeceleration = 5f;

    [Header("Jump Core")]
    public float apexHeight = 3f;      // height above takeoff
    public float apexTime = 0.35f;     // time to reach apex
    public float fallGravityMultiplier = 2f; // stronger gravity when jump released early

    [Header("Jump Buffer + Coyote")]
    public float jumpBufferTime = 0.12f; // seconds
    public float coyoteTime = 0.10f;     // seconds

    [Header("Apex Modifiers")]
    [Tooltip("Near apex (low |yVel|), gravity is multiplied by this ( < 1 = hang time ).")]
    public float apexGravityMultiplier = 0.6f;
    [Tooltip("Consider 'near apex' when |yVel| <= this.")]
    public float apexVelThreshold = 1.0f;
    [Tooltip("Optional: boost air accel near apex for nicer steering.")]
    public float apexAirAccelMultiplier = 1.15f;

    [Header("Grounding")]
    public float groundedStickY = -2f;

    [Header("Head Hit / Blocks")]
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float headCastExtra = 0.2f;
    [SerializeField] private float headRadiusScale = 0.9f;

    private CharacterController _controller;
    private Animator _animator;

    private float _xVelocity;
    private float _yVelocity;

    private Quaternion _facingRight;
    private Quaternion _facingLeft;

    private float _gravity;       // negative
    private float _jumpVelocity;  // positive

    // Timers
    private float _jumpBufferTimer;  // counts down
    private float _coyoteTimer;      // counts down

    // Animator hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int YVelHash = Animator.StringToHash("YVelocity");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        _facingRight = Quaternion.Euler(0f, 90f, 0f);
        _facingLeft  = Quaternion.Euler(0f, -90f, 0f);

        apexTime = Mathf.Max(0.01f, apexTime);

        // Kinematics: apexTime = time to apex
        _gravity = -2f * apexHeight / (apexTime * apexTime);
        _jumpVelocity = 2f * apexHeight / apexTime;
    }

    void Update()
    {
        // --- Input ---
        float direction = 0f;
        if (Keyboard.current.aKey.isPressed) direction -= 1f;
        if (Keyboard.current.dKey.isPressed) direction += 1f;

        bool jumpPressedThisFrame = Keyboard.current.spaceKey.wasPressedThisFrame;
        bool jumpHeld = Keyboard.current.spaceKey.isPressed;
        bool runHeld = Keyboard.current.leftShiftKey.isPressed;

        // Facing
        if (direction > 0f) transform.rotation = _facingRight;
        else if (direction < 0f) transform.rotation = _facingLeft;

        // --- Grounded / timers ---
        bool grounded = _controller.isGrounded;

        // Jump buffer: when pressed, load the buffer timer
        if (jumpPressedThisFrame)
            _jumpBufferTimer = jumpBufferTime;
        else
            _jumpBufferTimer -= Time.deltaTime;

        // Coyote: refresh when grounded, otherwise count down
        if (grounded)
            _coyoteTimer = coyoteTime;
        else
            _coyoteTimer -= Time.deltaTime;

        // --- Horizontal movement ---
        float maxSpeed = runHeld ? runSpeed : walkSpeed;
        float targetX = direction * maxSpeed;

        bool nearApex = Mathf.Abs(_yVelocity) <= apexVelThreshold && !grounded;
        float airAccelBoost = nearApex ? apexAirAccelMultiplier : 1f;

        float accel = grounded
            ? (Mathf.Abs(targetX) > Mathf.Abs(_xVelocity) ? groundAcceleration : groundDeceleration)
            : (Mathf.Abs(targetX) > Mathf.Abs(_xVelocity) ? airAcceleration * airAccelBoost : airDeceleration);

        _xVelocity = Mathf.MoveTowards(_xVelocity, targetX, accel * Time.deltaTime);

        // --- Jump attempt (buffer + coyote) ---
        bool bufferedJump = _jumpBufferTimer > 0f;
        bool canCoyoteJump = _coyoteTimer > 0f;

        if (bufferedJump && canCoyoteJump)
        {
            DoJump();
            _jumpBufferTimer = 0f; // consume buffer
            _coyoteTimer = 0f;     // consume coyote (prevents double-jumps)
            grounded = false;
        }

        // --- Ground stick ---
        if (grounded && _yVelocity < 0f)
            _yVelocity = groundedStickY;

        // --- Gravity with apex modifiers ---
        float g = _gravity;

        // Short hop: if rising but jump not held, fall faster
        if (_yVelocity > 0f && !jumpHeld)
            g *= fallGravityMultiplier;

        // Apex hang: reduce gravity near apex (only if not grounded)
        if (!grounded && Mathf.Abs(_yVelocity) <= apexVelThreshold)
            g *= apexGravityMultiplier;

        _yVelocity += g * Time.deltaTime;

        // --- Move ---
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

        // --- Animator ---
        if (AnimatorReady())
        {
            _animator.SetFloat(SpeedHash, Mathf.Abs(_xVelocity));
            _animator.SetBool(GroundedHash, _controller.isGrounded);
            _animator.SetFloat(YVelHash, _yVelocity);
        }
    }

    private void DoJump()
    {
        _yVelocity = _jumpVelocity;
        if (AnimatorReady()) _animator.SetTrigger(JumpHash);
    }

    private bool AnimatorReady()
        => _animator != null && _animator.runtimeAnimatorController != null;

    private void HitBlockAbove()
    {
        Vector3 centerWorld = transform.TransformPoint(_controller.center);
        float radius = _controller.radius * headRadiusScale;

        Vector3 topSphereCenter = centerWorld + Vector3.up * ((_controller.height * 0.5f) - _controller.radius);
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