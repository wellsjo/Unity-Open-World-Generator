using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    public TerrainSettings terrainSettings;
    public VegetationSettings vegetationSettings;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
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
}

[System.Serializable]
public struct VegetationSettings
{
    public NoiseSettings noiseSettings;
    public float startHeight;
    public float endHeight;
    public GameObject treePrefab;
}