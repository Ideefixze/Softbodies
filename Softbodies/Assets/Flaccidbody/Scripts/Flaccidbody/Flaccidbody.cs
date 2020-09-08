using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Softbodies
{
    [RequireComponent(typeof(MeshFilter))]
    public class Flaccidbody : MonoBehaviour
    {
        private Mesh _originalMesh;
        private GameObject[] _flaccidVertices;
        private Vector3[] _deformedVertices;
        private int _vCount;
        private int _tCount;

        [SerializeField]
        private float _spring;
        [SerializeField]
        private float _damper;
        [SerializeField]
        private GameObject _flaccidVertex;

        void Start()
        {
            CreateFlaccidbody();
        }

        private void CreateFlaccidbody()
        {
            _originalMesh = GetComponent<MeshFilter>().mesh;

            _vCount = _originalMesh.vertices.Length;
            _tCount = _originalMesh.triangles.Length;

            _flaccidVertices = new GameObject[_vCount];
            _deformedVertices = new Vector3[_vCount];

            for (int i = 0; i < _vCount; i++)
            {
                GameObject v = Instantiate(_flaccidVertex, transform);
                v.transform.localPosition = _originalMesh.vertices[i];
                _flaccidVertices[i] = v;

            }

            for (int i = 0; i < _vCount; i++)
            {
                for (int j = 0; j < _vCount; j++)
                {
                    if (i != j)
                    {
                        CreateSpringOnVertices(i, j);
                    }

                }
            }


            ///Per triangle joints: doesn't work well.
            /*
            for (int i = 0; i < _tCount; i++)
            {
                int[] triangle = new int[3];
                triangle[0] = _originalMesh.triangles[i];
                triangle[1] = _originalMesh.triangles[i + 1];
                triangle[2] = _originalMesh.triangles[i + 2];

                CreateSpringOnTriangle(triangle);
                

                i++;
                i++;
            }*/
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
            SpringJoint s = _flaccidVertices[a].AddComponent<SpringJoint>();
            s.connectedBody = _flaccidVertices[b].GetComponent<Rigidbody>();
            s.spring = _spring;
            s.damper = _damper;
            //s.maxDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            //s.minDistance = Vector3.Distance(_flaccidVertices[a].transform.localPosition, _flaccidVertices[b].transform.localPosition);
            
        }

        // Update is called once per frame
        void Update()
        {
            for(int i = 0; i<_vCount;i++)
            {
                _deformedVertices[i] = _flaccidVertices[i].transform.localPosition;
            }
            _originalMesh.vertices = _deformedVertices;
        }


    }
}
