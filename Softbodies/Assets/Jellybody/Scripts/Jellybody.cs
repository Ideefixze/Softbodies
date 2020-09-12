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
        private Mesh _originalMesh;                                 //Mesh we started with
        private GameObject[] _jellyVertices;                        //Jelly vertices objects
        private Dictionary<int, int[]> _jellyVertexToMeshVertex;    //Mapping of each jelly vertics to multiple mesh vertices
        private List<KeyValuePair<int,int>> _springs;               //To check if spring pairs are not duplicate
        private Vector3[] _deformedVertices;                        //Currently deformed vertices

        //How much distinct vertices there are
        private int _distinctVCount;
        //How much vertices there are
        private int _vCount;
        //How much triangles there are
        private int _tCount;

        [Tooltip("Strength of each spring.")]
        [SerializeField]
        private float _spring;
        [Tooltip("Damper of each spring.")]
        [SerializeField]
        private float _damper;
        [Tooltip("GameObject created on each vertex. Should have Rigidbody and a collider.")]
        [SerializeField]
        private GameObject _jellyVertex;
        [Tooltip("Creates springs on mesh triangles rather than between each vertex. Experimental.")]
        [SerializeField]
        private bool _springsOnTriangles = false;

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

            _springs = new List<KeyValuePair<int,int>>();

        }

        /// <summary>
        /// Creates mapping for the same vertices used in different triangles.
        /// <para>For example: One vertex in a 8-vertex cube appears in three faces making total of 24 vertices in memory.
        /// We should create only one JellyVertex on this multiple-appearing vertex for optimization. This function creates a mapping of JellyVertex to it's mesh vertices.</para>
        /// </summary>
        private void MapMeshVerticesToJellyVertices()
        {
            //Init
            _jellyVertexToMeshVertex = new Dictionary<int, int[]>();
            List<Vector3WithID> vids = new List<Vector3WithID>();

            //Create a struct of Vector3 with ID so we can easier find vertices with the same position but different id
            for (int i = 0; i < _vCount; i++)
            {
                vids.Add(new Vector3WithID(i, _originalMesh.vertices[i]));
            }

            //Group by position (this will give us all vertices with the same position but different id in Groupings
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

                //...to id of jelly vertex
                _jellyVertexToMeshVertex.Add(k, verts);
                k++;
            }
            //Also remember how many distinct vertices there are
            _distinctVCount = k;
        }

        /// <summary>
        /// Inits vertices on mesh vertices and creates springs between them.
        /// </summary>
        private void CreateJellybodyFromVertices()
        {

            for (int i = 0; i < _distinctVCount; i++)
            {
                GameObject v = Instantiate(_jellyVertex, transform);
                v.transform.localPosition = _originalMesh.vertices[_jellyVertexToMeshVertex.ElementAt(i).Value[0]];
                _jellyVertices[i] = v;
            }


            if(_springsOnTriangles)
            {
                for(int i = 0; i<_tCount;i++)
                {
                    int[] t = new int[3];
                    t[0] = _originalMesh.triangles[i];
                    t[1] = _originalMesh.triangles[i+1];
                    t[2] = _originalMesh.triangles[i+2];
                    CreateSpringOnTriangle(t);

                    i++;
                    i++;
                }
            }
            else
            {
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
            }
            
            //Make spring with the center of a mesh
            //This makes our object more stable and makes transform's center to be close to the mesh center
            //Without this, our mesh vertices would move too far away from object position
            //Making this mesh unrenderable
            for (int i = 0; i < _distinctVCount; i++)
            {
                CreateSpringWithCore(i);
            }
        }


        /// <summary>
        /// Recreates original mesh after jellyfication.
        /// Maps JellyVertices to the mesh vertices updating their position by their coresponding JellyVertex.
        /// </summary>
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
            CreateSpringOnMeshVertices(triangle[0], triangle[1]);
            //0 -> 2
            CreateSpringOnMeshVertices(triangle[0], triangle[2]);
            //1 -> 2
            CreateSpringOnMeshVertices(triangle[1], triangle[2]);
        }
        
        private void CreateSpringOnVertices(int a, int b)
        {
            //Avoid duplicate springs between pairs
            if (_springs.Contains(new KeyValuePair<int, int>(a,b)))
            {
                return;
            }
            Debug.Log(a);
            SpringJoint s = _jellyVertices[a].AddComponent<SpringJoint>();
            s.connectedBody = _jellyVertices[b].GetComponent<Rigidbody>();
            s.spring = _spring;
            s.damper = _damper;
            _springs.Add(new KeyValuePair<int, int>(a,b));
            //s.maxDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            //s.minDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            
        }

        private void CreateSpringOnMeshVertices(int a, int b)
        {
            int akey = 0;
            int bkey = 0;
            //Find mapping that has a and b
            foreach (var mapping in _jellyVertexToMeshVertex)
            {
                if (mapping.Value.Contains(a))
                {
                    akey = mapping.Key;
                }
                if (mapping.Value.Contains(b))
                {
                    bkey = mapping.Key;
                }
            }

            CreateSpringOnVertices(akey,bkey);
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
