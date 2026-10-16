using System;
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
        if (sceneCamera == null || layers == null || depthFactors == null || layers.Length != depthFactors.Length)
        {
            throw new InvalidOperationException($"BackgroundDepth '{name}' requires sceneCamera and matching layers/depthFactors arrays: layers={layers?.Length}, factors={depthFactors?.Length}.");
        }
        origins = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            Transform layer = layers[i];
            if (layer == null || layer.childCount != 0 || layer.GetComponent<Collider2D>() != null || layer.GetComponent<Rigidbody2D>() != null || layer.GetComponent<Collider>() != null || layer.GetComponent<Rigidbody>() != null || !float.IsFinite(depthFactors[i]) || depthFactors[i] < 0f || depthFactors[i] > 1f)
            {
                throw new InvalidOperationException($"BackgroundDepth '{name}' requires visual leaf layer[{i}]={layer} without physics and depthFactor in [0,1], received={depthFactors[i]}.");
            }
            for (int j = 0; j < i; j++)
            {
                if (layers[j] == layer) throw new InvalidOperationException($"BackgroundDepth '{name}' duplicate layer indices={j},{i}, layer='{layer.name}'.");
            }
            origins[i] = layer.position;
        }
        cameraOrigin = sceneCamera.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        Vector3 offset = sceneCamera.position - cameraOrigin;
        offset.z = 0f;
        for (int i = 0; i < layers.Length; i++)
        {
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
