using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    public TerrainSettings terrainSettings;
    public VegetationSettings vegetationSettings;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        terrainSettings.Validate();
        base.OnValidate();
    }
#endif

}

[System.Serializable]
public struct TerrainSettings
{
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

[System.Serializable]
public struct VegetationSettings
{
    public NoiseSettings noiseSettings;
    public float noiseThreshold;
    public float startHeight;
    public float endHeight;
    public GameObject treePrefab;
}