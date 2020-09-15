using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IsosurfaceEffector
{
    public abstract ComputeBuffer Evaluate(Vector3Int volumeSize, ComputeBuffer points);
}
