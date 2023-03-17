using UnityEngine;
using System.Linq;

[System.Serializable()]
public class TerrainSettings
{
    public LayerSettings layerSettings;
    public NoiseSettings noiseSettings;
    public AnimationCurve heightCurve;
    public float heightMultiplier;

    public void Validate()
    {
        if (noiseSettings.persistance == 0)
        {
            noiseSettings.persistance = 0.5f;
        }
        if (noiseSettings.lacunarity == 0)
        {
            noiseSettings.lacunarity = 2;
        }
        if (noiseSettings.octaves == 0)
        {
            noiseSettings.octaves = 4;
        }
        if (noiseSettings.scale == 0)
        {
            noiseSettings.scale = 20;
        }
        if (heightMultiplier == 0)
        {
            heightMultiplier = 30;
        }
        if (heightCurve == null)
        {
            heightCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        }
    }
}

[System.Serializable()]
public class LayerSettings
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    // Water plane will be applied to the start height of the second layer
    public GameObject waterPlane;
    public Layer[] layers;

    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColourStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    public Vector3 WaterPlanePosition
    {
        get
        {
            return new Vector3(0, layers[1].startHeight, 0);
        }
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

}

[System.Serializable()]
public class Layer
{
    public Texture2D texture;
    public Color tint;
    [Range(0, 1)]
    public float tintStrength;
    [Range(0, 1)]
    public float startHeight;
    [Range(0, 1)]
    public float blendStrength;
    public float textureScale;
    [Range(0, 1)]
    public float ObjectFrequency;
    // List of prefabs and settings to spawn
    public ObjectSettings[] layerObjectSettings;
}

// algorith to randomly place objects with weights assigned to each object that dictate how relatively common it is

[System.Serializable()]
public struct ObjectSettings
{
    [Range(0, 1)]
    public float density;
    public bool scatter;
    public GameObject prefab;
}