using System;
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

    private Vector2 acceptedPoint;

    public void Configure(BridgeSettings bridgeSettings, SpriteRenderer tipRenderer, SpriteRenderer pulseRenderer)
    {
        if (bridgeSettings == null || tipRenderer == null || pulseRenderer == null)
        {
            throw new ArgumentException("BridgeFeedback requires settings, tip and pulse renderers on " + name);
        }

        if (tipRenderer == pulseRenderer || tipRenderer.transform == transform || pulseRenderer.transform == transform ||
            !tipRenderer.transform.IsChildOf(transform) || !pulseRenderer.transform.IsChildOf(transform))
        {
            throw new ArgumentException("BridgeFeedback tip and pulse must be separate child renderers of " + name);
        }

        ResetFeedback();
        settings = bridgeSettings;
        tip = tipRenderer;
        pulse = pulseRenderer;
        ResetFeedback();
    }

    public void BeginStroke()
    {
        ValidateReferences();
        ClearFeedback();
    }

    public void AcceptPoint(Vector2 point)
    {
        ValidateReferences();
        acceptedPoint = point;
        hasAcceptedPoint = true;
        tip.transform.position = new Vector3(point.x, point.y, 0);
        tip.transform.localScale = Vector3.one * settings.TipSize;
        tip.color = settings.TipColor;
        tip.enabled = true;
    }

    public void CompleteStroke()
    {
        ValidateReferences();
        tip.enabled = false;
        if (hasAcceptedPoint) PlayPulse(settings.CompletionColor, settings.CompletionDuration);

        hasAcceptedPoint = false;
    }

    public void ExhaustInk()
    {
        ValidateReferences();
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
            if (settings != null)
            {
                tip.color = settings.TipColor;
                tip.transform.localScale = Vector3.one * settings.TipSize;
            }
        }

        if (pulse != null)
        {
            pulse.enabled = false;
            if (settings != null)
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

    private void ValidateReferences()
    {
        if (settings == null || tip == null || pulse == null)
        {
            throw new InvalidOperationException("BridgeFeedback settings, tip or pulse is unassigned on " + name);
        }
    }
}
