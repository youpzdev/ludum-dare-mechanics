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
        if (!Initialize()) enabled = false;
    }

    private void OnEnable()
    {
        if (!Initialize())
        {
            enabled = false;
            return;
        }
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
                    if (!EmitLanding(strength)) return;
                    SetImpact(-landingSquash * strength);
                    if (cameraShake != null && landingShake > 0f) cameraShake.Impulse(strength * landingShake);
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
        if (!initialized || !isActiveAndEnabled) return false;
        if (player == null || flow == null || body == null || visual == null || speedTrail == null)
        {
            Debug.LogError($"Визуал игрока: player={player}, flow={flow}, body={body}, visual={visual}, trail={speedTrail}", this);
            enabled = false;
            return false;
        }

        return focused && !paused && !flow.Finished && player.isActiveAndEnabled &&
            body.simulated && player.gameObject.activeInHierarchy;
    }

    private bool EmitLanding(float strength)
    {
        if (landingBurst == null) return true;
        if (contactCollider == null)
        {
            Debug.LogError("Приземление: потерян contactCollider", this);
            Clear();
            enabled = false;
            return false;
        }
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

        return true;
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

    private bool Initialize()
    {
        if (initialized) return true;
        if (player == null || player.Body == null || flow == null || visual == null || speedTrail == null)
        {
            Debug.LogError($"Визуал игрока: player={player}, body={(player != null ? player.Body : null)}, flow={flow}, visual={visual}, trail={speedTrail}", this);
            return false;
        }
        if (!ValidateVisuals() || !ValidateMotion() || !ValidateLanding()) return false;
        body = player.Body;
        originalScale = visual.localScale;
        originalRotation = visual.localRotation;
        initialized = true;
        Clear();

        return true;
    }

    private bool ValidateVisuals()
    {
        bool visualIsChild = visual != player.transform && visual.IsChildOf(player.transform);
        bool trailIsChild = speedTrail.transform != player.transform && speedTrail.transform.IsChildOf(player.transform);
        bool overlap = speedTrail.transform.IsChildOf(visual) || visual.IsChildOf(speedTrail.transform);
        if (!visualIsChild || HasPhysics(visual) || !trailIsChild || overlap || speedTrail.autodestruct)
        {
            Debug.LogError($"Визуал игрока: visual={visual.name}, trail={speedTrail.name}, overlap={overlap}, autodestruct={speedTrail.autodestruct}; нужны отдельные дочерние визуалы", this);
            return false;
        }

        return true;
    }

    private bool ValidateMotion()
    {
        if (!PositiveFinite(fullEffectSpeed) || !PositiveFinite(response) ||
            !PositiveFinite(minimumImpactSpeed) || !PositiveFinite(fullImpactSpeed) ||
            fullImpactSpeed < minimumImpactSpeed || !PositiveFinite(impactDuration))
        {
            Debug.LogError($"Движение визуала: fullSpeed={fullEffectSpeed}, response={response}, impactSpeed={minimumImpactSpeed}/{fullImpactSpeed}, duration={impactDuration}", this);
            return false;
        }
        if (!InRange(airStretch, 0f, 0.4f) || !InRange(maximumLean, 0f, 25f) ||
            !InRange(landingSquash, 0f, 0.4f) || !InRange(jumpStretch, 0f, 0.4f) ||
            !InRange(landingShake, 0f, 1f))
        {
            Debug.LogError($"Деформация игрока: air={airStretch}, lean={maximumLean}, squash={landingSquash}, jump={jumpStretch}, shake={landingShake}", this);
            return false;
        }
        if (!PositiveFinite(trailStartSpeed) || !InRange(trailStopSpeed, 0f, trailStartSpeed) || trailStopSpeed >= trailStartSpeed)
        {
            Debug.LogError($"Шлейф: startSpeed={trailStartSpeed}, stopSpeed={trailStopSpeed}", this);
            return false;
        }

        return true;
    }

    private bool ValidateLanding()
    {
        if (landingBurst == null) return true;
        if (contactCollider == null || contactCollider.attachedRigidbody != player.Body ||
            landingParticleCount < 1 || landingParticleCount > 24)
        {
            Debug.LogError($"Приземление: contactCollider={contactCollider}, count={landingParticleCount}; нужен коллайдер игрока", this);
            return false;
        }
        ParticleSystem.MainModule main = landingBurst.main;
        if (main.loop || main.playOnAwake || main.simulationSpace != ParticleSystemSimulationSpace.World ||
            main.stopAction != ParticleSystemStopAction.None || landingBurst.emission.enabled || landingBurst.subEmitters.enabled)
        {
            Debug.LogError($"Приземление: loop={main.loop}, awake={main.playOnAwake}, space={main.simulationSpace}, stop={main.stopAction}, emission={landingBurst.emission.enabled}, subEmitters={landingBurst.subEmitters.enabled}", this);
            return false;
        }
        Transform burst = landingBurst.transform;
        bool overlaps = burst.IsChildOf(visual) || visual.IsChildOf(burst) ||
            burst.IsChildOf(speedTrail.transform) || speedTrail.transform.IsChildOf(burst);
        if (burst.childCount != 0 || burst == player.transform || !burst.IsChildOf(player.transform) ||
            overlaps || HasPhysics(burst))
        {
            Debug.LogError($"Приземление: burst={burst.name}, children={burst.childCount}, overlap={overlaps}; нужен отдельный дочерний визуал", this);
            return false;
        }

        return true;
    }

    private static bool HasPhysics(Transform target)
    {
        return target.GetComponentInChildren<Collider2D>(true) != null ||
            target.GetComponentInChildren<Rigidbody2D>(true) != null ||
            target.GetComponentInChildren<Collider>(true) != null ||
            target.GetComponentInChildren<Rigidbody>(true) != null;
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
