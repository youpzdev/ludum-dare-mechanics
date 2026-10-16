using System;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class DemoPlayerVisuals : MonoBehaviour
{
    [SerializeField] private DemoPlayer player;
    [SerializeField] private DemoFlow flow;
    [SerializeField] private Transform visual;
    [SerializeField] private TrailRenderer speedTrail;
    [SerializeField] private DemoCameraShake cameraShake;
    [SerializeField] private ParticleSystem landingBurst;
    [SerializeField] private Collider2D contactCollider;
    [SerializeField] [Range(1, 24)] private int landingParticleCount = 10;

    [Header("Motion")]
    [SerializeField] [Min(0.01f)] private float fullEffectSpeed = 10f;
    [SerializeField] [Range(0f, 0.4f)] private float airStretch = 0.13f;
    [SerializeField] [Range(0f, 25f)] private float maximumLean = 9f;
    [SerializeField] [Min(0.01f)] private float response = 14f;

    [Header("Contacts")]
    [SerializeField] [Min(0.01f)] private float minimumImpactSpeed = 2f;
    [SerializeField] [Min(0.01f)] private float fullImpactSpeed = 10f;
    [SerializeField] [Range(0f, 0.4f)] private float landingSquash = 0.23f;
    [SerializeField] [Range(0f, 0.4f)] private float jumpStretch = 0.17f;
    [SerializeField] [Min(0.01f)] private float impactDuration = 0.24f;
    [SerializeField] [Range(0f, 1f)] private float landingShake = 0.45f;

    [Header("Trail")]
    [SerializeField] [Min(0f)] private float trailStartSpeed = 7f;
    [SerializeField] [Min(0f)] private float trailStopSpeed = 5.5f;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
    private Rigidbody2D body;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector2 previousVelocity;

    private bool previousGrounded;
    private bool hasState;
    private bool initialized;
    private bool focused = true;
    private bool paused;

    private int previousContactSides;

    private float fallingSpeed;
    private float impact;
    private float impactRemaining;
    private float currentStretch;
    private float currentLean;

    public event Action<float> Landed;
    public event Action<float> Jumped;

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        focused = Application.isFocused;
        hasState = false;
    }

    private void FixedUpdate()
    {
        if (!CanAnimate())
        {
            Clear();
            return;
        }
        Vector2 velocity = body.linearVelocity;
        bool grounded = player.Grounded;
        UpdateContactImpacts();
        if (hasState)
        {
            if (grounded && !previousGrounded)
            {
                float speed = Mathf.Max(fallingSpeed, -previousVelocity.y);
                if (speed >= minimumImpactSpeed)
                {
                    float strength = Mathf.Clamp01(speed / fullImpactSpeed);
                    SetImpact(-landingSquash * strength);
                    if (cameraShake != null && landingShake > 0f) cameraShake.Impulse(strength * landingShake);
                    EmitLanding(strength);
                    Landed?.Invoke(strength);
                }
                fallingSpeed = 0f;
            }
            if ((grounded || previousGrounded) && velocity.y >= minimumImpactSpeed &&
                previousVelocity.y < minimumImpactSpeed)
            {
                float strength = Mathf.Clamp01(velocity.y / fullImpactSpeed);
                SetImpact(jumpStretch * strength);
                Jumped?.Invoke(strength);
            }
        }
        if (!grounded) fallingSpeed = Mathf.Max(fallingSpeed, -velocity.y);
        previousVelocity = velocity;
        previousGrounded = grounded;
        hasState = true;
    }

    private void LateUpdate()
    {
        if (!CanAnimate())
        {
            Clear();
            return;
        }
        Vector2 velocity = body.linearVelocity;
        float delta = Time.unscaledDeltaTime;
        impactRemaining = Mathf.Max(0f, impactRemaining - delta);
        float contact = impact * Mathf.Pow(impactRemaining / impactDuration, 2f);
        float flight = player.Grounded ? 0f : airStretch * Mathf.Clamp01(Mathf.Abs(velocity.y) / fullEffectSpeed);
        float stretch = Mathf.Clamp(flight + contact, -0.4f, 0.4f);
        float lean = -Mathf.Clamp(velocity.x / fullEffectSpeed, -1f, 1f) * maximumLean;
        float blend = 1f - Mathf.Exp(-response * delta);
        currentStretch = Mathf.Lerp(currentStretch, stretch, blend);
        currentLean = Mathf.Lerp(currentLean, lean, blend);
        float verticalScale = 1f + currentStretch;
        visual.localScale = new Vector3(originalScale.x / verticalScale,
            originalScale.y * verticalScale, originalScale.z);
        visual.localRotation = originalRotation * Quaternion.Euler(0f, 0f, currentLean);
        float threshold = speedTrail.emitting ? trailStopSpeed : trailStartSpeed;
        speedTrail.emitting = velocity.sqrMagnitude >= threshold * threshold;
    }

    private void UpdateContactImpacts()
    {
        int count = body.GetContacts(contacts);
        int currentSides = 0;
        float strongestSpeed = 0f;
        bool sideImpact = false;
        for (int i = 0; i < count; i++)
        {
            Vector2 normal = contacts[i].normal;
            int side = normal.x > 0.5f ? 1 : normal.x < -0.5f ? 2 : normal.y < -0.5f ? 4 : 0;
            currentSides |= side;
            if (hasState && side != 0 && (previousContactSides & side) == 0)
            {
                float speed = Mathf.Max(0f, -Vector2.Dot(previousVelocity, normal));
                if (speed > strongestSpeed)
                {
                    strongestSpeed = speed;
                    sideImpact = side != 4;
                }
            }
        }
        previousContactSides = currentSides;
        if (strongestSpeed >= minimumImpactSpeed)
        {
            float strength = Mathf.Clamp01(strongestSpeed / fullImpactSpeed);
            SetImpact((sideImpact ? landingSquash : -landingSquash) * strength);
            if (cameraShake != null && landingShake > 0f) cameraShake.Impulse(strength * landingShake);
        }
    }

    private bool CanAnimate()
    {
        return initialized && focused && !paused && !flow.Finished && player.isActiveAndEnabled &&
            body.simulated && player.gameObject.activeInHierarchy;
    }

    private void EmitLanding(float strength)
    {
        if (landingBurst == null) return;
        Bounds bounds = contactCollider.bounds;
        Vector3 point = new Vector3(bounds.center.x, bounds.min.y, player.transform.position.z);
        int count = contactCollider.GetContacts(contacts);
        float bestNormal = 0.5f;
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > bestNormal)
            {
                bestNormal = contacts[i].normal.y;
                point.x = contacts[i].point.x;
                point.y = contacts[i].point.y;
            }
        }
        landingBurst.transform.position = point;
        landingBurst.Emit(Mathf.Clamp(Mathf.CeilToInt(landingParticleCount * strength), 1, 24));
    }

    private void SetImpact(float value)
    {
        impact = value;
        impactRemaining = impactDuration;
    }

    public void Clear()
    {
        hasState = false;
        previousContactSides = 0;
        fallingSpeed = 0f;
        impact = 0f;
        impactRemaining = 0f;
        currentStretch = 0f;
        currentLean = 0f;
        if (initialized && visual != null)
        {
            visual.localScale = originalScale;
            visual.localRotation = originalRotation;
        }
        if (speedTrail != null)
        {
            speedTrail.emitting = false;
            speedTrail.Clear();
        }
        if (landingBurst != null) landingBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Initialize()
    {
        if (initialized) return;
        if (player == null || player.Body == null || flow == null || visual == null || speedTrail == null)
        {
            throw new InvalidOperationException($"DemoPlayerVisuals '{name}' requires player with Body, flow, visual and speedTrail: player={player}, flow={flow}, visual={visual}, speedTrail={speedTrail}.");
        }
        if (visual == player.transform || !visual.IsChildOf(player.transform) ||
            visual.GetComponentInChildren<Collider2D>(true) != null ||
            visual.GetComponentInChildren<Rigidbody2D>(true) != null ||
            visual.GetComponentInChildren<Collider>(true) != null ||
            visual.GetComponentInChildren<Rigidbody>(true) != null ||
            speedTrail.transform == player.transform || !speedTrail.transform.IsChildOf(player.transform) ||
            speedTrail.transform.IsChildOf(visual) || visual.IsChildOf(speedTrail.transform) || speedTrail.autodestruct)
        {
            throw new InvalidOperationException($"DemoPlayerVisuals '{name}' requires a physics-free visual child and independent trail child with autodestruct=false: player='{player.name}', visual='{visual.name}', speedTrail='{speedTrail.name}', autodestruct={speedTrail.autodestruct}.");
        }
        if (!PositiveFinite(fullEffectSpeed) || !PositiveFinite(response) ||
            !PositiveFinite(minimumImpactSpeed) || !PositiveFinite(fullImpactSpeed) ||
            fullImpactSpeed < minimumImpactSpeed || !PositiveFinite(impactDuration) ||
            !InRange(airStretch, 0f, 0.4f) || !InRange(maximumLean, 0f, 25f) ||
            !InRange(landingSquash, 0f, 0.4f) || !InRange(jumpStretch, 0f, 0.4f) ||
            !InRange(landingShake, 0f, 1f) || !PositiveFinite(trailStartSpeed) ||
            !InRange(trailStopSpeed, 0f, trailStartSpeed) || trailStopSpeed >= trailStartSpeed)
        {
            throw new InvalidOperationException($"DemoPlayerVisuals '{name}' has invalid settings: fullEffectSpeed={fullEffectSpeed}, response={response}, minimumImpactSpeed={minimumImpactSpeed}, fullImpactSpeed={fullImpactSpeed}, impactDuration={impactDuration}, airStretch={airStretch}, maximumLean={maximumLean}, landingSquash={landingSquash}, jumpStretch={jumpStretch}, landingShake={landingShake}, trailStartSpeed={trailStartSpeed}, trailStopSpeed={trailStopSpeed}.");
        }
        if (landingBurst != null)
        {
            ParticleSystem.MainModule main = landingBurst.main;
            if (contactCollider == null || contactCollider.attachedRigidbody != player.Body ||
                landingParticleCount < 1 || landingParticleCount > 24 ||
                main.loop || main.playOnAwake || main.simulationSpace != ParticleSystemSimulationSpace.World ||
                main.stopAction != ParticleSystemStopAction.None || landingBurst.emission.enabled ||
                landingBurst.subEmitters.enabled || landingBurst.transform.childCount != 0 ||
                landingBurst.transform == player.transform || !landingBurst.transform.IsChildOf(player.transform) ||
                landingBurst.transform.IsChildOf(visual) || visual.IsChildOf(landingBurst.transform) ||
                landingBurst.transform.IsChildOf(speedTrail.transform) || speedTrail.transform.IsChildOf(landingBurst.transform) ||
                landingBurst.GetComponent<Collider2D>() != null || landingBurst.GetComponent<Rigidbody2D>() != null ||
                landingBurst.GetComponent<Collider>() != null || landingBurst.GetComponent<Rigidbody>() != null)
            {
                throw new InvalidOperationException($"DemoPlayerVisuals '{name}' landingBurst requires an independent physics-free child, player's contactCollider and authored manual world-space emission: contactCollider={contactCollider}, landingParticleCount={landingParticleCount}, loop={main.loop}, playOnAwake={main.playOnAwake}, simulationSpace={main.simulationSpace}, stopAction={main.stopAction}, emission={landingBurst.emission.enabled}, subEmitters={landingBurst.subEmitters.enabled}, children={landingBurst.transform.childCount}.");
            }
        }
        body = player.Body;
        originalScale = visual.localScale;
        originalRotation = visual.localRotation;
        initialized = true;
        Clear();
    }

    private static bool PositiveFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f; }

    private static bool InRange(float value, float minimum, float maximum) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum; }

    private void OnDisable()
    {
        Clear();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (isPaused) Clear();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus) Clear();
    }
}
