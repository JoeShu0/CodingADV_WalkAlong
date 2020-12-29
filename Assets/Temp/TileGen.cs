using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.Experimental.GlobalIllumination;

public class TileGen : MonoBehaviour
{
    public Shader tileShader;
    public ComputeShader ShapeShader;
    public Texture SkyTex;
    public Texture WaterDetailNormal;

    public float AnimWindDirDegs = 0.0f;
    public float WaveWindAngleRange = 90.0f;

    public Vector2 WaveLengthRange = new Vector2(128.0f, 0.25f);

    [Range(0.0f, 3.0f)]
    public float[] AnimWaveAmpMul = new float[8];

    public int OceanScale = 1;

    //Camera
    private Camera MainCam;
    private float ArcTanHalfFOV = 2.0f;
    private float CameraHeight0 = 10.0f;


    //WaveData for debug
    private float[] WaveLengths = new float[WaveCount];
    private float[] Amplitudes = new float[WaveCount];
    private float[] DirAngleDegs = new float[WaveCount];

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
        public float Amplitude;
        public float Speed;
        public Vector2 Direction;
    }
    private WaveData[] WDs = new WaveData[WaveCount];


    //Backlogs
    /* 
     * 
     * 3. Try only compute the suitable wave length for each LOD and add them to the higher LOD# set back to just reduce the wavecuont for LODs
     * 
     */


    //**************Params for OceanGeo *************** 
    //Count for LOD rings
    static int LODCount = 8;
    //Min grid size
    static float GridSize = 0.2f;
    //grid count for each tile in standard, have to be the mul of 4 since the snapping requires it
    static int GridCountPerTile = 80;
    //RTSize effect rendertexture size (displace and normal) for each LOD, lower it will effect normalmap quality
    static int RTSize = 512;
    //WaveCount should be mul of 4 Since we are packing it into vectors
    //And We are getting each LOD to compute diff wave length so we fix the WaveCount to 64=8*8
    static int WaveCount = 128;
    //LODMaterials
    private Material[] LODMats = new Material[LODCount];
    //LOD game object
    private GameObject TileObj;
    //All th tile types
    private Mesh[] TileMeshes = new Mesh[(int)TileType.Count];
    //anim wave render texture
    private RenderTexture[] LODDisplaceMaps = new RenderTexture[LODCount];
    private RenderTexture[] LODNormalMaps = new RenderTexture[LODCount];
    //Anim wave computeshader params
    private int threadGroupX, threadGroupY;
    private int KIndex;
    private int ID_BWavelength;

    private Light DirectionalLight;


    //private List<WaveData> WaveDatas = new List<WaveData>();
    //private WaveData[] WDs = new WaveData[WaveCount];

    // Start is called before the first frame update
    void Start()
    {
        //Get the camera
        MainCam = GameObject.FindObjectOfType<Camera>();
        //MainCam.depthTextureMode = DepthTextureMode.Depth;
        
        //string[] STileType = (string[])Enum.GetValues(typeof(TileType));
        //get a refernce to the directional light 
        DirectionalLight = GameObject.FindObjectOfType<Light>();
        Vector3 LightDir = DirectionalLight.transform.forward;

        //generate all th tile types 
        for (int i = 0; i < (int)TileType.Count; i++)
        {
            TileMeshes[i] = GenerateTile((TileType)i, GridSize, GridCountPerTile);
        }

        //Generate all LODs and Material for these LODs 
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

        //Initialize rendertextures
        InitLODRTs(LODDisplaceMaps, LODNormalMaps, RTSize);
        Debug.Log(LODDisplaceMaps[0].antiAliasing);

        //Initialize AnimWave compute shader
        KIndex = ShapeShader.FindKernel("CSMain");
        ShapeShader.SetInt("WaveCount", WaveCount);
        threadGroupX = Mathf.CeilToInt(RTSize / 32.0f);
        threadGroupY = Mathf.CeilToInt(RTSize / 32.0f);


        //generate Anim waves data
        GenerateWaves(WaveCount, ref WaveLengths, ref Amplitudes, ref DirAngleDegs,
                        WaveLengthRange, WaveWindAngleRange, AnimWindDirDegs, AnimWaveAmpMul);

        //assign wave data to the buffer
        for (int i = 0; i < WaveCount; i++)
        {
            WDs[i].WaveLength = WaveLengths[i];
            WDs[i].Amplitude = Amplitudes[i];
            WDs[i].Speed = Mathf.Sqrt(9.8f / 2.0f / 3.14159f * WaveLengths[i]);
            WDs[i].Direction = new Vector2((float)Mathf.Cos(Mathf.Deg2Rad*DirAngleDegs[i]), (float)Mathf.Sin(Mathf.Deg2Rad * DirAngleDegs[i]));
        }


        //Assign RTs to each LODMaterials
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


            LODMats[i].SetVector("_SunDir", new Vector4(LightDir.x, LightDir.y, LightDir.z, 0.0f)); 
        }

    }

    // Update is called once per frame
    void Update()
    {


        AdjustOceanForCamera();

        RenderOceanDIsNorRTs();

    }

    void AdjustOceanForCamera()
    {
        if (MainCam.transform.hasChanged)
        {
            //Debug.Log("Changed!!");
            MainCam.transform.hasChanged = false;
            int CameraHeightLevel = Mathf.FloorToInt(Mathf.Sqrt(Mathf.Abs(MainCam.transform.position.y) / CameraHeight0)) + 1;

            ScaleOcean(CameraHeightLevel);

            float CameraFEstimateDist = MainCam.transform.position.y * ArcTanHalfFOV;
            Vector3 Cal_OceanCenter = MainCam.transform.forward * CameraFEstimateDist + MainCam.transform.position;

            Vector3 CurPos = gameObject.transform.position;
            gameObject.transform.position = new Vector3(Cal_OceanCenter.x, CurPos.y, Cal_OceanCenter.z);

            foreach (Material TileMat in LODMats)
            {
                TileMat.SetVector("_CenterPos", Cal_OceanCenter);
            }

        }
    }


    void ScaleOcean(int in_OceanScale)
    {
        OceanScale = in_OceanScale;
        gameObject.transform.localScale = new Vector3(in_OceanScale, in_OceanScale, in_OceanScale);
        foreach (Material TileMat in LODMats)
        {
            TileMat.SetInt("_OceanScale", in_OceanScale);
        }
    }

    void RenderOceanDIsNorRTs()
    {
        //Generating displacement&Normal rendertexure with compute shader
        ComputeBuffer WaveBuffer = new ComputeBuffer(WaveCount, 20);

        ShapeShader.SetFloats("CenterPos", new float[] { gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z });

        

        for (int i = LODDisplaceMaps.Length-1; i >= 0; i--)
        {
            //trying to reduce the WaveCount computed for far LODs
            int WaveCountPLOD = WaveCount / LODCount;
            //ShapeShader.SetInt("WaveCount", WaveCountPLOD);
            //WaveBuffer.SetData(WDs.Skip(i* WaveCountPLOD).Take(WaveCountPLOD).ToArray());

            ShapeShader.SetInt("WaveCount", WaveCount - WaveCountPLOD * i);
            //WaveBuffer.SetData(WDs);
            WaveBuffer.SetData(WDs.Skip(i * WaveCountPLOD).ToArray());
            ShapeShader.SetBuffer(KIndex, "WavesBuffer", WaveBuffer);

            ShapeShader.SetFloat("LODSize", GridSize * GridCountPerTile * 4 * Mathf.Pow(2, i) * OceanScale);
            ShapeShader.SetInt("LODIndex", i);
            ShapeShader.SetFloat("_Time", Time.time);

            if (i != LODDisplaceMaps.Length - 1)
            {
                ShapeShader.SetTexture(KIndex, "BaseDisplace", LODDisplaceMaps[i + 1]);
                ShapeShader.SetTexture(KIndex, "BaseNormal", LODNormalMaps[i + 1]);
            }
            else
            {
                ShapeShader.SetTexture(KIndex, "BaseDisplace", LODDisplaceMaps[i]);
                ShapeShader.SetTexture(KIndex, "BaseNormal", LODNormalMaps[i]);
            }
            ShapeShader.SetTexture(KIndex, "Displace", LODDisplaceMaps[i]);
            ShapeShader.SetTexture(KIndex, "Normal", LODNormalMaps[i]);
            ShapeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);
        }
        WaveBuffer.Release();
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
        tilemesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(TileSizeX * 1.8f, 20.0f, TileSizeZ * 1.8f));
        //Debug.Log(type);
        return tilemesh;
    }

    static GameObject BuildLOD(Mesh[] in_TileMeshes, float GridSize, int GridCount, 
                                int LODIndex, Shader TileShader, GameObject parent, 
                                ref Material LODMat, bool bIsLastLOD = false)
    {
        //Build th LOD gameobject using tiles, each LOD have 4 tile along XZ axis
        //1st LOD is solid, other LODs are just rings, the Last LOD has the skrit 
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
        //create all the render textures for Anim Wave displacement and normal
        for (int i = 0; i< LODDisplaceMaps.Length; i++)
        {
            if (LODDisplaceMaps[i] != null)
                LODDisplaceMaps[i].Release();
            LODDisplaceMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODDisplaceMaps[i].enableRandomWrite = true;
            LODDisplaceMaps[i].antiAliasing = 1;
            //LODDisplaceMaps[i].bindTextureMS = true;
            LODDisplaceMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODDisplaceMaps[i].filterMode = FilterMode.Trilinear;
            LODDisplaceMaps[i].Create();

            if (LODNormalMaps[i] != null)
                LODNormalMaps[i].Release();
            LODNormalMaps[i] = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            LODNormalMaps[i].enableRandomWrite = true;
            LODNormalMaps[i].wrapMode = TextureWrapMode.Clamp;
            LODNormalMaps[i].filterMode = FilterMode.Trilinear;
            LODNormalMaps[i].Create();
        }  
    }
   

    static void GenerateWaves(int WaveCount, ref float[] WaveLengths, ref float[] Amplitudes, ref float[] DirAngleDegs,
                              Vector2 WaveLengthRange, float WaveWindAngle, float WindAngle, float[] AnimWaveAmpMul)
    {
        //Generate waves using Log ditribution, No steepness difference in diff wavelength!!!.
        //feels unnature, but it is OK for neow

        int GroupCount = Mathf.FloorToInt(Mathf.Log(Mathf.FloorToInt(WaveLengthRange.x), 2)) + 1;
        int WavePerGroup = Mathf.FloorToInt(WaveCount / GroupCount);

        int index = 0;

        float G_MaxWL = Mathf.Pow(2, GroupCount-1);

        for (int i = 0; i < GroupCount; i++)
        {
            float Max_WaveLength = Mathf.Pow(2, i);
            float Min_WaveLength = i == 0 ? WaveLengthRange.y : Mathf.Pow(2, i - 1);
            for (int n = 0; n < WavePerGroup; n++)
            {
                index = i * WavePerGroup + n;
                //Debug.Log(index);
                if (index < WaveCount)
                {
                    WaveLengths[index] = Mathf.Lerp(Min_WaveLength, Max_WaveLength, UnityEngine.Random.Range(0.1f, 1.0f));
                    Amplitudes[index] = WaveLengths[index] * 0.005f * AnimWaveAmpMul[i];
                    DirAngleDegs[index] = UnityEngine.Random.Range(-1.0f, 1.0f) * WaveWindAngle + WindAngle;
                    //DirX[index] = (float)Mathf.Cos(Mathf.Deg2Rad * DirAngleDegs[index]);
                    //DirZ[index] = (float)Mathf.Sin(Mathf.Deg2Rad * DirAngleDegs[index]);
                }
                
            }
        }

        if (index < WaveCount-1)
        {
            Debug.Log("waves not filled");
            for (int n = index+1; n < WaveCount; n++)
            {
                WaveLengths[n] = Mathf.Lerp(WaveLengthRange.x, WaveLengthRange.x, UnityEngine.Random.Range(0.0f, 1.0f));
                Amplitudes[n] = 0.0f;
                DirAngleDegs[n] = 0.0f;
            }
        }

        //Array.Sort(WaveLengths);
    }

    static Vector4[] FArray2VectorBatch(float[] floatarray)
    {
        //should check the array count to be mul of 4
        Assert.IsTrue(floatarray.Length % 4 == 0);
        int batchCount = floatarray.Length / 4;
        Vector4[] BatchedFoat = new Vector4[batchCount];
        for (int i = 0; i < batchCount; i++)
        {
            int batchindex = i * 4;
            BatchedFoat[i] = new Vector4(floatarray[batchindex], floatarray[batchindex + 1], floatarray[batchindex + 2], floatarray[batchindex + 3]);
        }
        return BatchedFoat;
    }
}
