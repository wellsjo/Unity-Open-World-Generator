using UnityEngine;
using System.Linq;


[CreateAssetMenu()]
public class MapSettings : UpdatableData
{
    public int seed;
    public Map.BorderType borderType;

    // number of terrain chunks to use per vertex
    // field is ignored if map border type is infinite
    public int fixedSize;
    public BiomeSettings biomeSettings;
    public MeshSettings meshSettings;
    public TextureSettings textureSettings;
    public LODInfo[] detailLevels;

    public const int MaxFixedSize = 11;

    public Vector2 range
    {
        get
        {
            int r = (int)Mathf.Floor((float)fixedSize / 2);
            return new Vector2(-r, r);
        }
    }

    public float MinHeight
    {
        get
        {
            float minHeight = float.MaxValue;
            for (int i = 0; i < biomeSettings.biomes.Length; i++)
            {
                Biome biome = biomeSettings.biomes[i];
                float minHeightForBiome = biome.heightMultiplier * biome.heightCurve.Evaluate(0);
                if (minHeightForBiome < minHeight)
                {
                    minHeight = minHeightForBiome;
                }
            }
            return minHeight;
        }
    }

    public float MaxHeight
    {
        get
        {
            float maxHeight = float.MinValue;
            for (int i = 0; i < biomeSettings.biomes.Length; i++)
            {
                Biome biome = biomeSettings.biomes[i];
                float maxHeightForBiome = biome.heightMultiplier * biome.heightCurve.Evaluate(0);
                if (maxHeightForBiome > maxHeight)
                {
                    maxHeight = maxHeightForBiome;
                }
            }
            return maxHeight;
        }
    }

    // Make sure fixed size is an odd square if border type is fixed
    // and make sure it's not too big.
    private void ValidateValues()
    {
        if (borderType == Map.BorderType.Infinite)
        {
            fixedSize = -1;
        }
        else if (fixedSize % 2 != 1)
        {
            if (fixedSize > 1)
            {
                fixedSize--;
            }
            else
            {
                fixedSize = 3;
            }
        }
        else if (fixedSize > MaxFixedSize)
        {
            fixedSize = MaxFixedSize;
        }
    }

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        this.ValidateValues();
        //noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif

}

[System.Serializable()]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;


    public float sqrVisibleDstThreshold
    {
        get
        {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}

[System.Serializable()]
public class MeshSettings
{

    public const int numSupportedLODs = 5;
    public const int numSupportedFlatshadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
    public const int numSupportedChunkSizes = 9;

    public float meshScale = 2.5f;
    public bool useFlatShading;

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, numSupportedFlatshadedChunkSizes - 1)]
    public int flatshadedChunkSizeIndex;


    // num verts per line of mesh rendered at LOD = 0. Includes the 2 extra verts that are excluded from final mesh, but used for calculating normals
    public int numVertsPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShading) ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;
        }
    }

    // Size of a terrain chunk
    public float meshWorldSize
    {
        get
        {
            return (numVertsPerLine - 3) * meshScale;
        }
    }


}
[System.Serializable()]
public class TextureSettings
{

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

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

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
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
    }

}