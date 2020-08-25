using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator
{

    // Start is called before the first frame update

    public float[] CalNoise(int MapSize)
    {
        
        float[] Height = new float[MapSize * MapSize];
        float MaxHeight = float.MinValue;
        float MinHeight = float.MaxValue;
        for (int x = 0; x < MapSize; x++)
        {
            for (int y = 0; y < MapSize; y++)
            {
                Height[x * MapSize + y] =(
                    Mathf.PerlinNoise(x / (float)MapSize * 1.0f, y / (float)MapSize * 1.0f) * 1.0f
                    + Mathf.PerlinNoise(x / (float)MapSize * 2.0f, y / (float)MapSize * 2.0f) * 0.5f
                    + Mathf.PerlinNoise(x / (float)MapSize * 4.0f, y / (float)MapSize * 4.0f) * 0.25f
                    + Mathf.PerlinNoise(x / (float)MapSize * 8.0f, y / (float)MapSize * 8.0f) * 0.125f
                    + Mathf.PerlinNoise(x / (float)MapSize * 16.0f, y / (float)MapSize * 16.0f) * 0.06125f
                    + Mathf.PerlinNoise(x / (float)MapSize * 32.0f, y / (float)MapSize * 32.0f) * 0.06125f * 0.25f
                    + Mathf.PerlinNoise(x / (float)MapSize * 64.0f, y / (float)MapSize * 64.0f) * 0.06125f * 0.06125f );

                MaxHeight = Mathf.Max(MaxHeight, Height[x * MapSize + y]);
                MinHeight = Mathf.Min(MinHeight, Height[x * MapSize + y]);
            }
        }

        for (int i = 0; i < Height.Length; i++)
        {
            Height[i] = (Height[i] - MinHeight) / (MaxHeight - MinHeight);

            Height[i] = Height[i] * Height[i];
        }

        return Height;
    }

    public float[,] CalNoise2d(int MapSize)
    {

        float[,] Height = new float[MapSize,MapSize];
        float MaxHeight = float.MinValue;
        float MinHeight = float.MaxValue;
        for (int x = 0; x < MapSize; x++)
        {
            for (int y = 0; y < MapSize; y++)
            {
                Height[x, y] = (
                    Mathf.PerlinNoise(x / (float)MapSize * 1.0f, y / (float)MapSize * 1.0f) * 1.0f
                    + Mathf.PerlinNoise(x / (float)MapSize * 2.0f, y / (float)MapSize * 2.0f) * 0.5f
                    + Mathf.PerlinNoise(x / (float)MapSize * 4.0f, y / (float)MapSize * 4.0f) * 0.25f
                    + Mathf.PerlinNoise(x / (float)MapSize * 8.0f, y / (float)MapSize * 8.0f) * 0.125f
                    + Mathf.PerlinNoise(x / (float)MapSize * 16.0f, y / (float)MapSize * 16.0f) * 0.06125f
                    + Mathf.PerlinNoise(x / (float)MapSize * 32.0f, y / (float)MapSize * 32.0f) * 0.06125f * 0.25f
                    + Mathf.PerlinNoise(x / (float)MapSize * 64.0f, y / (float)MapSize * 64.0f) * 0.06125f * 0.06125f);

                MaxHeight = Mathf.Max(MaxHeight, Height[x, y]);
                MinHeight = Mathf.Min(MinHeight, Height[x, y]);
            }
        }

        for (int i = 0; i < MapSize; i++)
            for(int n = 0;n < MapSize; n++)
            {
                Height[i,n] = (Height[i,n] - MinHeight) / (MaxHeight - MinHeight);

                Height[i, n] = Height[i, n] * Height[i, n];
            }

        return Height;
    }

}
