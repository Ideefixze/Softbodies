using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

struct Triangle
{
#pragma warning disable 649 // disable unassigned variable warning
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }
}

public class IsosurfaceVolume : MonoBehaviour
{
    [SerializeField]
    private int _sizeX;
    [SerializeField]
    private int _sizeY;
    [SerializeField]
    private int _sizeZ;
    [SerializeField]
    private float _spaceBetweenPoints;
    [SerializeField]
    private float _defaultValue;

    private IsosurfacePoint[] _points;
    private List<IsosurfaceEffector> _effectors;

    //Buffers
    private ComputeBuffer _triangleBuffer;
    private ComputeBuffer _pointsBuffer;
    private ComputeBuffer _triCountBuffer;
    private ComputeBuffer _argsBuffer;

    [SerializeField] private Material _drawBuffer;

    [SerializeField]
    ComputeShader _marchingShader;
    [SerializeField]
    ComputeShader _fixupArgsCount;
    public int GetVolume()
    {
        return _sizeX * _sizeY * _sizeZ;
    }
    private void InitializeVolume(float defaultVal)
    {
        _effectors = new List<IsosurfaceEffector>();
        _points = new IsosurfacePoint[GetVolume()];

        for (int i = 0; i<_sizeX;i++)
        {
            for (int j = 0; j < _sizeY; j++)
            {
                for (int k = 0; k < _sizeZ; k++)
                {
                    _points[(i * _sizeY + j) * _sizeZ + k] = new IsosurfacePoint(transform.position + new Vector3(i, j, k) * _spaceBetweenPoints, defaultVal);
                }
            }
        }
    }

    private void PrepareBuffers()
    {
        int numPoints = GetVolume();
        int numVoxelsPerAxisX = _sizeX - 1;
        int numVoxelsPerAxisY = _sizeY - 1;
        int numVoxelsPerAxisZ = _sizeZ - 1;
        int numVoxels = numVoxelsPerAxisX * numVoxelsPerAxisY * numVoxelsPerAxisZ;
        int maxTriangleCount = numVoxels * 5;

        ReleaseBuffers();
        _triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3,ComputeBufferType.Append);
        _pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        _triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        float[] data = new float[4 * numPoints];
        int k = 0;
        for(int i = 0; i<4*numPoints;i+=4)
        {
            data[i] = _points[k].vector3.x;
            data[i+1] = _points[k].vector3.y;
            data[i+2] = _points[k].vector3.z;
            data[i+3] = _points[k].value;
            k++;
        }
        _pointsBuffer.SetData(data);

        foreach(IsosurfaceEffector ie in _effectors)
        {
            ie.Evaluate(new Vector3Int(_sizeX, _sizeY, _sizeZ), _pointsBuffer);
        }

        
    }

    void ReleaseBuffers()
    {
        if (_triangleBuffer != null)
        {
            _triangleBuffer.Release();
            _pointsBuffer.Release();
            _triCountBuffer.Release();
            _argsBuffer.Release();
        }
    }

    private void CreateMesh()
    {
        PrepareBuffers();

        _triangleBuffer.SetCounterValue(0);
        _marchingShader.SetBuffer(0, "points", _pointsBuffer);
        _marchingShader.SetBuffer(0, "triangles", _triangleBuffer);
        _marchingShader.SetInt("sizeX", _sizeX);
        _marchingShader.SetInt("sizeY", _sizeY);
        _marchingShader.SetInt("sizeZ", _sizeZ);
        _marchingShader.SetFloat("isoLevel", 0f);

        
        _marchingShader.Dispatch(0, _sizeX - 1, _sizeY - 1, _sizeZ - 1);

        //Get number of triangles in the triangle buffer
        GraphicsBuffer.CopyCount(_triangleBuffer, _triCountBuffer, 0);
        int[] triCountArray = { 0 };
        _triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];
        Debug.Log(numTris);

        _argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        int[] args = new int[] {0, 1, 0, 0 };
        _argsBuffer.SetData(args);

        // Copy generated count
        ComputeBuffer.CopyCount(_triangleBuffer, _argsBuffer, 0);

        // Invoke very simple args fixup as generated count was triangles, not verts 
        _fixupArgsCount.SetBuffer(0, "DrawCallArgs", _argsBuffer);
        _fixupArgsCount.Dispatch(0, 1, 1, 1);

        // Draw mesh using indirect args buffer
        _drawBuffer.SetPass(0);
        _drawBuffer.SetBuffer("_Buffer", _triangleBuffer);

        Graphics.DrawProceduralIndirect(_drawBuffer, new Bounds(transform.position, transform.lossyScale),
                                        MeshTopology.Points, _argsBuffer);

        //argsBuffer.Dispose();

        //---------------------
        Triangle[] tris = new Triangle[numTris];
        _triangleBuffer.GetData(tris, 0, 0, numTris);


        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();

    }

    public void FinishMeshIndirect(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
    {
        NativeArray<int> trisCount = new NativeArray<int>();
        trisCount = request.GetData<int>();
        int numTris = trisCount[0];

        //Graphics.DrawProceduralIndirect(GetComponent<MeshRenderer>().material, new Bounds(gameObject.transform.position, new Vector3(_sizeX, _sizeY, _sizeZ) * _spaceBetweenPoints), MeshTopology.Triangles, _triangleBuffer,numTris);
    }

    /*
    public void FinishMesh(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
    {
        //Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(_triangleBuffer, _triCountBuffer, 0);
        int[] triCountArray = { 0 };
        _triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        NativeArray<Triangle> tris = new NativeArray<Triangle>();
        tris = request.GetData<Triangle>();
        
        /*
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }
    */

        public void Start()
    {
        InitializeVolume(_defaultValue);

        MetaballControler[] children = GetComponentsInChildren<MetaballControler>();
        foreach(MetaballControler mb in children)
        {
            _effectors.Add(mb.metaball);
        }
    }

    public void Update()
    {
        CreateMesh();
    }
}
