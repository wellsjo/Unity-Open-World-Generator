using UnityEngine;

[CreateAssetMenu()]
public class InfiniteMapSettings : UpdatableData
{
    public bool useFalloffPerChunk;
    public HeightMapSettings heightMapSettings;

    protected override void OnValidate()
    {
        // TODO do we need to validate noise settings?
        Debug.Log("InfiniteMapSettings OnValidate called");
        base.OnValidate();
    }
}
