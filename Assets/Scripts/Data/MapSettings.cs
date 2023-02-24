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

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif

}