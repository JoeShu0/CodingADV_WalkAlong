using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveEquaTest : MonoBehaviour
{

    public ComputeShader WaveComputeShader;

    private RenderTexture WaveRenderTexture;
    private RenderTexture Prev_WaveRenderTexture;
    private int KIndex, threadGroupX, threadGroupY;

    private float _timer = 0.0f;    // Start is called before the first frame update
    private Vector2 WaveOriginPos = new Vector2(0.5f, 0.5f);
    void Start()
    {
        int RTSize = 512;
        InitLODRTs(RTSize);

        KIndex = WaveComputeShader.FindKernel("CSMain");

        //WaveComputeShader.SetTexture(KIndex, "Result", WaveRenderTexture);


        threadGroupX = Mathf.CeilToInt(RTSize / 32.0f);
        threadGroupY = Mathf.CeilToInt(RTSize / 32.0f);

        gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = WaveRenderTexture;

        //int initializeRT = WaveComputeShader.FindKernel("Initialize");
        //WaveComputeShader.SetTexture(initializeRT, "Result", WaveRenderTexture);
        //WaveComputeShader.Dispatch(initializeRT, threadGroupX, threadGroupY, 1);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer > 0.5)
        {
            _timer = 0.0f;
            //WaveOriginPos += new Vector2(0.02f, 0.02f);
        }

        WaveComputeShader.SetTexture(KIndex, "PrevResult", Prev_WaveRenderTexture);
        WaveComputeShader.SetTexture(KIndex, "Result", WaveRenderTexture);

        WaveComputeShader.SetVector("WaveOriginData", new Vector4(WaveOriginPos.x, WaveOriginPos.y, 0.01f, 0.0f));
        WaveComputeShader.SetFloat("_Time", Time.time);
        WaveComputeShader.SetFloat("_DeltaTime", Time.deltaTime);

        WaveComputeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);

        Graphics.CopyTexture(WaveRenderTexture, Prev_WaveRenderTexture);
    }

    private void InitLODRTs(int RTSize)
    {

        if (WaveRenderTexture != null)
            WaveRenderTexture.Release();
        WaveRenderTexture = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        WaveRenderTexture.enableRandomWrite = true;
        WaveRenderTexture.wrapMode = TextureWrapMode.Clamp;
        WaveRenderTexture.Create();

        if (Prev_WaveRenderTexture != null)
            Prev_WaveRenderTexture.Release();
        Prev_WaveRenderTexture = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Prev_WaveRenderTexture.enableRandomWrite = true;
        Prev_WaveRenderTexture.wrapMode = TextureWrapMode.Clamp;
        Prev_WaveRenderTexture.Create();

    }       

    
}
