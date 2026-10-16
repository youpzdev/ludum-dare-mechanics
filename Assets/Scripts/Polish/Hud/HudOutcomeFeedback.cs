using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class HudOutcomeFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text outcome;
    [SerializeField] private ParticleSystem victoryParticles;
    [SerializeField] private AudioSource victoryAudio;
    [SerializeField] [Range(0.5f, 1f)] private float scaleFrom = 0.9f;
    [SerializeField] [Min(0f)] private float riseDistance = 12f;

    private Tween motion;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private bool initialized;

    public void Play(bool won, float duration)
    {
        if (!initialized) Initialize();
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "HudOutcomeFeedback duration must be finite and positive on " + name);
        }

        Clear();
        if (!isActiveAndEnabled) return;

        outcome.rectTransform.localScale = originalScale * scaleFrom;
        outcome.rectTransform.anchoredPosition = originalPosition - Vector2.up * riseDistance;
        motion = DOTween.Sequence()
            .Join(outcome.rectTransform.DOScale(originalScale, duration).SetEase(Ease.OutCubic))
            .Join(outcome.rectTransform.DOAnchorPos(originalPosition, duration).SetEase(Ease.OutCubic))
            .SetUpdate(true);
        if (won)
        {
            victoryParticles?.Play(true);
            if (victoryAudio != null) victoryAudio.Play();
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (outcome == null || float.IsNaN(scaleFrom) || float.IsInfinity(scaleFrom) ||
            scaleFrom < 0.5f || scaleFrom > 1f || float.IsNaN(riseDistance) ||
            float.IsInfinity(riseDistance) || riseDistance < 0f)
        {
            throw new InvalidOperationException("HudOutcomeFeedback requires outcome and finite scale/rise on " + name + "; scale=" + scaleFrom + " rise=" + riseDistance);
        }
        if (victoryAudio != null && (victoryAudio.clip == null || victoryAudio.loop || victoryAudio.playOnAwake))
        {
            throw new InvalidOperationException("HudOutcomeFeedback victoryAudio requires a clip, loop=false and playOnAwake=false on " + name);
        }
        if (victoryParticles != null && (victoryParticles.main.loop || victoryParticles.main.playOnAwake))
        {
            throw new InvalidOperationException("HudOutcomeFeedback victoryParticles requires loop=false and playOnAwake=false on " + name);
        }

        originalScale = outcome.rectTransform.localScale;
        originalPosition = outcome.rectTransform.anchoredPosition;
        initialized = true;
    }

    public void Clear()
    {
        motion?.Kill();
        motion = null;
        if (initialized && outcome != null)
        {
            outcome.rectTransform.localScale = originalScale;
            outcome.rectTransform.anchoredPosition = originalPosition;
        }
        if (victoryParticles != null) victoryParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (victoryAudio != null) victoryAudio.Stop();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) Clear();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Clear();
    }
}
