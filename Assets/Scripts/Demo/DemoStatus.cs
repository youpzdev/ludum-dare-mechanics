using UnityEngine;

public class DemoStatus : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private DemoFlow flow;
    [SerializeField] private HudPresentation presentation;

    [Space]
    [Header("Scene mechanic")]
    [SerializeField] private BridgeDrawing bridge;
    [SerializeField] private TimeObject battery;
    [SerializeField] private TimePuzzle puzzle;
    [SerializeField] private GrappleController grapple;
    [SerializeField] private GrappleSettings grappleSettings;

    private string previous;
    private float primary;
    private float secondary;
    private float initialInk;

    private int previousState;
    private bool hasState;

    private int previousMechanic;
    private bool previousAttached;
    private bool previousAnimate;

    private float previousPrimary;
    private float previousSecondary;
    private float previousCapacity;
    private float previousMultiplier;
    private float previousHoldSeconds;
    private float previousRange;

    private void Update()
    {
        if (flow == null)
        {
            Debug.LogError($"Не обновлен HUD '{name}': не задан flow", this);
            enabled = false;
            return;
        }
        if (!flow.enabled) return;

        bool animate = presentation != null && presentation.isActiveAndEnabled;
        int mechanic;
        bool attached = false;
        float capacity = 0f;
        float multiplier = 0f;
        float holdSeconds = 0f;
        float range = 0f;
        float primaryTarget;
        float secondaryTarget = 0f;
        int state;
        if (bridge != null)
        {
            mechanic = 1;
            primaryTarget = bridge.RemainingInk;
            if (!hasState)
                initialInk = primaryTarget;
            state = primaryTarget <= 0f ? 1 : 0;
            Smooth(primaryTarget, secondaryTarget, animate);
            if (animate)
                presentation.Present(initialInk > 0f ? primary / initialInk : 0f, 0f, hasState && state != previousState);
        }
        else if (battery != null)
        {
            mechanic = 2;
            if (battery.Settings == null || puzzle == null)
            {
                Debug.LogError($"Не обновлен HUD '{name}': settings={battery.Settings}, puzzle={puzzle}", this);
                enabled = false;
                return;
            }
            primaryTarget = battery.Charge;
            secondaryTarget = puzzle.HoldProgress;
            state = GetTimeState();
            Smooth(primaryTarget, secondaryTarget, animate);
            capacity = battery.Settings.Capacity;
            multiplier = battery.LocalMultiplier;
            holdSeconds = battery.Settings.HoldSeconds;
            if (animate)
                presentation.Present(primary / battery.Settings.Capacity, secondary / battery.Settings.HoldSeconds,
                    hasState && state != previousState);
            if (puzzle.IsComplete)
                flow.Finish(true);
        }
        else if (grapple != null)
        {
            mechanic = 3;
            attached = grapple.IsAttached;
            primaryTarget = attached ? grapple.CurrentDistance : 0f;
            state = attached ? 1 : 0;
            Smooth(primaryTarget, secondaryTarget, animate && hasState && state == previousState);
            if (animate)
            {
                if (grappleSettings == null || !grappleSettings.IsValid)
                {
                    Debug.LogError($"Не обновлен HUD '{name}': неверные grappleSettings={grappleSettings}", this);
                    enabled = false;
                    return;
                }
                range = grappleSettings.Range;
                presentation.Present(attached ? Mathf.Clamp01(primary / grappleSettings.Range) : 0f, 0f,
                    hasState && state != previousState);
            }
        }
        else
            return;

        hasState = true;
        previousState = state;
        // кэш только для текста, полоски пусть живут отдельно
        if (previous != null && mechanic == previousMechanic && attached == previousAttached &&
            animate == previousAnimate && primary == previousPrimary && secondary == previousSecondary &&
            capacity == previousCapacity && multiplier == previousMultiplier &&
            holdSeconds == previousHoldSeconds && range == previousRange)
            return;
        previousMechanic = mechanic;
        previousAttached = attached;
        previousAnimate = animate;
        previousPrimary = primary;
        previousSecondary = secondary;
        previousCapacity = capacity;
        previousMultiplier = multiplier;
        previousHoldSeconds = holdSeconds;
        previousRange = range;

        string text;
        if (mechanic == 1)
            text = $"Чернила: {primary:0.0} м";
        else if (mechanic == 2)
            text = $"Заряд: {primary:0} / {capacity:0}   Время: x{multiplier:0.0}   Приемник: {secondary:0.0} / {holdSeconds:0} с";
        else
        {
            text = attached ? $"Зацеп: {primary:0.0} м" : "Наведи на бирюзовый якорь и удерживай ЛКМ";
            if (animate)
                text += $"   Дальность: {range:0.0} м";
        }
        if (text == previous)
            return;

        previous = text;
        flow.SetStatus(text);
    }

    private void Smooth(float primaryTarget, float secondaryTarget, bool animate)
    {
        primary = animate && hasState ? presentation.Smooth(primary, primaryTarget) : primaryTarget;
        secondary = animate && hasState ? presentation.Smooth(secondary, secondaryTarget) : secondaryTarget;
    }

    private int GetTimeState()
    {
        if (puzzle.IsComplete) return 4;
        if (battery.IsDocked) return 3;
        if (battery.LocalMultiplier > 1f) return 2;
        if (battery.LocalMultiplier < 1f) return 1;

        return 0;
    }

    private void OnDisable()
    {
        hasState = false;
        previous = null;
    }
}
