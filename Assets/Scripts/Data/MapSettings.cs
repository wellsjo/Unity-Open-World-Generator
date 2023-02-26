using UnityEngine;

[CreateAssetMenu()]
public class MapSettings : UpdatableData
{
    public int seed;
    public Map.BorderType borderType;

    // number of terrain chunks to use per vertex
    // field is ignored if map border type is infinite
    public int fixedSize;
    //public bool useFalloff;

    public NoiseSettings noiseSettings;
    public MeshSettings meshSettings;
    public float heightMultiplier;
    public AnimationCurve heightCurve;
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

    public float minHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(1);
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
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif

}

[System.Serializable]
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


[System.Serializable]
public class NoiseSettings
{
    //public Noise.NormalizeMode normalizeMode;

    public float scale = 50;

    public int octaves = 6;
    [Range(0, 1)]
    public float persistance = .6f;
    public float lacunarity = 2;

    //public int seed;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}