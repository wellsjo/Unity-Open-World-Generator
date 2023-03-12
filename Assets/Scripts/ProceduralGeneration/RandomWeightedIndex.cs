public static class RandomWeightedIndex
{
    public static int Get(float[] weights, System.Random rng)
    {
        if (weights == null || weights.Length == 0) return -1;

        float w;
        float total = 0f;
        int i;
        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (w >= 0f && !float.IsNaN(w)) total += weights[i];
        }

        // Get number between 0 and 1
        float r = (float)rng.Next(0, 100) / 100;
        float s = 0f;

        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (float.IsNaN(w) || w <= 0f) continue;

            s += w / total;
            if (s >= r) return i;
        }

        return -1;
    }
}