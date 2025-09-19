namespace Playground.GameCatalog.Api.Tools;

public static class VectorUtils
{
    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null) return -1;
        if (a.Length != b.Length) return -1;

        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            na += (double)a[i] * a[i];
            nb += (double)b[i] * b[i];
        }
        var denom = Math.Sqrt(na) * Math.Sqrt(nb) + 1e-10;
        return dot / denom;
    }
}