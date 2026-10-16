using DG.Tweening;
using UnityEngine;

public class GrappleFeedback : MonoBehaviour
{
    [SerializeField] private GrappleSettings settings;
    [SerializeField] private SpriteRenderer aimMarker;
    [SerializeField] private SpriteRenderer contactCue;
    [SerializeField] private LineRenderer rope;
    [SerializeField] private SpriteRenderer contactGlow;
    [SerializeField] private ParticleSystem contactBurst;

    private Sequence glowTween;
    private Sequence contactTween;
    private Tween ropeTween;

    private bool initialized;
    private bool invalid;
    private bool disabledByFailure;

    private Color ropeStartColor;
    private Color ropeEndColor;

    public void Configure(GrappleSettings balance, SpriteRenderer marker, SpriteRenderer contact, LineRenderer line)
    {
        Clear();
        settings = balance;
        aimMarker = marker;
        contactCue = contact;
        rope = line;
        initialized = false;
        invalid = false;
        if (!Initialize()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
    }

    public void ConfigureContactExtras(SpriteRenderer glow, ParticleSystem burst)
    {
        Clear();
        contactGlow = glow;
        contactBurst = burst;
        initialized = false;
        invalid = false;
        if (!Initialize()) return;
        if (disabledByFailure) { disabledByFailure = false; enabled = true; }
    }

    private bool ValidateExtras()
    {
        if (contactGlow != null)
        {
            if (contactGlow.sprite == null)
                return StopInvalid($"Крюк {name}: у glow {contactGlow.name} нет sprite");
            if (!ValidateExtraTransform(contactGlow.transform)) return false;
        }
        if (contactBurst != null)
        {
            if (!ValidateExtraTransform(contactBurst.transform)) return false;
            ParticleSystem.MainModule main = contactBurst.main;
            if (main.loop || main.playOnAwake || main.simulationSpace != ParticleSystemSimulationSpace.World ||
                main.stopAction != ParticleSystemStopAction.None ||
                contactBurst.transform.childCount != 0 || contactBurst.subEmitters.enabled)
            {
                return StopInvalid($"Крюк {name}: burst {contactBurst.name}, loop={main.loop}, playOnAwake={main.playOnAwake}, space={main.simulationSpace}, stop={main.stopAction}, children={contactBurst.transform.childCount}, subEmitters={contactBurst.subEmitters.enabled}");
            }
        }
        if (contactGlow != null && contactBurst != null &&
            (contactGlow.transform == contactBurst.transform || contactGlow.transform.IsChildOf(contactBurst.transform) ||
            contactBurst.transform.IsChildOf(contactGlow.transform)))
        {
            return StopInvalid($"Крюк {name}: glow и burst должны быть отдельными объектами");
        }

        return true;
    }

    private bool ValidateExtraTransform(Transform visual)
    {
        if (visual.GetComponent<Rigidbody2D>() != null || visual.GetComponent<Collider2D>() != null ||
            visual.GetComponent<Rigidbody>() != null || visual.GetComponent<Collider>() != null ||
            visual.childCount != 0 ||
            (aimMarker != null && (visual == aimMarker.transform || aimMarker.transform.IsChildOf(visual) || visual.IsChildOf(aimMarker.transform))) ||
            (contactCue != null && (visual == contactCue.transform || contactCue.transform.IsChildOf(visual) || visual.IsChildOf(contactCue.transform))) ||
            (rope != null && (visual == rope.transform || rope.transform.IsChildOf(visual))))
        {
            return StopInvalid($"Крюк {name}: эффект {visual.name} вложен в другой эффект или содержит физику, children={visual.childCount}");
        }

        return true;
    }

    private void Start()
    {
        if (!Initialize()) return;
    }

    private bool Initialize()
    {
        if (invalid) return false;

        if (settings == null || aimMarker == null || contactCue == null || rope == null ||
            aimMarker == contactCue || aimMarker.sprite == null || contactCue.sprite == null)
        {
            return StopInvalid($"Крюк {name}: settings={settings}, marker={aimMarker}, contact={contactCue}, rope={rope}, нужны разные маркеры со sprite");
        }

        if (aimMarker.GetComponent<Rigidbody2D>() != null || contactCue.GetComponent<Rigidbody2D>() != null)
            return StopInvalid($"Крюк {name}: физика на marker={aimMarker.name} или contact={contactCue.name}");

        if (!settings.IsFeedbackValid)
            return StopInvalid($"Крюк {name}: {settings.name}, marker={settings.AvailableMarkerSize}/{settings.BlockedMarkerSize}/{settings.NoTargetMarkerSize}, contact={settings.ContactSize}/{settings.ContactEndSize}/{settings.ContactDuration}, rope={settings.RopeAccentDuration}, glow={settings.ContactGlowSize}/{settings.ContactGlowEndSize}/{settings.ContactGlowOpacity}");

        if (!ValidateExtras() || !ValidateSize(aimMarker, out _) || !ValidateSize(contactCue, out _) ||
            (contactGlow != null && !ValidateSize(contactGlow, out _))) return false;

        if (initialized) return true;

        ropeStartColor = rope.startColor;
        ropeEndColor = rope.endColor;
        initialized = true;
        Clear();

        return true;
    }

    private bool StopInvalid(string message)
    {
        if (!invalid) Debug.LogError(message, this);
        disabledByFailure |= enabled;
        invalid = true;
        initialized = false;
        Clear();
        enabled = false;

        return false;
    }

    public void ShowTarget(GrappleTarget target, float depth)
    {
        if (!Initialize()) return;
        if (!isActiveAndEnabled)
        {
            Clear();
            return;
        }

        Color color = target.State == GrappleTargetState.Available ? settings.AvailableColor :
            target.State == GrappleTargetState.Blocked ? settings.BlockedColor : settings.NoTargetColor;
        float size = target.State == GrappleTargetState.Available ? settings.AvailableMarkerSize :
            target.State == GrappleTargetState.Blocked ? settings.BlockedMarkerSize : settings.NoTargetMarkerSize;
        aimMarker.transform.position = new Vector3(target.Point.x, target.Point.y, depth);
        aimMarker.color = color;
        if (!SetSize(aimMarker, size)) return;
        aimMarker.enabled = true;
    }

    public void ShowAttachment(Vector3 point)
    {
        if (!Initialize()) return;
        Clear();
        if (!isActiveAndEnabled || invalid) return;

        contactCue.transform.position = point;
        contactCue.color = settings.ContactColor;
        if (!SetSize(contactCue, settings.ContactSize)) return;
        contactCue.enabled = true;
        contactTween = DOTween.Sequence().SetUpdate(true);
        contactTween.Join(DOTween.To(() => settings.ContactSize, value => SetSize(contactCue, value),
            settings.ContactEndSize, settings.ContactDuration).SetEase(Ease.OutQuad));
        contactTween.Join(contactCue.DOFade(0f, settings.ContactDuration).SetEase(Ease.OutQuad));
        contactTween.OnComplete(() =>
        {
            contactCue.enabled = false;
            contactTween = null;
        });
        if (contactGlow != null)
        {
            contactGlow.transform.position = point;
            Color glowColor = settings.ContactColor;
            glowColor.a *= settings.ContactGlowOpacity;
            contactGlow.color = glowColor;
            if (!SetSize(contactGlow, settings.ContactGlowSize)) return;
            contactGlow.enabled = true;
            glowTween = DOTween.Sequence().SetUpdate(true);
            glowTween.Join(DOTween.To(() => settings.ContactGlowSize, value => SetSize(contactGlow, value),
                settings.ContactGlowEndSize, settings.ContactDuration).SetEase(Ease.OutQuad));
            glowTween.Join(contactGlow.DOFade(0f, settings.ContactDuration).SetEase(Ease.OutQuad));
            glowTween.OnComplete(() =>
            {
                contactGlow.enabled = false;
                glowTween = null;
            });
        }
        if (contactBurst != null)
        {
            contactBurst.transform.position = point;
            contactBurst.Play(false);
        }
        if (settings.RopeAccentDuration > 0f)
        {
            rope.startColor = settings.ContactColor;
            rope.endColor = settings.ContactColor;
            ropeTween = DOTween.To(() => 0f, progress =>
            {
                rope.startColor = Color.Lerp(settings.ContactColor, ropeStartColor, progress);
                rope.endColor = Color.Lerp(settings.ContactColor, ropeEndColor, progress);
            }, 1f, settings.RopeAccentDuration).SetEase(Ease.OutQuad).SetUpdate(true)
                .OnComplete(() => ropeTween = null);
        }
    }

    public void FollowAttachment(Vector3 point)
    {
        if (invalid) return;

        if (contactCue != null && contactCue.enabled) contactCue.transform.position = point;
        if (contactGlow != null && contactGlow.enabled) contactGlow.transform.position = point;
        if (contactBurst != null && contactBurst.isPlaying) contactBurst.transform.position = point;
    }

    public void Clear()
    {
        contactTween?.Kill();
        contactTween = null;
        ropeTween?.Kill();
        ropeTween = null;
        glowTween?.Kill();
        glowTween = null;
        if (contactGlow != null)
        {
            contactGlow.enabled = false;
            if (initialized && settings != null)
            {
                Color glowColor = settings.ContactColor;
                glowColor.a *= settings.ContactGlowOpacity;
                contactGlow.color = glowColor;
                if (!SetSize(contactGlow, settings.ContactGlowSize)) return;
            }
        }
        if (contactBurst != null) contactBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (aimMarker != null) aimMarker.enabled = false;
        if (contactCue != null)
        {
            contactCue.enabled = false;
            if (initialized && settings != null)
            {
                contactCue.color = settings.ContactColor;
                if (!SetSize(contactCue, settings.ContactSize)) return;
            }
        }
        if (initialized && rope != null)
        {
            rope.startColor = ropeStartColor;
            rope.endColor = ropeEndColor;
        }
    }

    private bool ValidateSize(SpriteRenderer renderer, out float scale)
    {
        scale = 0f;
        if (renderer == null || renderer.sprite == null)
            return StopInvalid($"Крюк {name}: у renderer={renderer} нет sprite");

        Vector3 bounds = renderer.sprite.bounds.size;
        float spriteSize = Mathf.Max(bounds.x, bounds.y);
        float parentScale = renderer.transform.parent == null ? 1f :
            Mathf.Max(Mathf.Abs(renderer.transform.parent.lossyScale.x), Mathf.Abs(renderer.transform.parent.lossyScale.y));
        if (!GrappleMath.IsFinite(spriteSize) || !GrappleMath.IsFinite(parentScale) ||
            spriteSize <= Mathf.Epsilon || parentScale <= Mathf.Epsilon)
        {
            return StopInvalid($"Крюк {name}: renderer={renderer.name}, spriteSize={spriteSize}, parentScale={parentScale}");
        }

        scale = spriteSize * parentScale;

        return true;
    }

    private bool SetSize(SpriteRenderer renderer, float size)
    {
        if (!ValidateSize(renderer, out float spriteScale)) return false;

        float scale = size / spriteScale;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);

        return true;
    }

    private void OnDisable()
    {
        Clear();
    }
}
