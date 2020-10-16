using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileGen : MonoBehaviour
{
    public Shader tileShader;


    private Material TileMat, TileMat2, TileMat4;
    // Start is called before the first frame update
    void Start()
    {
        GameObject go00 = GenerateTile(20, 21);

        go00.transform.parent = gameObject.transform;
        MeshRenderer TileMeshRender = go00.GetComponent<MeshRenderer>();
        TileMat = new Material(tileShader);
        TileMat.SetVector("_CenterPos", gameObject.transform.position);
        TileMeshRender.sharedMaterial = TileMat;

        //GameObject go01 = GameObject.Instantiate(go00, gameObject.transform);

        //go00.transform.position = new Vector3(-10, 0, 0);
        //go01.transform.position = new Vector3(10, 0, 0);

        GameObject go10 = GameObject.Instantiate(go00, gameObject.transform);
        go10.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        TileMat2 = new Material(tileShader);
        TileMat2.SetFloat("_GridSize", 2.0f);
        TileMat2.SetVector("_TransitionParam", new Vector4(20.0f,0.0f,30.0f,0.0f));
        TileMat2.SetVector("_CenterPos", gameObject.transform.position);
        go10.GetComponent<MeshRenderer>().sharedMaterial = TileMat2;
        GameObject go11 = GameObject.Instantiate(go10, gameObject.transform);

        //go00.transform.position = new Vector3(-20, 0, 40);
        //go01.transform.position = new Vector3(20, 0, 40);

        GameObject go20 = GameObject.Instantiate(go00, gameObject.transform);
        go10.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
        TileMat4 = new Material(tileShader);
        TileMat4.SetFloat("_GridSize", 4.0f);
        TileMat4.SetVector("_TransitionParam", new Vector4(60.0f, 0.0f, 60.0f, 0.0f));
        TileMat4.SetVector("_CenterPos", gameObject.transform.position);
        go10.GetComponent<MeshRenderer>().sharedMaterial = TileMat4;
    }

    // Update is called once per frame
    void Update()
    {
        TileMat.SetVector("_CenterPos", gameObject.transform.position);
        TileMat2.SetVector("_CenterPos", gameObject.transform.position);
        TileMat4.SetVector("_CenterPos", gameObject.transform.position);
    }

    GameObject GenerateTile(int tileSize, int tileXPCount)
    {
        GameObject TileObj = new GameObject();
        TileObj.AddComponent<MeshFilter>();
        TileObj.AddComponent<MeshRenderer>();
        Mesh tilemesh  = new Mesh();

        tilemesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[tileXPCount * tileXPCount];
        float increment = (float)tileSize / (float)(tileXPCount-1);
        for (int x = 0; x < tileXPCount; x++)
        {
            for (int z = 0; z < tileXPCount; z++)
            {
                vertices[x * tileXPCount + z] = new Vector3(increment * x, 0.0f, increment * z);
            }
        }
        tilemesh.vertices = vertices;

        int[] triangles = new int[(tileXPCount-1) * (tileXPCount - 1) * 6];
        int QuadOrient = 0;
        for (int x = 0; x < tileXPCount - 1; x++)
        {
            QuadOrient++;
            for (int z = 0; z < tileXPCount - 1; z++)
            {
                //quad num x*(HeightMapSize-1) + z
                int QuadNum = x * (tileXPCount - 1) + z;
                int TLPont = x * (tileXPCount) + z;
                if (QuadOrient % 2 == 0)
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + tileXPCount + 1;
                    triangles[QuadNum * 6 + 3] = TLPont;
                    triangles[QuadNum * 6 + 4] = TLPont + tileXPCount + 1;
                    triangles[QuadNum * 6 + 5] = TLPont + tileXPCount;
                }
                else 
                {
                    triangles[QuadNum * 6 + 0] = TLPont;
                    triangles[QuadNum * 6 + 1] = TLPont + 1;
                    triangles[QuadNum * 6 + 2] = TLPont + tileXPCount;
                    triangles[QuadNum * 6 + 3] = TLPont + tileXPCount;
                    triangles[QuadNum * 6 + 4] = TLPont + 1;
                    triangles[QuadNum * 6 + 5] = TLPont + tileXPCount + 1;
                    
                }
                QuadOrient++;


            }
        }
        tilemesh.triangles = triangles;

        Vector2[] UVs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            UVs[i] = new Vector2(vertices[i].z / (float)tileSize, vertices[i].x / (float)tileSize);
        }

        tilemesh.uv = UVs;

        TileObj.GetComponent<MeshFilter>().sharedMesh = tilemesh;

        return TileObj;
    }
}
