using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System;
using System.Threading;

public class TileGen : MonoBehaviour
{
    public Shader tileShader;
    public ComputeShader ShapeShader;

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

    struct WaveData
    {
        public float WaveLength;
        public float Amplitude;
        public Vector2 Direction;
    }

    static int LODCount = 8;
    static float GridSize = 0.5f;
    static int GridCountPerTile = 24;//this value haveto be thr mul of 4 since the snapping requires it
    static int RTSize = 512;
    static int WaveCount = 2;

    private Material[] LODMats = new Material[LODCount];
    private GameObject TileObj;
    private Mesh[] TileMeshes = new Mesh[(int)TileType.Count];

    private RenderTexture[] LODDisplaceMaps = new RenderTexture[LODCount];
    private int threadGroupX, threadGroupY;
    private int KIndex;

    private List<WaveData> WaveDatas = new List<WaveData>();
    private WaveData[] WDs = new WaveData[WaveCount];

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




        InitLODRTs(LODDisplaceMaps, RTSize);

        KIndex = ShapeShader.FindKernel("CSMain");
        //ShapeShader.SetTexture(KIndex, "Result", LODDisplaceMaps[0]);
        ShapeShader.SetInt("WaveCount", WaveCount);
        

        threadGroupX = Mathf.CeilToInt(RTSize / 32.0f);
        threadGroupY = Mathf.CeilToInt(RTSize / 32.0f);
        //ShapeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);

        WDs[0].WaveLength = 5.0f;
        WDs[0].Amplitude = 0.5f;
        WDs[0].Direction = new Vector2(1.0f, 0.0f);
        WDs[1].WaveLength = 2.0f;
        WDs[1].Amplitude = 0.0f;
        WDs[1].Direction = new Vector2(0.0f, 1.0f);

        

        //WaveBuffer.Release();

        for (int i = 0; i < LODDisplaceMaps.Length; i++)
        {
            LODMats[i].mainTexture = LODDisplaceMaps[i];
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        ComputeBuffer WaveBuffer = new ComputeBuffer(WaveCount, 16);
        WaveBuffer.SetData(WDs);
        ShapeShader.SetBuffer(KIndex, "WavesBuffer", WaveBuffer);

        for (int i = 0; i < LODDisplaceMaps.Length; i++)
        {
            ShapeShader.SetFloat("LODSize", GridSize * GridCountPerTile * 4 * (i + 1));
            ShapeShader.SetInt("LODIndex", i);
            ShapeShader.SetFloat("_Time", Time.time);
            ShapeShader.SetTexture(KIndex, "Result", LODDisplaceMaps[i]);
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
        float LODSize = TileSize * 4.0f * (LODIndex+1);
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

    static private void InitLODRTs(RenderTexture[] LODDisplaceMaps, int RTSize)
    {
        for (int i = 0; i< LODDisplaceMaps.Length; i++)
        {
            if (LODDisplaceMaps[i] != null)
                LODDisplaceMaps[i].Release();
            LODDisplaceMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODDisplaceMaps[i].enableRandomWrite = true;
            LODDisplaceMaps[i].wrapMode = TextureWrapMode.Repeat;
            LODDisplaceMaps[i].Create();
        }  
    }
}
