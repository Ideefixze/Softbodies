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
    GraphicsBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;

    [SerializeField]
    ComputeShader marchingShader;
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
        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, maxTriangleCount, sizeof(float) * 3 * 3);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

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
        pointsBuffer.SetData(data);

        foreach(IsosurfaceEffector ie in _effectors)
        {
            ie.Evaluate(new Vector3Int(_sizeX, _sizeY, _sizeZ), pointsBuffer);
        }

        
    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
        }
    }

    private void CreateMesh()
    {
        PrepareBuffers();

        triangleBuffer.SetCounterValue(0);
        marchingShader.SetBuffer(0, "points", pointsBuffer);
        marchingShader.SetBuffer(0, "triangles", triangleBuffer);
        marchingShader.SetInt("sizeX", _sizeX);
        marchingShader.SetInt("sizeY", _sizeY);
        marchingShader.SetInt("sizeZ", _sizeZ);
        marchingShader.SetFloat("isoLevel", 0f);

        
        marchingShader.Dispatch(0, _sizeX - 1, _sizeY - 1, _sizeZ - 1);

        //Get number of triangles in the triangle buffer
        GraphicsBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];
        Debug.Log(numTris);

        ComputeBuffer argsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        int[] args = new int[] {3, 1, 1, 1 };
        argsBuffer.SetData(args);
        
        //ComputeBuffer.CopyCount(triangleBuffer, argsBuffer, 0);

        Graphics.DrawProceduralIndirect(GetComponent<MeshRenderer>().material, new Bounds(gameObject.transform.position, new Vector3(_sizeX, _sizeY, _sizeZ) * _spaceBetweenPoints),MeshTopology.Points, triangleBuffer,argsBuffer);

        //argsBuffer.Dispose();
        //Graphics.DrawProceduralIndirect(GraphicsBuffer, new Bounds(gameObject.transform.position, new Vector3(_sizeX, _sizeY, _sizeZ) * _spaceBetweenPoints), MeshTopology.Triangles, argsBuffer);
        //Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles,triangleBuffer,);
        // Get triangle data from shader
        //UnityEngine.Rendering.AsyncGPUReadback.Request(triCountBuffer, FinishMeshIndirect);

    }

    public void FinishMeshIndirect(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
    {
        NativeArray<int> trisCount = new NativeArray<int>();
        trisCount = request.GetData<int>();
        int numTris = trisCount[0];

        //Graphics.DrawProceduralIndirect(GetComponent<MeshRenderer>().material, new Bounds(gameObject.transform.position, new Vector3(_sizeX, _sizeY, _sizeZ) * _spaceBetweenPoints), MeshTopology.Triangles, triangleBuffer,numTris);
    }

    /*
    public void FinishMesh(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
    {
        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
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
