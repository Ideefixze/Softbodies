using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Softbodies
{
    public struct Vector3WithID
    {
        public int id;
        public Vector3 vertex;

        public Vector3WithID(int id, Vector3 vertex)
        {
            this.id = id;
            this.vertex = vertex;
        }
    }

    [RequireComponent(typeof(MeshFilter))]
    public class Jellybody : MonoBehaviour
    {
        private Mesh _originalMesh;
        private GameObject[] _jellyVertices;
        private Dictionary<int, int[]> _jellyVertexToMeshVertex;
        private Vector3[] _deformedVertices;

        //How much distinct vertices there are
        private int _distinctVCount;
        //How much vertices there are
        private int _vCount;
        //How much triangles there are
        private int _tCount;

        [SerializeField]
        private float _spring;
        [SerializeField]
        private float _damper;
        [SerializeField]
        private GameObject _jellyVertex;

        void Start()
        {
            InitData();
            MapMeshVerticesToJellyVertices();
            CreateJellybodyFromVertices();
        }

        private void InitData()
        {
            _originalMesh = GetComponent<MeshFilter>().mesh;

            _vCount = _originalMesh.vertices.Length;
            _tCount = _originalMesh.triangles.Length;

            _jellyVertices = new GameObject[_vCount];
            _deformedVertices = new Vector3[_vCount];

        }

        private void MapMeshVerticesToJellyVertices()
        {
            _jellyVertexToMeshVertex = new Dictionary<int, int[]>();
            List<Vector3WithID> vids = new List<Vector3WithID>();

            for (int i = 0; i < _vCount; i++)
            {
                vids.Add(new Vector3WithID(i, _originalMesh.vertices[i]));
            }

            var groups = vids.GroupBy(v => v.vertex);


            int k = 0;
            //Foreach group...
            foreach(var v in groups)
            {
                //Add new mapping of ids...
                int[] verts = new int[v.Count()];
                int j = 0;
                foreach(var w in v)
                {
                    verts[j] = w.id;
                    j++;
                }

                //...to next id of jelly vertex
                _jellyVertexToMeshVertex.Add(k, verts);
                k++;
            }
            _distinctVCount = k;
        }

        private void CreateJellybodyFromVertices()
        {

            for (int i = 0; i < _distinctVCount; i++)
            {
                GameObject v = Instantiate(_jellyVertex, transform);
                v.transform.localPosition = _originalMesh.vertices[_jellyVertexToMeshVertex.ElementAt(i).Value[0]];
                _jellyVertices[i] = v;
            }



            for (int i = 0; i < _distinctVCount; i++)
            {
                for (int j = 0; j < _distinctVCount; j++)
                {
                    if (i != j)
                    {
                        CreateSpringOnVertices(i, j);
                    }

                }
            }

            for (int i = 0; i < _distinctVCount; i++)
            {
                CreateSpringWithCore(i);
            }
        }


        private void UpdateJellybody()
        {
            for (int i = 0; i < _distinctVCount; i++)
            {
                foreach(int j in _jellyVertexToMeshVertex.ElementAt(i).Value)
                {
                    _deformedVertices[j] = _jellyVertices[i].transform.localPosition;
                }
            }
            
            _originalMesh.vertices = _deformedVertices;
            _originalMesh.RecalculateNormals();
        }

        private void CreateSpringOnTriangle(int[] triangle)
        {
            //0 -> 1
            CreateSpringOnVertices(triangle[0], triangle[1]);
            //0 -> 2
            CreateSpringOnVertices(triangle[0], triangle[2]);
            //1 -> 2
            CreateSpringOnVertices(triangle[1], triangle[2]);
        }
        
        private void CreateSpringOnVertices(int a, int b)
        {
            SpringJoint s = _jellyVertices[a].AddComponent<SpringJoint>();
            s.connectedBody = _jellyVertices[b].GetComponent<Rigidbody>();
            s.spring = _spring;
            s.damper = _damper;
            //s.maxDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            //s.minDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            
        }

        private void CreateSpringWithCore(int a)
        {
            SpringJoint s = _jellyVertices[a].AddComponent<SpringJoint>();
            s.connectedBody = gameObject.GetComponent<Rigidbody>();
            
            s.spring = 1;
            s.damper = 1;
            //s.maxDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            //s.minDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);

        }

        // Update is called once per frame
        void FixedUpdate()
        {
           UpdateJellybody();
        }


    }
}
