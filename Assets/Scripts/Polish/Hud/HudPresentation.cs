using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudPresentation : MonoBehaviour
{
    [SerializeField] private TMP_Text status;
    [SerializeField] private Image primaryFill;
    [SerializeField] private Image secondaryFill;
    [SerializeField] [Min(0.01f)] private float smoothingDuration = 0.16f;
    [SerializeField] [Min(0.01f)] private float stateDuration = 0.2f;
    [SerializeField] [Range(1f, 1.1f)] private float stateScale = 1.025f;

    private Tween stateTween;
    private Vector3 originalScale;
    private bool initialized;

    public float Smooth(float current, float target)
    {
        if (!isActiveAndEnabled || !Initialize()) return current;
        float next = Mathf.Lerp(current, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f / smoothingDuration));

        return Mathf.Abs(next - target) < 0.001f ? target : next;
    }

    public void Present(float primary, float secondary, bool stateChanged)
    {
        if (!isActiveAndEnabled || !Initialize()) return;
        if (status == null)
        {
            Debug.LogError("HUD: потерян status", this);
            enabled = false;
            return;
        }

        if (primaryFill != null) primaryFill.fillAmount = Mathf.Clamp01(primary);
        if (secondaryFill != null) secondaryFill.fillAmount = Mathf.Clamp01(secondary);
        if (!stateChanged || !isActiveAndEnabled) return;

        stateTween?.Kill();
        status.rectTransform.localScale = originalScale * stateScale;
        stateTween = status.rectTransform.DOScale(originalScale, stateDuration)
            .SetEase(Ease.OutCubic).SetUpdate(true);
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
        if (status == null || !PositiveFinite(smoothingDuration) || !PositiveFinite(stateDuration) ||
            !PositiveFinite(stateScale) || stateScale < 1f || stateScale > 1.1f)
        {
            Debug.LogError($"HUD: status={status}, smoothing={smoothingDuration}, duration={stateDuration}, scale={stateScale}", this);
            return false;
        }
        if (!ValidateFill(primaryFill, nameof(primaryFill)) || !ValidateFill(secondaryFill, nameof(secondaryFill))) return false;
        originalScale = status.rectTransform.localScale;
        initialized = true;

        return true;
    }

    private bool ValidateFill(Image image, string field)
    {
        if (image == null || image.type == Image.Type.Filled) return true;
        Debug.LogError($"HUD: {field}.type={image.type}, нужен Filled", this);

        return false;
    }

    private static bool PositiveFinite(float value) { return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value); }

    private void ClearMotion()
    {
        stateTween?.Kill();
        stateTween = null;
        if (initialized && status != null) status.rectTransform.localScale = originalScale;
    }

    private void OnDisable()
    {
        ClearMotion();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ClearMotion();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ClearMotion();
    }
}
