using System;
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
        Initialize();
    }

    public void ConfigureContactExtras(SpriteRenderer glow, ParticleSystem burst)
    {
        Clear();
        contactGlow = glow;
        contactBurst = burst;
        ValidateExtras();
        Clear();
    }

    private void ValidateExtras()
    {
        if (contactGlow != null)
        {
            if (contactGlow.sprite == null)
            {
                throw new InvalidOperationException($"GrappleFeedback '{name}' glow '{contactGlow.name}' requires an authored sprite.");
            }
            ValidateExtraTransform(contactGlow.transform);
        }
        if (contactBurst != null)
        {
            ValidateExtraTransform(contactBurst.transform);
            ParticleSystem.MainModule main = contactBurst.main;
            if (main.loop || main.playOnAwake || main.simulationSpace != ParticleSystemSimulationSpace.World ||
                main.stopAction != ParticleSystemStopAction.None ||
                contactBurst.transform.childCount != 0 || contactBurst.subEmitters.enabled)
            {
                throw new InvalidOperationException($"GrappleFeedback '{name}' burst '{contactBurst.name}' must be a single authored nonlooping world-space system: loop={main.loop}, playOnAwake={main.playOnAwake}, simulationSpace={main.simulationSpace}, stopAction={main.stopAction}, children={contactBurst.transform.childCount}, subEmitters={contactBurst.subEmitters.enabled}.");
            }
        }
        if (contactGlow != null && contactBurst != null &&
            (contactGlow.transform == contactBurst.transform || contactGlow.transform.IsChildOf(contactBurst.transform) ||
            contactBurst.transform.IsChildOf(contactGlow.transform)))
        {
            throw new InvalidOperationException($"GrappleFeedback '{name}' glow '{contactGlow.name}' and burst '{contactBurst.name}' require independent transforms.");
        }
    }

    private void ValidateExtraTransform(Transform visual)
    {
        if (visual.GetComponent<Rigidbody2D>() != null || visual.GetComponent<Collider2D>() != null ||
            visual.GetComponent<Rigidbody>() != null || visual.GetComponent<Collider>() != null ||
            visual.childCount != 0 ||
            (aimMarker != null && (visual == aimMarker.transform || aimMarker.transform.IsChildOf(visual) || visual.IsChildOf(aimMarker.transform))) ||
            (contactCue != null && (visual == contactCue.transform || contactCue.transform.IsChildOf(visual) || visual.IsChildOf(contactCue.transform))) ||
            (rope != null && (visual == rope.transform || rope.transform.IsChildOf(visual))))
        {
            throw new InvalidOperationException($"GrappleFeedback '{name}' extra '{visual.name}' must be an independent visual leaf without physics components: children={visual.childCount}.");
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;

        if (settings == null || aimMarker == null || contactCue == null || rope == null ||
            aimMarker == contactCue || aimMarker.sprite == null || contactCue.sprite == null)
        {
            throw new InvalidOperationException($"GrappleFeedback '{name}' requires settings, two distinct authored SpriteRenderers with sprites and rope: settings={settings}, marker={aimMarker}, contact={contactCue}, rope={rope}.");
        }

        if (aimMarker.GetComponent<Rigidbody2D>() != null || contactCue.GetComponent<Rigidbody2D>() != null)
        {
            throw new InvalidOperationException($"GrappleFeedback '{name}' cannot move or scale a physics root: marker='{aimMarker.name}', contact='{contactCue.name}'.");
        }

        if (!settings.IsFeedbackValid)
        {
            throw new InvalidOperationException($"GrappleFeedback '{name}' has invalid settings '{settings.name}': availableSize={settings.AvailableMarkerSize}, blockedSize={settings.BlockedMarkerSize}, noTargetSize={settings.NoTargetMarkerSize}, contactSize={settings.ContactSize}, contactEndSize={settings.ContactEndSize}, contactDuration={settings.ContactDuration}, ropeAccentDuration={settings.RopeAccentDuration}, glowSize={settings.ContactGlowSize}, glowEndSize={settings.ContactGlowEndSize}, glowOpacity={settings.ContactGlowOpacity}.");
        }

        ValidateExtras();
        ropeStartColor = rope.startColor;
        ropeEndColor = rope.endColor;
        initialized = true;
        Clear();
    }

    public void ShowTarget(GrappleTarget target, float depth)
    {
        Initialize();
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
        SetSize(aimMarker, size);
        aimMarker.enabled = true;
    }

    public void ShowAttachment(Vector3 point)
    {
        Initialize();
        Clear();
        if (!isActiveAndEnabled) return;

        contactCue.transform.position = point;
        contactCue.color = settings.ContactColor;
        SetSize(contactCue, settings.ContactSize);
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
            SetSize(contactGlow, settings.ContactGlowSize);
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
            if (initialized)
            {
                Color glowColor = settings.ContactColor;
                glowColor.a *= settings.ContactGlowOpacity;
                contactGlow.color = glowColor;
                SetSize(contactGlow, settings.ContactGlowSize);
            }
        }
        if (contactBurst != null) contactBurst.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (aimMarker != null) aimMarker.enabled = false;
        if (contactCue != null)
        {
            contactCue.enabled = false;
            if (initialized)
            {
                contactCue.color = settings.ContactColor;
                SetSize(contactCue, settings.ContactSize);
            }
        }
        if (initialized && rope != null)
        {
            rope.startColor = ropeStartColor;
            rope.endColor = ropeEndColor;
        }
    }

    private static void SetSize(SpriteRenderer renderer, float size)
    {
        Vector3 bounds = renderer.sprite.bounds.size;
        float spriteSize = Mathf.Max(bounds.x, bounds.y);
        float parentScale = renderer.transform.parent == null ? 1f :
            Mathf.Max(Mathf.Abs(renderer.transform.parent.lossyScale.x), Mathf.Abs(renderer.transform.parent.lossyScale.y));
        if (!GrappleMath.IsFinite(spriteSize) || !GrappleMath.IsFinite(parentScale) ||
            spriteSize <= Mathf.Epsilon || parentScale <= Mathf.Epsilon)
        {
            throw new InvalidOperationException($"GrappleFeedback renderer '{renderer.name}' requires nonzero sprite bounds and parent scale: spriteSize={spriteSize}, parentScale={parentScale}.");
        }
        float scale = size / (spriteSize * parentScale);
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void OnDisable()
    {
        Clear();
    }
}
