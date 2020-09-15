using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectorMetaball : IsosurfaceEffector
{
    private ComputeShader _shader;
    private float _radius;
    private Transform _transform;

    public EffectorMetaball(Transform pos, float radius)
    {
        _shader = (ComputeShader)Resources.Load("ComputeShaders/Metaball");
        _radius = radius;
        _transform = pos;
    }

    public override ComputeBuffer Evaluate(Vector3Int volumeSize, ComputeBuffer points)
    {
        if(_shader==null)
        {
            Debug.LogError("Shader for " + this.GetType() + " has been not found!");
            return null;
        }

        _shader.SetBuffer(0, "points", points);
        _shader.SetFloat("radius", _radius);
        float[] f = new float[3];
        f[0] = _transform.position.x;
        f[1] = _transform.position.y;
        f[2] = _transform.position.z;
        _shader.SetFloats("pos",f);
        _shader.SetInt("sizeX", volumeSize.x);
        _shader.SetInt("sizeY", volumeSize.y);
        _shader.SetInt("sizeZ", volumeSize.z);

        _shader.Dispatch(0, volumeSize.x, volumeSize.y, volumeSize.z);

        return points;
    }
}
