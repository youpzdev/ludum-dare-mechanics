using DG.Tweening;
using UnityEngine;

public class BridgeFeedback : MonoBehaviour
{
    [SerializeField] private BridgeSettings settings;
    [SerializeField] private SpriteRenderer tip;
    [SerializeField] private SpriteRenderer pulse;

    private Tween pulseTween;

    private bool hasAcceptedPoint;
    private bool exhaustionShown;
    private bool failed;

    private Vector2 acceptedPoint;

    public void Configure(BridgeSettings bridgeSettings, SpriteRenderer tipRenderer, SpriteRenderer pulseRenderer)
    {
        if (bridgeSettings == null || tipRenderer == null || pulseRenderer == null)
        {
            Fail($"Отклик моста {name}: settings={bridgeSettings}, tip={tipRenderer}, pulse={pulseRenderer}");
            return;
        }

        if (tipRenderer == pulseRenderer || tipRenderer.transform == transform || pulseRenderer.transform == transform ||
            !tipRenderer.transform.IsChildOf(transform) || !pulseRenderer.transform.IsChildOf(transform))
        {
            Fail($"Отклик моста {name}: tip={tipRenderer.name} и pulse={pulseRenderer.name} должны быть разными дочерними объектами");
            return;
        }

        if (!bridgeSettings.Validate()) { Fail(null); return; }

        ResetFeedback();
        settings = bridgeSettings;
        tip = tipRenderer;
        pulse = pulseRenderer;
        if (failed) { failed = false; enabled = true; }
        ResetFeedback();
    }

    public void BeginStroke()
    {
        if (!ValidateReferences()) return;
        ClearFeedback();
    }

    public void AcceptPoint(Vector2 point)
    {
        if (!ValidateReferences()) return;
        acceptedPoint = point;
        hasAcceptedPoint = true;
        tip.transform.position = new Vector3(point.x, point.y, 0);
        tip.transform.localScale = Vector3.one * settings.TipSize;
        tip.color = settings.TipColor;
        tip.enabled = true;
    }

    public void CompleteStroke()
    {
        if (!ValidateReferences()) return;
        tip.enabled = false;
        if (hasAcceptedPoint) PlayPulse(settings.CompletionColor, settings.CompletionDuration);

        hasAcceptedPoint = false;
    }

    public void ExhaustInk()
    {
        if (!ValidateReferences()) return;
        tip.enabled = false;
        if (hasAcceptedPoint && !exhaustionShown)
        {
            PlayPulse(settings.ExhaustionColor, settings.ExhaustionDuration);
            exhaustionShown = true;
        }

        hasAcceptedPoint = false;
    }

    public void ClearFeedback()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
            pulseTween = null;
        }

        if (tip != null)
        {
            tip.enabled = false;
            if (settings != null && !failed)
            {
                tip.color = settings.TipColor;
                tip.transform.localScale = Vector3.one * settings.TipSize;
            }
        }

        if (pulse != null)
        {
            pulse.enabled = false;
            if (settings != null && !failed)
            {
                pulse.color = settings.CompletionColor;
                pulse.transform.localScale = Vector3.one * settings.PulseSize;
            }
        }

        hasAcceptedPoint = false;
    }

    public void ResetFeedback()
    {
        ClearFeedback();
        exhaustionShown = false;
    }

    private void PlayPulse(Color color, float duration)
    {
        if (pulseTween != null) pulseTween.Kill();

        pulse.transform.position = new Vector3(acceptedPoint.x, acceptedPoint.y, 0);
        pulse.transform.localScale = Vector3.one * settings.PulseSize;
        pulse.color = color;
        pulse.enabled = true;
        Color transparent = color;
        transparent.a = 0;
        pulseTween = DOTween.To(() => pulse.color, value => pulse.color = value, transparent, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                pulseTween = null;
                pulse.enabled = false;
                pulse.color = settings.CompletionColor;
            });
    }

    private void OnEnable()
    {
        ResetFeedback();
    }

    private void OnDisable()
    {
        ClearFeedback();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ClearFeedback();
    }

    private bool ValidateReferences()
    {
        if (failed) return false;
        if (settings == null || tip == null || pulse == null)
            return Fail($"Отклик моста {name}: settings={settings}, tip={tip}, pulse={pulse}");
        if (!settings.Validate()) return Fail(null);

        return true;
    }

    private bool Fail(string error)
    {
        if (!failed && error != null) Debug.LogError(error, this);
        failed = true;
        enabled = false;

        return false;
    }
}
