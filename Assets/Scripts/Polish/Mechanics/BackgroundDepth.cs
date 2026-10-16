using UnityEngine;

[DefaultExecutionOrder(400)]
public class BackgroundDepth : MonoBehaviour
{
    [SerializeField] private Transform sceneCamera;
    [SerializeField] private Transform[] layers;
    [SerializeField] private float[] depthFactors;

    private Vector3 cameraOrigin;
    private Vector3[] origins;
    private bool initialized;

    private void Start()
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
        if (sceneCamera == null || layers == null || depthFactors == null || layers.Length != depthFactors.Length)
        {
            Debug.LogError($"Фон: camera={sceneCamera}, layers={layers?.Length}, factors={depthFactors?.Length}", this);
            return false;
        }
        origins = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            Transform layer = layers[i];
            if (layer == null || layer.childCount != 0 ||
                layer.GetComponent<Collider2D>() != null || layer.GetComponent<Rigidbody2D>() != null ||
                layer.GetComponent<Collider>() != null || layer.GetComponent<Rigidbody>() != null ||
                !float.IsFinite(depthFactors[i]) || depthFactors[i] < 0f || depthFactors[i] > 1f)
            {
                Debug.LogError($"Фон: неверный layers[{i}]={layer}, depthFactor={depthFactors[i]}", this);
                return false;
            }
            for (int j = 0; j < i; j++)
            {
                if (layers[j] == layer)
                {
                    Debug.LogError($"Фон: слои {j} и {i} ссылаются на {layer.name}", this);
                    return false;
                }
            }
            origins[i] = layer.position;
        }
        cameraOrigin = sceneCamera.position;
        initialized = true;

        return true;
    }

    private void LateUpdate()
    {
        if (!initialized || !isActiveAndEnabled) return;
        if (sceneCamera == null)
        {
            Debug.LogError("Фон: потерян sceneCamera", this);
            enabled = false;
            return;
        }
        Vector3 offset = sceneCamera.position - cameraOrigin;
        offset.z = 0f;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
            {
                Debug.LogError($"Фон: потерян layers[{i}]", this);
                enabled = false;
                return;
            }
            layers[i].position = origins[i] + offset * depthFactors[i];
        }
    }

    private void OnDisable()
    {
        if (!initialized) return;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null) layers[i].position = origins[i];
        }
    }
}
