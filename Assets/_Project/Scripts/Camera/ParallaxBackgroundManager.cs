using UnityEngine;

public class ParallaxBackgroundManager : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string layerName;
        public GameObject layerObject;
        public float zDepth;
    }

    [Header("Parallax Layers")]
    public Layer[] layers;

    void Start()
    {
        SetupLayers();
    }

    void SetupLayers()
    {
        foreach (Layer layer in layers)
        {
            if (layer.layerObject == null) continue;

            // Set Z position
            Vector3 pos = layer.layerObject.transform.position;
            pos.z = layer.zDepth;
            layer.layerObject.transform.position = pos;

            // Add ParallaxLayer if it doesn't exist
            if (layer.layerObject.GetComponent<ParallaxLayer>() == null)
            {
                layer.layerObject.AddComponent<ParallaxLayer>();
            }
        }
    }
}