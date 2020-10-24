using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveEquaTest : MonoBehaviour
{

    public ComputeShader WaveComputeShader;

    private RenderTexture WaveRenderTexture;
    private int KIndex, threadGroupX, threadGroupY;

    private float _timer = 0.0f;    // Start is called before the first frame update
    private Vector2 WaveOriginPos = new Vector2(0.5f, 0.5f);
    void Start()
    {
        int RTSize = 512;
        InitLODRTs(RTSize);

        KIndex = WaveComputeShader.FindKernel("CSMain");

        WaveComputeShader.SetTexture(KIndex, "Result", WaveRenderTexture);


        threadGroupX = Mathf.CeilToInt(RTSize / 32.0f);
        threadGroupY = Mathf.CeilToInt(RTSize / 32.0f);

        gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = WaveRenderTexture;
    }

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer > 0.5)
        {
            _timer = 0.0f;
            //WaveOriginPos += new Vector2(0.01f, 0.01f);
        }

        WaveComputeShader.SetVector("WaveOriginData", new Vector4(WaveOriginPos.x, WaveOriginPos.y, 0.01f, 0.0f));
        WaveComputeShader.SetFloat("_Time", Time.time);

        WaveComputeShader.Dispatch(KIndex, threadGroupX, threadGroupY, 1);

    }

    private void InitLODRTs(int RTSize)
    {

            if (WaveRenderTexture != null)
            WaveRenderTexture.Release();
            WaveRenderTexture = new RenderTexture(RTSize, RTSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            WaveRenderTexture.enableRandomWrite = true;
            WaveRenderTexture.wrapMode = TextureWrapMode.Repeat;
            WaveRenderTexture.Create();
    }
}
