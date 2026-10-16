using UnityEngine;
using UnityEngine.InputSystem;

public class DemoPlayer : MonoBehaviour
{
    private const float GroundProbeDistance = 0.08f;
    private const float MinimumGroundNormal = 0.5f;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;

    [Space]
    [Header("Movement")]
    [SerializeField] private DemoSettings settings;
    [SerializeField] private LayerMask groundMask;

    [Space]
    [Header("Automatic movement")]
    [SerializeField] private bool automatic;
    [Tooltip("Ball speed is used only in automatic mode.")]
    [SerializeField] private BridgeSettings bridgeSettings;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

    private bool jumpRequested;
    private bool stopped;

    public Rigidbody2D Body => body;
    public bool Grounded { get; private set; }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        if (stopped)
            return;

        UpdateGrounded();

        Vector2 velocity = body.linearVelocity;
        if (automatic)
            velocity.x = bridgeSettings.BallSpeed;
        else
            velocity = ApplyManualMovement(velocity);

        jumpRequested = false;
        body.linearVelocity = velocity;
    }

    private void UpdateGrounded()
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
        int hitCount = bodyCollider.Cast(Vector2.down, filter, groundHits, GroundProbeDistance);
        Grounded = false;

        for (int i = 0; i < hitCount; i++)
        {
            if (groundHits[i].normal.y > MinimumGroundNormal &&
                groundHits[i].point.y <= bodyCollider.bounds.min.y + GroundProbeDistance)
            {
                Grounded = true;
                break;
            }
        }
    }

    private Vector2 ApplyManualMovement(Vector2 velocity)
    {
        float axis = ReadHorizontalInput();
        float acceleration = Grounded ? settings.GroundAcceleration : settings.AirAcceleration;

        if (Grounded || Mathf.Abs(axis) > 0f)
            velocity.x = Mathf.MoveTowards(velocity.x, axis * settings.MoveSpeed, acceleration * Time.fixedDeltaTime);

        if (jumpRequested && Grounded)
            velocity.y = settings.JumpSpeed;

        return velocity;
    }

    private static float ReadHorizontalInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return 0f;

        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;

        return (right ? 1f : 0f) - (left ? 1f : 0f);
    }

    public void Stop()
    {
        stopped = true;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
    }
}
