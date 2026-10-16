using UnityEngine;

[CreateAssetMenu(fileName = "NeonRenderSettings", menuName = "Ludum Dare/Neon Render Settings")]
public class NeonRenderSettings : ScriptableObject
{
    [SerializeField] [Min(0f)] private float lightIntensity = 3f;
    [SerializeField] [Min(0.01f)] private float lightRange = 4f;
    [SerializeField] [Min(0.01f)] private float lightOffset = 0.8f;
    [SerializeField] [Min(0f)] private float bloomIntensity = 0.3f;
    [SerializeField] [Min(0f)] private float bloomThreshold = 1f;
    [SerializeField] [Range(0f, 1f)] private float bloomScatter = 0.55f;

    public float LightIntensity => lightIntensity;
    public float LightRange => lightRange;
    public float LightOffset => lightOffset;
    public float BloomIntensity => bloomIntensity;
    public float BloomThreshold => bloomThreshold;
    public float BloomScatter => bloomScatter;
}
