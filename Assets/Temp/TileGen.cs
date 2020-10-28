using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
//using System;
using System.Threading;


public class TileGen : MonoBehaviour
{
    public Shader tileShader;
    public ComputeShader ShapeShader;
    public Texture SkyTex;
    public Texture WaterDetailNormal;
    [Range(0.0f, 2.0f)]
    public float AnimWaveScale;

    enum TileType
    {
        Interior,
        FatX,
        SlimX,
        FatXSlimZ,
        SlimXFatZ,
        SlimXZ,
        FatXZ,
        FatXOuter,
        FatXZOuter,
        Count
    }

    [SerializeField]
    public struct WaveData
    {
        public float WaveLength;
        public float Steepness;
        public Vector2 Direction;
    }
    public WaveData[] WDs = new WaveData[WaveCount];

    public Vector2 WaveLengthRange = new Vector2(128.0f, 0.1f);
    public Vector2 SteepnessRange = new Vector2(0.1f, 0.001f);
    public float WaveWindAngle = 90.0f;
    public float[] WaveLengths;
    public float[] Steepnesses;
    public float[] DirAngleDegs;

    //Count for LOD rings
    static int LODCount = 8;
    //Min grid size
    static float GridSize = 0.1f;
    //grid count for each tile in standard, have to be the mul of 4 since the snapping requires it
    static int GridCountPerTile = 50;
    //RTSize effect rendertexture size (displace and normal) for each LOD, lower it will effect normalmap quality
    static int RTSize = 512;
    //WaveCount should be mul of 4 Since we are packing it into vectors
    static int WaveCount = 48;

    private Material[] LODMats = new Material[LODCount];
    private GameObject TileObj;
    private Mesh[] TileMeshes = new Mesh[(int)TileType.Count];

    private RenderTexture[] LODDisplaceMaps = new RenderTexture[LODCount];
    private RenderTexture[] LODNormalMaps = new RenderTexture[LODCount];
    private int threadGroupX, threadGroupY;
    private int KIndex;

    private List<WaveData> WaveDatas = new List<WaveData>();
    //private WaveData[] WDs = new WaveData[WaveCount];

