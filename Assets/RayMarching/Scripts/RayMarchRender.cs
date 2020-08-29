using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class RayMarchRender : MonoBehaviour
{
    public ComputeShader RayMarchShader;
    
    private RenderTexture _target;

    private Camera _camera;

    private GameObject[] LRayMarchObjects;
    private RayMarchObject[] LRayMarchobjProperties;

    [SerializeField]
    struct ObjData
    {
        public int type;
        public Vector3 origin;
        public Vector3 upvector;
        public float size;
    };

    private List<ObjData> MarchData = new List<ObjData>();
    
    private void OnRenderImage(RenderTexture source, RenderTexture Desitnation)
    {
        Render(Desitnation);
    }

    private void Render(RenderTexture Desitnation)
    {
        InitRenderTexture();

        int KIndex = RayMarchShader.FindKernel("CSMain");

        RayMarchShader.SetTexture(KIndex, "Result", _target);
        RayMarchShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayMarchShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);

        GatherRenderObject();

        if (MarchData.Count == 0)
        {
            Debug.Log("Zero Data");
            return;
        }

        ComputeBuffer DataBuffer = new ComputeBuffer(MarchData.Count, 32);
        DataBuffer.SetData(MarchData);
        RayMarchShader.SetBuffer(0, "RayObjectsBuffer", DataBuffer);
        
        /*
        ComputeBuffer TestBuffer = new ComputeBuffer(2, 4);
        float[] TempData = new float[2];
        TempData[0] = MarchData[0].origin.z;
        TempData[1] = MarchData[0].origin.x;
        TestBuffer.SetData(TempData);
        RayMarchShader.SetBuffer(0, "TestBuffer", TestBuffer);
       */

        int threadGroupX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayMarchShader.Dispatch(0, threadGroupX, threadGroupY, 1);

        DataBuffer.Release();
        //TestBuffer.Release();

        Graphics.Blit(_target, Desitnation);
    }

    private void InitRenderTexture()
    {
        if(_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            if(_target !=  null)
            {
                _target.Release();
            }

            _target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
        }
    }

    
    void Awake()
    {
        _camera = gameObject.GetComponent<Camera>();
    }
    void Start()
    {
        LRayMarchObjects = GameObject.FindGameObjectsWithTag("RayMarchObjects");
        LRayMarchobjProperties = new RayMarchObject[LRayMarchObjects.Length];
        for (int i = 0; i < LRayMarchObjects.Length; i++)
        {
            LRayMarchobjProperties[i] = LRayMarchObjects[i].GetComponent<RayMarchObject>();
        }
    }
    void GatherRenderObject()
    {
        if (LRayMarchObjects == null)
            return;
        if (LRayMarchObjects.Length == 0)
        {
            Debug.Log("Zero Object");
            return;
        }



        for (int i = 0; i < LRayMarchObjects.Length; i++)
        {
            ObjData OD = new ObjData();
            OD.type = (int)LRayMarchobjProperties[i].Type;
            OD.origin = LRayMarchObjects[i].transform.position;
            OD.upvector = LRayMarchObjects[i].transform.up;
            OD.size = LRayMarchObjects[i].transform.localScale.x;

            MarchData.Clear();
            MarchData.Add(OD);
        }

        
    }
}
