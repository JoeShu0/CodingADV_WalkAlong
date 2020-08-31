using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMarchObject : MonoBehaviour
{
    public enum ShapeType {Invalid, Sphere, Box};
    public ShapeType ShType;

    public enum OperationType { add, substruct, Intersection};
    public OperationType OpType;

    public Color baseColor = new Color(0.5f,0.5f,0.5f,1.0f);
}
