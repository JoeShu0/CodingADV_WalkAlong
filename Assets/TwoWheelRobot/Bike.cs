using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bike : MonoBehaviour
{
    public WheelCollider LWC, RWC;
    public Rigidbody RBD;
    public float WheelPower = 50;
    public Transform LW, RW;
    
    // Start is called before the first frame update
    void Start()
    {
        RBD.centerOfMass = new Vector3(0, -5, 0);
    }

    private void FixedUpdate()
    {
        //LW.rotation = LWC.
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
