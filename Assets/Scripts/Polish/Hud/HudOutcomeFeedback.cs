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
        if (!isActiveAndEnabled || !Initialize()) return;
        if (outcome == null)
        {
            Debug.LogError("Результат: потерян outcome", this);
            enabled = false;
            return;
        }
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
        {
            Debug.LogError($"Результат: неверная duration={duration}", this);
            return;
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
        if (!Initialize()) enabled = false;
    }

    private void OnEnable()
    {
        if (!Initialize()) enabled = false;
    }

    private bool Initialize()
    {
        if (initialized) return true;
        if (outcome == null || float.IsNaN(scaleFrom) || float.IsInfinity(scaleFrom) ||
            scaleFrom < 0.5f || scaleFrom > 1f || float.IsNaN(riseDistance) ||
            float.IsInfinity(riseDistance) || riseDistance < 0f)
        {
            Debug.LogError($"Результат: outcome={outcome}, scale={scaleFrom}, rise={riseDistance}", this);
            return false;
        }
        if (victoryAudio != null && (victoryAudio.clip == null || victoryAudio.loop || victoryAudio.playOnAwake))
        {
            Debug.LogError($"Победный звук: clip={victoryAudio.clip}, loop={victoryAudio.loop}, playOnAwake={victoryAudio.playOnAwake}", this);
            return false;
        }
        if (victoryParticles != null && (victoryParticles.main.loop || victoryParticles.main.playOnAwake))
        {
            Debug.LogError($"Победные частицы: loop={victoryParticles.main.loop}, playOnAwake={victoryParticles.main.playOnAwake}", this);
            return false;
        }

        originalScale = outcome.rectTransform.localScale;
        originalPosition = outcome.rectTransform.anchoredPosition;
        initialized = true;

        return true;
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
