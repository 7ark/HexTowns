using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public static class Utility
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        var buffer = source.ToList();
        for (int i = 0; i < buffer.Count; i++)
        {
            int j = Random.Range(i, buffer.Count);
            yield return buffer[j];

            buffer[j] = buffer[i];
        }
    }
}
