using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsosurfacePoint
{
    public Vector3 vector3;
    public float value;

    public IsosurfacePoint(Vector3 pos, float val)
    {
        vector3 = pos;
        value = val;
    }

}
