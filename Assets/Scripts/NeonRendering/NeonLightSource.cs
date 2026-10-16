using UnityEngine;

[DisallowMultipleComponent]
public class NeonLightSource : MonoBehaviour
{
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private Light localLight;
    [SerializeField] private NeonRenderSettings settings;

    private bool failed;

    public void Configure(SpriteRenderer renderer, Light light, NeonRenderSettings renderSettings)
    {
        if (renderer == null || light == null || renderSettings == null)
        {
            Debug.LogError($"Не настроен свет '{name}': source={renderer}, light={light}, settings={renderSettings}", this);
            return;
        }
        source = renderer;
        localLight = light;
        settings = renderSettings;
        if (failed)
        {
            failed = false;
            enabled = true;
        }
        Refresh();
    }

    private void OnEnable()
    {
        if (ValidateReferences()) Refresh();
    }

    private void LateUpdate()
    {
        if (!ValidateReferences()) return;
        Refresh();
    }

    private bool ValidateReferences()
    {
        if (source != null && localLight != null && settings != null) return true;

        if (!failed)
            Debug.LogError($"Не обновлен свет '{name}': source={source}, light={localLight}, settings={settings}", this);
        failed = true;
        enabled = false;

        return false;
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