    // Start is called before the first frame update
    void Start()
    {
        //string[] STileType = (string[])Enum.GetValues(typeof(TileType));
        
        
        for (int i = 0; i < (int)TileType.Count; i++)
        {
            TileMeshes[i] = GenerateTile((TileType)i, GridSize, GridCountPerTile);
        }

        GameObject[] LODS = new GameObject[LODCount];
        Material LODMat = null;

        for (int i = 0; i < LODCount-1; i++)
        {
            LODS[i] = BuildLOD(TileMeshes, GridSize, GridCountPerTile, i, tileShader, gameObject, ref LODMat);
            LODMats[i] = LODMat;
        }
        //Gen the outer LOD
        int lastLODIndex = LODCount - 1;
        LODS[lastLODIndex] = BuildLOD(TileMeshes, GridSize, GridCountPerTile, lastLODIndex, tileShader, gameObject, ref LODMat, true);
        LODMats[lastLODIndex] = LODMat;




        InitLODRTs(LODDisplaceMaps, LODNormalMaps, RTSize);

        KIndex = ShapeShader.FindKernel("CSMain");
        //ShapeShader.SetTexture(KIndex, "Result", LODDisplaceMaps[0]);
        ShapeShader.SetInt("WaveCount", WaveCount);
        

        threadGroupX = Mathf.CeilToInt(RTSize / 32.0f);
        threadGroupY = Mathf.CeilToInt(RTSize / 32.0f);
        //ShapeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);
        
        WDs[0].WaveLength = 100.0f;
        WDs[0].Steepness = 0.04f;
        WDs[0].Direction = new Vector2(0.35f, 0.5f);
        WDs[1].WaveLength = 50.0f;
        WDs[1].Steepness = 0.06f;
        WDs[1].Direction = new Vector2(0.15f, 0.95f);
        WDs[2].WaveLength = 25.0f;
        WDs[2].Steepness = 0.08f;
        WDs[2].Direction = new Vector2(-0.35f, -0.65f);

        WDs[3].WaveLength = 12.0f;
        WDs[3].Steepness = 0.1f;
        WDs[3].Direction = new Vector2(0.7f, 0.5f);
        WDs[4].WaveLength = 6.0f;
        WDs[4].Steepness = 0.11f;
        WDs[4].Direction = new Vector2(-0.35f, 0.85f);
        WDs[5].WaveLength = 3.0f;
        WDs[5].Steepness = 0.06f;
        WDs[5].Direction = new Vector2(-0.65f, -0.15f);

        WDs[6].WaveLength = 1.5f;
        WDs[6].Steepness = 0.15f;
        WDs[6].Direction = new Vector2(0.5f, 0.5f);
        WDs[7].WaveLength = 0.8f;
        WDs[7].Steepness = 0.10f;
        WDs[7].Direction = new Vector2(0.45f, 0.95f);
        WDs[8].WaveLength = 4f;
        WDs[8].Steepness = 0.05f;
        WDs[8].Direction = new Vector2(-0.45f, 0.15f);
        
        WDs[9].WaveLength = 1.0f;
        WDs[9].Steepness = 0.08f;
        WDs[9].Direction = new Vector2(0.25f, 0.5f);
        WDs[10].WaveLength = 0.6f;
        WDs[10].Steepness = 0.04f;
        WDs[10].Direction = new Vector2(-0.55f, 0.65f);
        WDs[11].WaveLength = 0.4f;
        WDs[11].Steepness = 0.01f;
        WDs[11].Direction = new Vector2(0.25f, 0.15f);

        GenerateWaves(WaveCount, ref WaveLengths, ref Steepnesses, ref DirAngleDegs, WaveLengthRange, SteepnessRange, WaveWindAngle);

        for (int i = 0; i < WaveCount; i++)
        {
            WDs[i].WaveLength = WaveLengths[i];
            WDs[i].Steepness = Steepnesses[i] * AnimWaveScale;
            WDs[i].Direction = new Vector2((float)Mathf.Cos(Mathf.Deg2Rad*DirAngleDegs[i]), (float)Mathf.Sin(Mathf.Deg2Rad * DirAngleDegs[i]));
        }

        //WaveBuffer.Release();
        //LODMats[0].SetTexture("_LODDisTex", LODDisplaceMaps[1]);
        //LODMats[0].SetFloat("_AddUVScale", 0.5f);

        //LODMats[1].SetTexture("_LODDisTex", LODDisplaceMaps[1]);



        for (int i = 0; i < LODDisplaceMaps.Length; i++)
        {
            if (i + 1 < LODDisplaceMaps.Length)
            {
                LODMats[i].SetTexture("_LODDisTex", LODDisplaceMaps[i]);
                LODMats[i].SetTexture("_NextLODDisTex", LODDisplaceMaps[i+1]);
                LODMats[i].SetTexture("_LODNTex", LODNormalMaps[i]);
                LODMats[i].SetTexture("_NextLODNTex", LODNormalMaps[i + 1]);
            }
            else 
            {
                LODMats[i].SetTexture("_LODDisTex", LODDisplaceMaps[i]);
                LODMats[i].SetTexture("_NextLODDisTex", LODDisplaceMaps[i]);
                LODMats[i].SetTexture("_LODNTex", LODNormalMaps[i]);
                LODMats[i].SetTexture("_NextLODNTex", LODNormalMaps[i]);
            }
            LODMats[i].SetTexture("_SkyTex", SkyTex);
            LODMats[i].SetTexture("_DetailN", WaterDetailNormal);
        }

    }

