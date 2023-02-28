using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    public Biome[] biomes;
}

[System.Serializable]
public class Biome
{
    public NoiseSettings noiseSettings;
    public AnimationCurve heightCurve;
    public float heightMultiplier;
    public float endDistance;
}