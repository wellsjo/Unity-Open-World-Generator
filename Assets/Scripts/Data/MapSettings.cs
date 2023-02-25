using UnityEngine;

[CreateAssetMenu()]
public class MapSettings : UpdatableData
{
    public Map.BorderType borderType;

    // number of terrain chunks to use per vertex
    // field is ignored if map border type is infinite
    public int fixedSize;
    public bool useFalloff;

    public NoiseSettings noiseSettings;
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