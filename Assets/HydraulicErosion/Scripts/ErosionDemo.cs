using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ErosionDemo : MonoBehaviour
{
    [Header("Map Settings")]
    public int MapSize = 10;
    public int HeightMapSize = 256;
    public float HeightScale = 1.0f;

    private Renderer rd;
    private Texture2D HeightTex;
    private Mesh mesh;
    private Vector3[] vertices;

    private float[] HData;
    Erosion ED;


    int Counter = 0;
    float Timer = 0.0f;
    bool Finished = false;


    // Start is called before the first frame update
    void Start()
    {
        rd = GetComponent<Renderer>();

        ED = GetComponent<Erosion>();

        HeightMapGenerator HMG = new HeightMapGenerator();

        HData = HMG.CalNoise(HeightMapSize);




        GenerateMesh();

        UpdateMeshHeight();

        DisplayHeightMap();

        //Debug.Log("Data: " + HData[100] + " | " + HData[101]);


        
    }



    void Update()
    {
        
        //Debug.Log("DropCount: " + Counter);
        
        if(Counter <= 128 * 512)
        {
            for (int i = 0; i < 128; i++)
            {
                ED.Erode(ref HData, HeightMapSize, 100);
                Counter++;
            }
            Timer += Time.deltaTime;
        }
        else if(!Finished)
        {
            Debug.Log("Finished Time: " + Timer);
            UpdateMeshHeight();
            Finished = true;
        }
    }

    void DisplayHeightMap()
    {
        HeightTex = new Texture2D(HeightMapSize, HeightMapSize);

        rd.material.mainTexture = HeightTex;

        Color[] PixelData = new Color[HeightMapSize * HeightMapSize];

        for (int i = 0; i < HData.Length; i++)
        {
            //PixelData[i] = new Color(HData[i]* HData[i], HData[i]* HData[i], HData[i]* HData[i]);
            PixelData[i] = new Color(0.5f,0.5f,0.5f);
        }
        HeightTex.SetPixels(PixelData);
        HeightTex.Apply();
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

    void UpdateMeshHeight()
    {
        for(int i=0;i<vertices.Length;i++)
        {
            vertices[i].y = (HData[i]) * HeightScale;
        }

        mesh.vertices = vertices;

        mesh.RecalculateNormals();
    }
}

