using DG.Tweening;
using UnityEngine;

public class HudSceneTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] [Min(0.01f)] private float duration = 0.18f;

    private Tween fade;

    private void OnEnable()
    {
        if (overlay == null || float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
        {
            Debug.LogError($"Переход: overlay={overlay}, duration={duration}", this);
            enabled = false;
            return;
        }

        overlay.blocksRaycasts = false;
        overlay.interactable = false;
        overlay.alpha = 1f;
        fade = overlay.DOFade(0f, duration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    private void Clear()
    {
        fade?.Kill();
        fade = null;
        if (overlay != null) overlay.alpha = 0f;
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
