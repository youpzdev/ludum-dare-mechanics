using DG.Tweening;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class TimeRoomFeedback : MonoBehaviour
{
    private bool failed;
    private bool disabledByFailure;

    private const int ChargeFillIndex = 0;
    private const int HoldFillIndex = 1;
    private const int DoorVisualIndex = 4;

    [SerializeField] private TimeObject battery;
    [SerializeField] private TimePuzzle puzzle;
    [SerializeField] private CarryController carry;
    [SerializeField] private SpriteRenderer chargeFill;
    [SerializeField] private SpriteRenderer holdFill;
    [SerializeField] private SpriteRenderer modeVisual;
    [SerializeField] private SpriteRenderer socketVisual;
    [SerializeField] private SpriteRenderer doorVisual;

    private SpriteRenderer[] visuals;
    private Color[] originalColors;
    private Vector3[] originalScales;
    private bool[] originalEnabled;

    private Tween modeTween;
    private Tween socketTween;

    private int lastMode;
    private int lastSocket;

    private bool initialized;
    private bool hasState;

    public void Configure(TimeObject item, TimePuzzle timePuzzle, CarryController controller,
        SpriteRenderer chargeRenderer, SpriteRenderer holdRenderer, SpriteRenderer modeRenderer,
        SpriteRenderer socketRenderer, SpriteRenderer doorRenderer)
    {
        failed = false;
        RestoreVisuals();
        initialized = false;
        battery = item;
        puzzle = timePuzzle;
        carry = controller;
        chargeFill = chargeRenderer;
        holdFill = holdRenderer;
        modeVisual = modeRenderer;
        socketVisual = socketRenderer;
        doorVisual = doorRenderer;
        if (!Initialize()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
        RefreshVisuals(false);
    }

    public void ResetFeedback()
    {
        if (!initialized && !Initialize()) return;

        RestoreVisuals();
        RefreshVisuals(false);
    }

    private void Start()
    {
        if (!initialized && !Initialize()) return;

        RefreshVisuals(false);
    }

    private void OnEnable()
    {
        if (initialized) RefreshVisuals(false);
    }

    private void LateUpdate()
    {
        RefreshVisuals(true);
    }

    private void RefreshVisuals(bool animate)
    {
        if (failed) return;
        if (!initialized && !Initialize()) return;
        TimeRoomSettings settings = battery.Settings;
        Vector3 chargeScale = originalScales[ChargeFillIndex];
        chargeScale.x *= Mathf.Clamp01(battery.Charge / settings.Capacity);
        chargeFill.transform.localScale = chargeScale;
        chargeFill.color = settings.ChargeColor;
        Vector3 holdScale = originalScales[HoldFillIndex];
        holdScale.x *= Mathf.Clamp01(puzzle.HoldProgress / settings.HoldSeconds);
        holdFill.transform.localScale = holdScale;
        holdFill.color = settings.HoldColor;
        doorVisual.enabled = originalEnabled[DoorVisualIndex] && !puzzle.IsComplete;

        int mode = battery.LocalMultiplier > 1f ? 1 : battery.LocalMultiplier < 1f ? -1 : 0;
        int socket = battery.IsDocked ? 2 : carry.IsDragging && battery.CanDock ? 1 : 0;
        bool tween = animate && hasState && isActiveAndEnabled;
        if (!hasState || mode != lastMode)
        {
            Color color = mode > 0 ? settings.FastColor : mode < 0 ? settings.SlowColor : settings.NormalColor;
            ChangeColor(modeVisual, color, ref modeTween, tween);
            lastMode = mode;
        }

        if (!hasState || socket != lastSocket)
        {
            Color color = socket == 2 ? settings.DockedColor : socket == 1 ? settings.DockReadyColor : settings.SocketColor;
            ChangeColor(socketVisual, color, ref socketTween, tween);
            lastSocket = socket;
        }

        hasState = true;
    }

    private void ChangeColor(SpriteRenderer target, Color color, ref Tween ownedTween, bool animate)
    {
        if (ownedTween != null)
        {
            ownedTween.Kill();
            ownedTween = null;
        }

        if (animate)
            ownedTween = DOTween.To(() => target.color, value => target.color = value,
                color, battery.Settings.FeedbackDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        else
            target.color = color;
    }

    private bool Initialize()
    {
        if (failed) return false;
        if (battery == null || puzzle == null || carry == null || battery.Settings == null)
            return Invalid($"Отклик времени {name}: нужны battery с settings, puzzle и carry");

        if (!battery.Settings.Validate())
        {
            RestoreVisuals();
            initialized = false;
            disabledByFailure |= enabled;
            failed = true;
            enabled = false;
            return false;
        }
        float duration = battery.Settings.FeedbackDuration;
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0)
            return Invalid($"Отклик времени {name}: feedbackDuration={duration}, нужно положительное конечное время");

        visuals = new[] { chargeFill, holdFill, modeVisual, socketVisual, doorVisual };
        for (int i = 0; i < visuals.Length; i++)
        {
            SpriteRenderer visual = visuals[i];
            if (visual == null) return Invalid($"Отклик времени {name}: visuals[{i}]=null");
            if (visual.GetComponentInChildren<Collider2D>(true) != null ||
                visual.transform == battery.transform || visual.transform == transform)
                return Invalid($"Отклик времени {name}: visuals[{i}] нужен отдельный transform без коллайдеров");

            for (int j = 0; j < i; j++)
            {
                if (visual.transform.IsChildOf(visuals[j].transform) || visuals[j].transform.IsChildOf(visual.transform))
                    return Invalid($"Отклик времени {name}: visuals[{i}] и visuals[{j}] вложены друг в друга");
            }
        }

        originalColors = new Color[visuals.Length];
        originalScales = new Vector3[visuals.Length];
        originalEnabled = new bool[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            originalColors[i] = visuals[i].color;
            originalScales[i] = visuals[i].transform.localScale;
            originalEnabled[i] = visuals[i].enabled;
        }

        initialized = true;
        hasState = false;

        return true;
    }

    private bool Invalid(string message)
    {
        if (failed) return false;
        Debug.LogError(message, this);
        RestoreVisuals();
        initialized = false;
        disabledByFailure |= enabled;
        failed = true;
        enabled = false;

        return false;
    }

    private void RestoreVisuals()
    {
        if (modeTween != null)
        {
            modeTween.Kill();
            modeTween = null;
        }

        if (socketTween != null)
        {
            socketTween.Kill();
            socketTween = null;
        }

        if (initialized)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] != null)
                {
                    visuals[i].color = originalColors[i];
                    visuals[i].transform.localScale = originalScales[i];
                    visuals[i].enabled = originalEnabled[i];
                }
            }
        }

        hasState = false;
    }

    private void OnDisable()
    {
        RestoreVisuals();
        if (initialized && doorVisual != null && puzzle != null)
            doorVisual.enabled = originalEnabled[DoorVisualIndex] && !puzzle.IsComplete;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && initialized) ResetFeedback();
    }
}
