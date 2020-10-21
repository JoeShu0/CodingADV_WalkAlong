using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System;
using System.Threading;

public class TileGen : MonoBehaviour
{
    public Shader tileShader;

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

    static int LODCount = 8;
    static float GridSize = 0.5f;
    static int GridCountPerTile = 24;//this value haveto be thr mul of 4 since the snapping requires it

    private Material[] LODMats = new Material[LODCount];
    private GameObject TileObj;
    private Mesh[] TileMeshes = new Mesh[(int)TileType.Count];

    private Material TileMat,TileMat2,TileMat4,TileMatY;

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





        /*
        GameObject goin = GenerateTile(10, 10, 11, 11);
        TileMat = new Material(tileShader);
        TileMat.SetFloat("_GridSize", 1.0f);
        TileMat.SetVector("_TransitionParam", new Vector4(10.0f, 10.0f, 5.0f, 5.0f));
        TileMat.SetVector("_CenterPos", gameObject.transform.position);
        goin.GetComponent<MeshRenderer>().sharedMaterial = TileMat;
        
        GameObject go00 = GenerateTile(9, 10, 10, 11);
        GameObject go00FX = GenerateTile(11, 10, 12, 11);

        goin.transform.parent = gameObject.transform;
        go00.transform.parent = gameObject.transform;
        go00FX.transform.parent = gameObject.transform;
        MeshRenderer TileMeshRender = go00.GetComponent<MeshRenderer>();
        MeshRenderer TileMeshRenderFX = go00FX.GetComponent<MeshRenderer>();
        TileMat = new Material(tileShader);
        TileMat.SetFloat("_GridSize", 1.0f);
        TileMat.SetVector("_TransitionParam", new Vector4(11.0f, 11.0f, 5.0f, 5.0f));
        TileMat.SetVector("_CenterPos", gameObject.transform.position);
        TileMeshRender.sharedMaterial = TileMat;
        TileMeshRenderFX.sharedMaterial = TileMat;
        

        GameObject goi1 = GameObject.Instantiate(goin, gameObject.transform);
        goin.GetComponent<MeshRenderer>().sharedMaterial = TileMat;
        goi1.GetComponent<MeshRenderer>().sharedMaterial = TileMat;

        goi1.transform.position = new Vector3(0, 0, 0);
        goin.transform.position = new Vector3(-10, 0, 0);

        //go00.transform.Rotate(new Vector3(0, 1, 0), 180);
        go00.transform.position = new Vector3(10, 0, 0);
        go00FX.transform.Rotate(new Vector3(0, 1, 0), 180);
        go00FX.transform.position = new Vector3(-10, 0, 10);
        //**
        GameObject go10 = GameObject.Instantiate(go00, gameObject.transform);
        GameObject go10FX = GameObject.Instantiate(go00FX, gameObject.transform);
        go10FX.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        go10.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        TileMat2 = new Material(tileShader);
        TileMat2.SetFloat("_GridSize", 2.0f);
        TileMat2.SetVector("_TransitionParam", new Vector4(22.0f,22.0f,10.0f,10.0f));
        TileMat2.SetVector("_CenterPos", gameObject.transform.position);
        go10.GetComponent<MeshRenderer>().sharedMaterial = TileMat2;
        go10FX.GetComponent<MeshRenderer>().sharedMaterial = TileMat2;

        go10.transform.position = new Vector3(20, 0, 0);
        go10FX.transform.position = new Vector3(-20, 0, 20);
        //**
        GameObject go20 = GameObject.Instantiate(go00, gameObject.transform);
        GameObject go20FX = GameObject.Instantiate(go00FX, gameObject.transform);
        go20.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
        go20FX.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
        TileMat4 = new Material(tileShader);
        TileMat4.SetFloat("_GridSize", 4.0f);
        TileMat4.SetVector("_TransitionParam", new Vector4(44.0f, 44.0f, 20.0f, 20.0f));
        TileMat4.SetVector("_CenterPos", gameObject.transform.position);
        go20.GetComponent<MeshRenderer>().sharedMaterial = TileMat4;
        go20FX.GetComponent<MeshRenderer>().sharedMaterial = TileMat4;

        go20.transform.position = new Vector3(40, 0, 0);
        go20FX.transform.position = new Vector3(-40, 0, 40);

        //Y axis~~~~
        GameObject go00Y = GameObject.Instantiate(go00, gameObject.transform);
        GameObject go01Y = GameObject.Instantiate(go00, gameObject.transform);
        
        TileMatY = new Material(tileShader);
        TileMatY.SetFloat("_GridSize", 1.0f);
        TileMatY.SetVector("_TransitionParam", new Vector4(11.0f, 11.0f, 5.0f, 5.0f));
        TileMatY.SetVector("_CenterPos", gameObject.transform.position);
        go00Y.GetComponent<MeshRenderer>().sharedMaterial = TileMat;
        go01Y.GetComponent<MeshRenderer>().sharedMaterial = TileMat;
        go00Y.transform.position = new Vector3(10, 0, 10);
        go01Y.transform.position = new Vector3(0, 0, 10);
        go00Y.transform.rotation = Quaternion.Euler(0,270,0);
        go01Y.transform.rotation = Quaternion.Euler(0, 270, 0);


        GenerateTile(TileType.Count, 10, 10);
        */
    }

    // Update is called once per frame
    void Update()
    {
        //TileMat.SetVector("_CenterPos", gameObject.transform.position);
        //TileMat2.SetVector("_CenterPos", gameObject.transform.position);
        //TileMat4.SetVector("_CenterPos", gameObject.transform.position);
        //TileMatY.SetVector("_CenterPos", gameObject.transform.position);
        /*
        My_Timer += Time.deltaTime;
        if (My_Timer > 1.0f)
        {
            TileObj.GetComponent<MeshFilter>().sharedMesh = TileMeshes[MI];
            MI =(MI+1)%9;
            My_Timer = 0.0f;
        }
        */
        //TileObj.GetComponent<MeshFilter>().sharedMesh = TileMeshes[7];

        foreach (Material TileMat in LODMats)
        {
            TileMat.SetVector("_CenterPos", gameObject.transform.position);
        }
        
    }

    GameObject GenerateTile(float tileSizeX, float tileSizeZ, int tileXPCount, int tileZPCount)
    {
        GameObject TileObj = new GameObject();
        TileObj.AddComponent<MeshFilter>();
        TileObj.AddComponent<MeshRenderer>();
        Mesh tilemesh  = new Mesh();

        tilemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[tileXPCount * tileZPCount];
        float incrementX = tileSizeX / (float)(tileXPCount - 1);
        float incrementZ = tileSizeZ / (float)(tileZPCount - 1);
        for (int x = 0; x < tileXPCount; x++)
        {
            for (int z = 0; z < tileZPCount; z++)
            {
                vertices[x * tileZPCount + z] = new Vector3(incrementX * x, 0.0f, incrementZ * z);
            }
        }
        tilemesh.vertices = vertices;

        int[] triangles = new int[(tileXPCount-1) * (tileZPCount - 1) * 6];
        int QuadOrient = 0;
        for (int x = 0; x < tileXPCount - 1; x++)
        {
            QuadOrient++;
            for (int z = 0; z < tileZPCount - 1; z++)
            {
                //quad num x*(HeightMapSize-1) + z
                int QuadNum = x * (tileZPCount - 1) + z;
                int TLPont = x * (tileZPCount) + z;
                if (QuadOrient % 2 == 0)
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + tileZPCount + 1;
                    triangles[QuadNum * 6 + 3] = TLPont;
                    triangles[QuadNum * 6 + 4] = TLPont + tileZPCount + 1;
                    triangles[QuadNum * 6 + 5] = TLPont + tileZPCount;
                }
                else 
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + tileZPCount;
                    triangles[QuadNum * 6 + 3] = TLPont + tileZPCount;
                    triangles[QuadNum * 6 + 4] = TLPont + 1;
                    triangles[QuadNum * 6 + 5] = TLPont + tileZPCount + 1;
                    
                }
                QuadOrient++;


            }
        }
        tilemesh.triangles = triangles;

        Vector2[] UVs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            UVs[i] = new Vector2(vertices[i].z / (float)tileSizeX, vertices[i].x / (float)tileSizeZ);
        }

        tilemesh.uv = UVs;

        TileObj.GetComponent<MeshFilter>().sharedMesh = tilemesh;

        return TileObj;
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

        Debug.Log(type);
        return tilemesh;
    }

    static GameObject BuildLOD(Mesh[] in_TileMeshes, float GridSize, int GridCount, int LODIndex, Shader TileShader, GameObject parent, ref Material LODMat, bool bIsLastLOD = false)
    {
        GameObject LOD = new GameObject("LOD_" + LODIndex.ToString());
        LOD.transform.parent = parent.transform;
        float LODScale = Mathf.Pow(2.0f, LODIndex);

        float TileSize = GridSize * (float)GridCount;
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
}
