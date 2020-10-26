using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CPUWaveEquation : MonoBehaviour
{
    [Header("Map Settings")]
    public int MapSize = 10;
    public int HeightMapSize = 256;

    private Renderer rd;
    private Texture2D HeightTex;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] Wavevertices;

    private float[] HData;


   

    // Start is called before the first frame update
    void Start()
    {
        rd = GetComponent<Renderer>();

        GenerateMesh();

        InitializeMeshHeight();

        Wavevertices = new Vector3[vertices.Length];
        for (int i = 0; i < Wavevertices.Length; i++)
        {
            Wavevertices[i] = new Vector3(0.0f, 0.0f, 0.0f);
        }

        for (int i = 0; i < Wavevertices.Length; i++)
        {
            float y = i / HeightMapSize;
            float x = i % HeightMapSize;
            if (Mathf.Abs(x - 128)<2 && Mathf.Abs(y - 128) < 2)
            Wavevertices[i].y = 0.1f;
        }

        

    }



    void Update()
    {
        UpdateData();

        UpdateMeshHeight();

    }


    void GenerateMesh()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        vertices = new Vector3[HeightMapSize * HeightMapSize];
        float increment = (float)MapSize / (float)HeightMapSize;
        for (int x = 0; x < HeightMapSize; x++)
        {
            for (int z = 0; z < HeightMapSize; z++)
            {
                vertices[x * HeightMapSize + z] = new Vector3(increment * x, 0.0f, increment * z);
            }
        }
        mesh.vertices = vertices;

        int[] triangles = new int[(HeightMapSize - 1) * (HeightMapSize - 1) * 6];
        for (int x = 0; x < HeightMapSize - 1; x++)
        {
            for (int z = 0; z < HeightMapSize - 1; z++)
            {
                //quad num x*(HeightMapSize-1) + z
                int QuadNum = x * (HeightMapSize - 1) + z;
                int TLPont = x * HeightMapSize + z;
                triangles[QuadNum * 6 + 0] = TLPont;
                triangles[QuadNum * 6 + 1] = TLPont + 1;
                triangles[QuadNum * 6 + 2] = TLPont + HeightMapSize + 1;
                triangles[QuadNum * 6 + 3] = TLPont;
                triangles[QuadNum * 6 + 4] = TLPont + HeightMapSize + 1;
                triangles[QuadNum * 6 + 5] = TLPont + HeightMapSize;
            }
        }
        mesh.triangles = triangles;

        Vector2[] UVs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            UVs[i] = new Vector2(vertices[i].z / (float)MapSize, vertices[i].x / (float)MapSize);
        }

        mesh.uv = UVs;
    }

    void UpdateData()
    {
        for (int i = 0; i < Wavevertices.Length; i++)
        {
            float pe = Wavevertices[i].x;
            float ps = Wavevertices[i].y;

            int ex = i + 1 > Wavevertices.Length-1 ? i : i + 1;
            int emx = i - 1 < 0 ? i : i - 1;
            int ey = i + HeightMapSize > Wavevertices.Length-1 ? i : i + HeightMapSize;
            int emy = i - HeightMapSize < 0 ? i : i - HeightMapSize;

            float Eex = Wavevertices[ex].x;
            float Eemx = Wavevertices[emx].x;
            float Eey = Wavevertices[ey].x;
            float Eemy = Wavevertices[emy].x;

            float ne = pe + ps + 0.1f * (Eex + Eemx + Eey + Eemy - 4.0f * pe);
            float ns = ne - pe;

            ne *= 0.99f;

            Wavevertices[i].y = ns;
            Wavevertices[i].x = ne;
        }
    }

    void InitializeMeshHeight()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = 0.0f;
        }

        mesh.vertices = vertices;

        mesh.RecalculateNormals();
    }

    void UpdateMeshHeight()
    {
        

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = Wavevertices[i].y;
        }

        mesh.vertices = vertices;

        mesh.RecalculateNormals();
    }
}
