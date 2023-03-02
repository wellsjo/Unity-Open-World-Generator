using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    public AnimationCurve heightCurve;
    public float heightMultiplier;
    public float endDistance;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
    }
#endif

}