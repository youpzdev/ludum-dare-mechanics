using DG.Tweening;
using TMPro;
using UnityEngine;

public class HudErrorFeedback : MonoBehaviour
{
    [SerializeField] private MechanicPresentation source;
    [SerializeField] private TMP_Text message;
    [SerializeField] [Min(0.01f)] private float hintDelay = 1f;
    [SerializeField] [Min(0.01f)] private float hintDuration = 3f;
    [SerializeField] [Min(0.01f)] private float fadeDuration = 0.18f;

    private Sequence feedback;
    private MechanicPresentation subscribedSource;

    private void OnEnable()
    {
        if (source == null || message == null || !PositiveFinite(hintDelay) ||
            !PositiveFinite(hintDuration) || !PositiveFinite(fadeDuration))
        {
            Debug.LogError($"Подсказки: source={source}, message={message}, delay={hintDelay}, duration={hintDuration}, fade={fadeDuration}", this);
            enabled = false;
            return;
        }

        Clear();
        subscribedSource = source;
        subscribedSource.Cue += OnCue;
    }

    private static bool PositiveFinite(float value) { return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value); }

    private void OnCue(MechanicCue cue, Vector3 position)
    {
        if (!isActiveAndEnabled) return;
        if (cue == MechanicCue.InkRejected || cue == MechanicCue.HookRejected || cue == MechanicCue.BatteryRejected)
        {
            if (source == null || message == null)
            {
                Debug.LogError($"Подсказки: потеряны source={source}, message={message}", this);
                Clear();
                enabled = false;
                return;
            }
            string error = source.ErrorText;
            string hint = source.HintText;
            if (string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(hint))
            {
                Debug.LogError($"Подсказки: cue={cue}, пустые ErrorText/HintText у {source.name}", this);
                Clear();
                return;
            }

            Clear();
            message.text = error;
            feedback = DOTween.Sequence()
                .Append(message.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic))
                .AppendInterval(hintDelay)
                .AppendCallback(() => { if (message != null) message.text = error + "\n" + hint; })
                .AppendInterval(hintDuration)
                .Append(message.DOFade(0f, fadeDuration).SetEase(Ease.OutCubic))
                .OnComplete(() => { if (message != null) message.text = string.Empty; })
                .SetUpdate(true);
        }
        else if (cue == MechanicCue.BridgeCompleted || cue == MechanicCue.HookAttached ||
            cue == MechanicCue.BatteryDocked || cue == MechanicCue.DoorOpened ||
            cue == MechanicCue.HookReleased || cue == MechanicCue.Cancelled)
        {
            Clear();
        }
    }

    public void Clear()
    {
        feedback?.Kill();
        feedback = null;
        if (message != null)
        {
            message.text = string.Empty;
            message.alpha = 0f;
        }
    }

    private void OnDisable()
    {
        if (subscribedSource != null)
        {
            subscribedSource.Cue -= OnCue;
            subscribedSource = null;
        }
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
