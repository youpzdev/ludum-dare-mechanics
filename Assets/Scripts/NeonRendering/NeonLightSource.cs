using System;
using UnityEngine;

[DisallowMultipleComponent]
public class NeonLightSource : MonoBehaviour
{
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private Light localLight;
    [SerializeField] private NeonRenderSettings settings;

    public void Configure(SpriteRenderer renderer, Light light, NeonRenderSettings renderSettings)
    {
        if (renderer == null || light == null || renderSettings == null)
        {
            throw new ArgumentException($"Neon light configuration renderer={renderer} light={light} settings={renderSettings} object={name}");
        }
        source = renderer;
        localLight = light;
        settings = renderSettings;
        Refresh();
    }

    private void OnEnable()
    {
        if (source == null || localLight == null || settings == null)
        {
            throw new InvalidOperationException($"Neon light references missing object={name} renderer={source} light={localLight} settings={settings}");
        }
        Refresh();
    }

    private void LateUpdate()
    {
        if (source == null)
        {
            localLight.enabled = false;
            enabled = false;
            return;
        }
        Refresh();
    }

    private void Refresh()
    {
        bool visible = source.enabled && source.gameObject.activeInHierarchy && source.color.a > 0f;
        localLight.enabled = visible;
        localLight.color = new Color(source.color.r, source.color.g, source.color.b, 1f);
        localLight.intensity = settings.LightIntensity * Mathf.Clamp01(source.color.a);
        localLight.range = settings.LightRange;
        localLight.transform.position = source.bounds.center + Vector3.back * settings.LightOffset;
    }

    private void OnDisable()
    {
        if (localLight != null) localLight.enabled = false;
    }
}
