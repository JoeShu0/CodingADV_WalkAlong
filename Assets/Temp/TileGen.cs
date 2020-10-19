using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private Material TileMat, TileMat2, TileMat4, TileMatY;
    // Start is called before the first frame update
    void Start()
    {
        GameObject goin = GenerateTile(10, 10, 11, 11);

        GameObject go00 = GenerateTile(9, 10, 10, 11);
        GameObject go00FX = GenerateTile(11, 10, 12, 11);

        goin.transform.parent = gameObject.transform;
        go00.transform.parent = gameObject.transform;
        go00FX.transform.parent = gameObject.transform;
        MeshRenderer TileMeshRender = go00.GetComponent<MeshRenderer>();
        MeshRenderer TileMeshRenderFX = go00FX.GetComponent<MeshRenderer>();
        TileMat = new Material(tileShader);
        TileMat.SetFloat("_GridSize", 1.0f);
        TileMat.SetVector("_TransitionParam", new Vector4(10.0f, 10.0f, 5.0f, 5.0f));
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
        TileMat2.SetVector("_TransitionParam", new Vector4(20.0f,20.0f,10.0f,10.0f));
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
        TileMat4.SetVector("_TransitionParam", new Vector4(40.0f, 40.0f, 20.0f, 20.0f));
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
        TileMatY.SetVector("_TransitionParam", new Vector4(10.0f, 10.0f, 5.0f, 5.0f));
        TileMatY.SetVector("_CenterPos", gameObject.transform.position);
        go00Y.GetComponent<MeshRenderer>().sharedMaterial = TileMat;
        go01Y.GetComponent<MeshRenderer>().sharedMaterial = TileMat;
        go00Y.transform.position = new Vector3(10, 0, 10);
        go01Y.transform.position = new Vector3(0, 0, 10);
        go00Y.transform.rotation = Quaternion.Euler(0,270,0);
        go01Y.transform.rotation = Quaternion.Euler(0, 270, 0);


        GenerateTile(TileType.Count, 10, 10);
    }

    // Update is called once per frame
    void Update()
    {
        TileMat.SetVector("_CenterPos", gameObject.transform.position);
        TileMat2.SetVector("_CenterPos", gameObject.transform.position);
        TileMat4.SetVector("_CenterPos", gameObject.transform.position);
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
        Debug.Log(type);
        return new Mesh();
    }
}