    // Update is called once per frame
    void Update()
    {
        ComputeBuffer WaveBuffer = new ComputeBuffer(WaveCount, 16);

      

        WaveBuffer.SetData(WDs);
        ShapeShader.SetBuffer(KIndex, "WavesBuffer", WaveBuffer);
        ShapeShader.SetFloats("CenterPos", new float[] {gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z});
        //ShapeShader.SetVector("CenterPos", WaveBuffer);

        for (int i = 0; i < LODDisplaceMaps.Length; i++)
        {
            ShapeShader.SetFloat("LODSize", GridSize * GridCountPerTile * 4 * Mathf.Pow(2,i));
            ShapeShader.SetInt("LODIndex", i);
            ShapeShader.SetFloat("_Time", Time.time);
            ShapeShader.SetTexture(KIndex, "Displace", LODDisplaceMaps[i]);
            ShapeShader.SetTexture(KIndex, "Normal", LODNormalMaps[i]);
            ShapeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);
        }

        WaveBuffer.Release();
        //ShapeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);


        foreach (Material TileMat in LODMats)
        {
            TileMat.SetVector("_CenterPos", gameObject.transform.position);
        }
        
    }

   

    Mesh GenerateTile(TileType type, float GridSize, int GridCount)
    {
        //make sure the grid size is fixed bwteen tiles si that the snap works
        //the generated mesh should have the pivot on the center of the interior type. and consistent through out all tiles
        //So when placing the tiles we can just use symmetry~

        //try build a interior tile
        Mesh tilemesh = new Mesh();
        tilemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        int GridCountX = GridCount;
        int GridCountZ = GridCount;
        float TileSizeX = GridSize * GridCountX;
        float TileSizeZ = GridSize * GridCountZ;

        bool bIsOutertile = false;
        switch(type)
        { 
            case TileType.Interior:
                break;
            case TileType.FatX:
                GridCountX++;
                break;
            case TileType.SlimX:
                GridCountX--;
                break;
            case TileType.FatXSlimZ:
                GridCountX++;
                GridCountZ--;
                break;
            case TileType.SlimXFatZ:
                GridCountX--;
                GridCountZ++;
                break;
            case TileType.SlimXZ:
                GridCountX--;
                GridCountZ--;
                break;
            case TileType.FatXZ:
                GridCountX++;
                GridCountZ++;
                break;
            case TileType.FatXOuter:
                GridCountX++;
                bIsOutertile = true;
                break;
            case TileType.FatXZOuter:
                GridCountX++;
                GridCountZ++;
                bIsOutertile = true;
                break;
            case TileType.Count:
                Debug.LogError("Invalide TileType!");
                return null;
        }

        Vector3[] vertices = new Vector3[(GridCountX + 1) * (GridCountZ + 1)];
        //float incrementX = tileSizeX / (float)(tileXPCount - 1);
        //float incrementZ = tileSizeZ / (float)(tileZPCount - 1);
        for (int x = 0; x < GridCountX + 1; x++)
        {
            for (int z = 0; z < GridCountZ + 1; z++)
            {
                vertices[x * (GridCountZ + 1) + z] = new Vector3(GridSize * x, 0.0f, GridSize * z) 
                    + new Vector3(-0.5f * TileSizeX, 0, -0.5f * TileSizeX);
            }
        }
        if(bIsOutertile)
            for(int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].x - 0.5f * TileSizeX > 0.1f )
                    vertices[i].x *= 50.0f;
                if (vertices[i].z - 0.5f * TileSizeZ > 0.1f)
                    vertices[i].z *= 50.0f;
            }
        tilemesh.vertices = vertices;

        int[] triangles = new int[(GridCountX) * (GridCountZ) * 6];
        int QuadOrient = 0;
        for (int x = 0; x < GridCountX; x++)
        {
            for (int z = 0; z < GridCountZ; z++)
            {
                //quad num x*(HeightMapSize-1) + z
                int QuadNum = x * (GridCountZ) + z;
                int TLPont = x * (GridCountZ + 1) + z;
                if (QuadOrient % 2 == 0)
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + GridCountZ + 2;
                    triangles[QuadNum * 6 + 3] = TLPont;
                    triangles[QuadNum * 6 + 4] = TLPont + GridCountZ + 2;
                    triangles[QuadNum * 6 + 5] = TLPont + GridCountZ + 1;
                }
                else
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + GridCountZ + 1;
                    triangles[QuadNum * 6 + 3] = TLPont + GridCountZ + 1;
                    triangles[QuadNum * 6 + 4] = TLPont + 1;
                    triangles[QuadNum * 6 + 5] = TLPont + GridCountZ + 2;

                }
                QuadOrient++;


            }
            QuadOrient += GridCountZ % 2 + 1;
        }
        tilemesh.triangles = triangles;

        Vector2[] UVs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            UVs[i] = new Vector2(vertices[i].z / (float)TileSizeX, vertices[i].x / (float)TileSizeZ);
        }

        tilemesh.uv = UVs;
        
        //Intentionly make larger bound to avoid fructrum culling when moving vertex~
        tilemesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(TileSizeX * 1.35f, 10.0f, TileSizeZ * 1.35f));
        //Debug.Log(type);
        return tilemesh;
    }

    static GameObject BuildLOD(Mesh[] in_TileMeshes, float GridSize, int GridCount, 
                                int LODIndex, Shader TileShader, GameObject parent, 
                                ref Material LODMat, bool bIsLastLOD = false)
    {
        GameObject LOD = new GameObject("LOD_" + LODIndex.ToString());
        LOD.transform.parent = parent.transform;
        float LODScale = Mathf.Pow(2.0f, LODIndex);

        float TileSize = GridSize * (float)GridCount;
        float LODSize = TileSize * 4.0f * Mathf.Pow(2,LODIndex);
        int TileCount = 0;
        Vector2[] TilesOffsets;
        TileType[] TilesType;
        int[] TilesRotate;
        if (LODIndex == 0)
        {
            TileCount = 16;
            TilesOffsets = new[] {new Vector2(-1.5f, 1.5f), new Vector2(-0.5f, 1.5f), new Vector2(0.5f, 1.5f), new Vector2(1.5f, 1.5f),
                                  new Vector2(-1.5f, 0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1.5f, 0.5f),
                                  new Vector2(-1.5f, -0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(1.5f, -0.5f),
                                  new Vector2(-1.5f, -1.5f), new Vector2(-0.5f, -1.5f), new Vector2(0.5f, -1.5f), new Vector2(1.5f, -1.5f) };
            TilesType = new[] {TileType.SlimXFatZ,    TileType.SlimX,    TileType.SlimX,    TileType.SlimXZ,
                               TileType.FatX,         TileType.Interior, TileType.Interior, TileType.SlimX,
                               TileType.FatX,         TileType.Interior, TileType.Interior, TileType.SlimX,
                               TileType.FatXZ,        TileType.FatX,     TileType.FatX,     TileType.FatXSlimZ };
            TilesRotate = new[] { -90,    -90,    -90,    0,
                                  180,    0,      0,      0,
                                  180,    0,      0,      0,
                                  180,    90,     90,     90};
        }
        else 
        {
            TileCount = 12;
            TilesOffsets = new[] {new Vector2(-1.5f, 1.5f), new Vector2(-0.5f, 1.5f), new Vector2(0.5f, 1.5f), new Vector2(1.5f, 1.5f),
                                  new Vector2(-1.5f, 0.5f),                                                      new Vector2(1.5f, 0.5f),
                                  new Vector2(-1.5f, -0.5f),                                                     new Vector2(1.5f, -0.5f),
                                  new Vector2(-1.5f, -1.5f), new Vector2(-0.5f, -1.5f), new Vector2(0.5f, -1.5f), new Vector2(1.5f, -1.5f) };

            if(!bIsLastLOD)
                TilesType = new[] {TileType.SlimXFatZ,    TileType.SlimX,    TileType.SlimX,    TileType.SlimXZ,
                               TileType.FatX,                                                TileType.SlimX,
                               TileType.FatX,                                                TileType.SlimX,
                               TileType.FatXZ,        TileType.FatX,     TileType.FatX,     TileType.FatXSlimZ };
            else
                TilesType = new[] {TileType.FatXZOuter,    TileType.FatXOuter,    TileType.FatXOuter,    TileType.FatXZOuter,
                               TileType.FatXOuter,                                                      TileType.FatXOuter,
                               TileType.FatXOuter,                                                      TileType.FatXOuter,
                               TileType.FatXZOuter,        TileType.FatXOuter,     TileType.FatXOuter,     TileType.FatXZOuter };

            TilesRotate = new[] {   -90,    -90,    -90,    0,
                                    180,                    0,
                                    180,                    0,
                                    180,    90,     90,     90 };

        }
        

        Material TileMat = new Material(TileShader);
        TileMat.SetFloat("_GridSize", GridSize * LODScale);
        TileMat.SetVector("_TransitionParam", new Vector4(TileSize*1.25f, TileSize * 1.25f, 0.5f* TileSize, 0.5f * TileSize) * LODScale);
        TileMat.SetVector("_CenterPos", LOD.transform.position);
        TileMat.SetFloat("_LODSize", LODSize);

        for (int i = 0; i < TileCount; i++)
        {
            GameObject CTile = new GameObject(TilesType[i].ToString() + "_" + i.ToString());
            CTile.transform.parent = LOD.transform;
            CTile.AddComponent<MeshFilter>();
            CTile.AddComponent<MeshRenderer>();
            CTile.GetComponent<MeshFilter>().sharedMesh = in_TileMeshes[(int)TilesType[i]];
            CTile.transform.localPosition = new Vector3(TilesOffsets[i].x * TileSize, 0, TilesOffsets[i].y * TileSize);
            CTile.transform.localRotation = Quaternion.Euler(0, TilesRotate[i], 0);

            
            CTile.GetComponent<MeshRenderer>().sharedMaterial = TileMat;  
        }

        LODMat = TileMat;
        LOD.transform.localScale = new Vector3(LODScale, 1, LODScale);
        return LOD;
    }

    static private void InitLODRTs(RenderTexture[] LODDisplaceMaps, RenderTexture[] LODNormalMaps, int RTSize)
    {
        for (int i = 0; i< LODDisplaceMaps.Length; i++)
        {
            if (LODDisplaceMaps[i] != null)
                LODDisplaceMaps[i].Release();
            LODDisplaceMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODDisplaceMaps[i].enableRandomWrite = true;
            LODDisplaceMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODDisplaceMaps[i].Create();

            if (LODNormalMaps[i] != null)
                LODNormalMaps[i].Release();
            LODNormalMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODNormalMaps[i].enableRandomWrite = true;
            LODNormalMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODNormalMaps[i].Create();
        }  
    }
    /*
    void UpdateAnimWaveData()
    {
        for(int i = 0; i<WDs.Length; i++)
        {
            WDs[i].Steepness *= AnimWaveScale;
        }
    }
    */

    static void GenerateWaves(int WaveCount, ref float[] WaveLengths, ref float[] Steepnesses, ref float[] DirAngleDegs, Vector2 WaveLengthRange, Vector2 SteepnessRange, float WaveWindAngle)
    {
        if (WaveLengths != null) { WaveLengths = new float[WaveCount]; };
        if (Steepnesses != null) { Steepnesses = new float[WaveCount]; };
        if (DirAngleDegs != null) { DirAngleDegs = new float[WaveCount]; };

        float rnd = Random.Range(0.0f, 1.0f);

        float GroupCount = Mathf.Log(WaveCount, 2);

        int WavePerGroup = Mathf.FloorToInt(WaveCount / GroupCount);

        float WaveLengthIncre = (WaveLengthRange.x - WaveLengthRange.y) / (float)WaveCount;

        for (int i = 0; i < WaveCount; i++)
        {
            WaveLengths[i] = WaveLengthIncre * i + WaveLengthIncre * Random.Range(0.0f, 1.0f);
            Steepnesses[i] = Mathf.Lerp(SteepnessRange.x, SteepnessRange.y, Random.Range(0.0f, 1.0f));
            DirAngleDegs[i] = Random.Range(-1.0f, 1.0f) * WaveWindAngle;
        }
    }
}
