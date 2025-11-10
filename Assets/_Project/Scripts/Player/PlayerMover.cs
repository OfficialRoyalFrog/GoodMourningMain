using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    public enum MoveStyle { ArcadeInstant, SmoothAccel }

    [Header("Movement Mode")]
    public MoveStyle moveStyle = MoveStyle.ArcadeInstant;
    [Tooltip("Move relative to camera forward/right on XZ. Off = world XZ.")]
    public bool cameraRelative = false;

    [Header("Speeds")]
    [Min(0f)] public float moveSpeed = 6f;
    [Min(0f)] public float acceleration = 12f;   // used in SmoothAccel
    [Min(0f)] public float deceleration = 16f;   // used in SmoothAccel
    [Tooltip("Multiplier when reversing direction (helps snappy turn).")]
    [Range(1f, 4f)] public float turnBoost = 2f; // used in SmoothAccel

    [Header("Input")]
    [Range(0f, 0.5f)] public float inputDeadzone = 0.1f;
    public bool normalizeDiagonal = true;
    [Tooltip("If true and input < deadzone, we zero horizontal velocity immediately.")]
    public bool snapStopOnRelease = true;

    [Header("Vertical (optional)")]
    public bool useGravity = false;            // leave off for top-down 2D
    public float gravity = -20f;
    public float stickToGround = -2f;

    [Header("Sprite Facing")]
    [Tooltip("Assign your child SpriteRenderer Transform here (the visual).")]
    public Transform visual;
    [Tooltip("Flip using localScale X sign. If off and a SpriteRenderer exists, we flipX instead.")]
    public bool flipUsingScale = true;
    [Tooltip("Is the default art facing LEFT? If false, we assume default faces RIGHT.")]
    public bool defaultFacingLeft = true;
    [Range(0f, 0.3f)] public float flipThreshold = 0.05f;
    public Vector3 leftScale = new Vector3(1, 1, 1);
    public Vector3 rightScale = new Vector3(-1, 1, 1);

    // ---- runtime
    private CharacterController controller;
    private Transform cam;
    private Vector2 moveInput;
    private Vector3 velocity; // used only in SmoothAccel or when gravity on
    private SpriteRenderer visualSR;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main ? Camera.main.transform : null;

        if (visual != null)
            visualSR = visual.GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // 1) Read input -> desired world direction (XZ)
        Vector2 in2 = (moveInput.magnitude < inputDeadzone) ? Vector2.zero : moveInput;

        Vector3 dirWorld;
        if (cameraRelative && cam != null)
        {
            Vector3 f = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 r = Vector3.ProjectOnPlane(cam.right,   Vector3.up).normalized;
            dirWorld = (f * in2.y + r * in2.x);
        }
        else
        {
            dirWorld = new Vector3(in2.x, 0f, in2.y);
        }

        if (normalizeDiagonal && dirWorld.sqrMagnitude > 1f) dirWorld.Normalize();
        Vector3 desiredVel = dirWorld * moveSpeed;

        // 2) Horizontal motion
        Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);

        if (moveStyle == MoveStyle.ArcadeInstant)
        {
            // instant start/stop (tight feel)
            horizVel = snapStopOnRelease && in2 == Vector2.zero ? Vector3.zero : desiredVel;
        }
        else // SmoothAccel
        {
            float accelNow = (desiredVel.sqrMagnitude > 0.0001f) ? acceleration : deceleration;

            // boost when reversing direction for snappier turns
            if (Vector3.Dot(horizVel, desiredVel) < 0f) accelNow *= turnBoost;

            horizVel = Vector3.MoveTowards(horizVel, desiredVel, accelNow * Time.deltaTime);

            if (snapStopOnRelease && in2 == Vector2.zero && horizVel.sqrMagnitude < 0.001f)
                horizVel = Vector3.zero;
        }

        // 3) Vertical (optional)
        float yVel = velocity.y;
        if (useGravity)
        {
            yVel += gravity * Time.deltaTime;
            if (controller.isGrounded && yVel < stickToGround) yVel = stickToGround;
        }
        else
        {
            yVel = 0f;
        }

        velocity = new Vector3(horizVel.x, yVel, horizVel.z);

        // 4) Move
        controller.Move(velocity * Time.deltaTime);

        // 5) Flip visual
        HandleFlip(in2.x);
    }

    void HandleFlip(float xInput)
    {
        if (visual == null) return;

        if (Mathf.Abs(xInput) < flipThreshold) return;

        bool goingRight = xInput > 0f;

        if (flipUsingScale)
        {
            if (defaultFacingLeft)
                visual.localScale = goingRight ? rightScale : leftScale;
            else
                visual.localScale = goingRight ? leftScale : rightScale; // if your art faces right by default
        }
        else if (visualSR != null)
        {
            // flipX=true means mirror horizontally
            bool flipX = defaultFacingLeft ? goingRight : !goingRight;
            visualSR.flipX = flipX;
        }
    }

    // Input System (PlayerInput → Invoke Unity Events → bind to OnMove)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
}