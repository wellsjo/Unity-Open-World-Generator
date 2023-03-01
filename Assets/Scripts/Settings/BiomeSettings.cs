using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    public AnimationCurve heightCurve;
    public float heightMultiplier;
    public float endDistance;
}