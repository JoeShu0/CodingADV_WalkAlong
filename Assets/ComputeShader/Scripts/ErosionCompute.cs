using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErosionCompute : MonoBehaviour
{
    const int HeightMapSize = 512;
    const int P_maxPatStep = 128;

    const int ThreadGroup = 8;
    const int ThresdNum = 1024;

    const int MaxRadius = 4;
    const int MaxInfluencedPoints = (MaxRadius*2) * (MaxRadius*2);

    [Header("Map Settings")]
    public int MapSize = 10;
    
    public float HeightScale = 1.0f;

    public ComputeShader ErosionShader;

    private Renderer rd;
    private Texture2D HeightTex;
    private Mesh mesh;
    private Vector3[] vertices;

    private float[] HData;
    Erosion ED;

    [Header("Erosion Settings")]
    [Range(0, 1)]
    public float P_inertia = 0.4f;
    [Range(0, 1)]
    public float P_minslope = 0.01f;
    public float P_capacity = 2.0f;
    [Range(0, 1)]
    public float P_evaporation = 0.05f;
    [Range(0, 1)]
    public float P_deposition = 0.1f;
    [Range(0, 1)]
    public float P_erosion = 0.01f;
    public float P_eradius = 4.0f;
    public float P_dradius = 4.0f;
    public float P_gravity = -9.8f;

    int Counter = 0;
    float Timer = 0.0f;
    bool Finished = false;

    [SerializeField]
    struct OffsetPair
    {
        public int Index;
        public float Offset;
    };

    
    float[] HeightData1d = new float[HeightMapSize * HeightMapSize];
    float[] RandomDropPos = new float[ThreadGroup * ThresdNum * 2];
    //OffsetPair[] OffsetData = new OffsetPair[ThreadGroup * ThresdNum * P_maxPatStep * MaxInfluencedPoints];


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start!");

        rd = GetComponent<Renderer>();

        ED = GetComponent<Erosion>();

        HeightMapGenerator HMG = new HeightMapGenerator();

        HeightData1d = HMG.CalNoise(HeightMapSize);

        

        GenerateMesh();

        UpdateMeshHeight();

        RunShader();

        UpdateMeshHeight();
    }



    void Update()
    {
        //for(int i=0;i<5;i++)
        RunShader();
        UpdateMeshHeight();
        /* 
        if(Counter <= 128 * 512)
        {
            RunShader();
            //UpdateMeshHeight();
            Counter += ThreadGroup * ThresdNum;

            Timer += Time.deltaTime;
        }
        else if(!Finished)
        {
            Debug.Log("Finished Time: " + Timer);
            UpdateMeshHeight();
            Finished = true;
        }
         */
        
    }


    void RunShader()
    {
        //Temp offset data, 16 point max edited by each step each thread
        //OffsetPair[] OffsetData = new OffsetPair[ThreadGroup * ThresdNum * P_maxPatStep * MaxInfluencedPoints];
        /* 
        for(int i = 0;i<OffsetData.Length;i++)
        {
            OffsetData[i].Index = 0;
            OffsetData[i].Offset =0.0f;
        }
        */
        //random drop position
        for(int i=0;i<RandomDropPos.Length;i++)
        {
            RandomDropPos[i] = Random.Range((float)MaxRadius, (float)(HeightMapSize-MaxRadius-1)) ;
        }
        //
        ComputeBuffer RPosBuffer = new ComputeBuffer(RandomDropPos.Length, 4);
        RPosBuffer.SetData(RandomDropPos);
        ComputeBuffer HeightBuffer = new ComputeBuffer(HeightData1d.Length, 4);
        HeightBuffer.SetData(HeightData1d);
        //ComputeBuffer OffsetBuffer = new ComputeBuffer(OffsetData.Length, 8);
        //OffsetBuffer.SetData(OffsetData);

        ErosionShader.SetBuffer(0, "HeightData", HeightBuffer);
        //ErosionShader.SetBuffer(0, "OffsetBuffer", OffsetBuffer);
        ErosionShader.SetBuffer(0, "RPosBuffer", RPosBuffer);

        ErosionShader.SetFloat("P_inertia",P_inertia);
        ErosionShader.SetFloat("P_minslope",P_minslope);
        ErosionShader.SetFloat("P_capacity",P_capacity);
        ErosionShader.SetFloat("P_evaporation",P_evaporation);
        ErosionShader.SetFloat("P_deposition",P_deposition);
        ErosionShader.SetFloat("P_erosion",P_erosion);
        ErosionShader.SetFloat("P_eradius",P_eradius);
        ErosionShader.SetFloat("P_dradius",P_dradius);
        ErosionShader.SetFloat("P_gravity",P_gravity);
       
        ErosionShader.Dispatch(0, ThreadGroup, 1, 1);

        //OffsetBuffer.GetData(OffsetData);
        HeightBuffer.GetData(HeightData1d);

        //OffsetBuffer.Release();
        HeightBuffer.Release();
        RPosBuffer.Release();
        /* 
        for(int i=0; i<OffsetData.Length;i++ )
        {
            if(OffsetData[i].Offset == 0)
                continue;
            if(OffsetData[i].Index < 0)
            {
                Debug.Log("Location: "+ i + " Data: " + OffsetData[i].Index + " || " + OffsetData[i].Offset);
                break;
            }
                
            HeightData1d[OffsetData[i].Index] += OffsetData[i].Offset;
            OffsetData[i].Index = 0;
            OffsetData[i].Offset = 0.0f;
        }
        //Debug.Log(HeightData1d[0]); 
        */
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
        for (int i = 0; i < HeightMapSize; i++)
        {
            for (int n = 0; n < HeightMapSize; n++)
            {

                //vertices[i * MapSize + n].y = (HeightData2d[i, n]) * HeightScale;
                vertices[i * HeightMapSize + n].y = (HeightData1d[i* HeightMapSize + n]) * HeightScale;
            }
        }

        mesh.vertices = vertices;

        mesh.RecalculateNormals();
    }
}
