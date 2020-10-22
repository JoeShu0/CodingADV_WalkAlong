using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GerstnerTest : MonoBehaviour
{
    public float _WaveLength = 2.0f;
    public float _Amplitude = 1.0f;
    public float _Speed = 1.0f;
    public Vector3 _Direction = new Vector3(1.0f, 0.0f, 0.0f);
    
    
    // Start is called before the first frame update
    private Vector3 OriginalPos;
    void Start()
    {
        OriginalPos = gameObject.transform.position;
        _Direction = _Direction.normalized;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = new Vector3(0, 0, 0);
        float theta = Time.time + OriginalPos.x;
        float sin = Mathf.Sin(theta);
        float cos = Mathf.Cos(theta);
        float Wx = _Amplitude * Mathf.Cos(Vector3.Dot(OriginalPos, _Direction)* (2 / _WaveLength) + Time.time * (_Speed * 2 / _WaveLength)) * _Direction.x;
        float Wz = _Amplitude * Mathf.Cos(Vector3.Dot(OriginalPos, _Direction) * (2 / _WaveLength) + Time.time * (_Speed * 2 / _WaveLength)) * _Direction.z;
        float Wy = _Amplitude * Mathf.Sin(Vector3.Dot(OriginalPos, _Direction) * (2 / _WaveLength) + Time.time * (_Speed * 2 / _WaveLength));
        pos = new Vector3(OriginalPos.x + Wx, OriginalPos.y + Wy, OriginalPos.z + Wz);
        gameObject.transform.position = pos;
    }
}
