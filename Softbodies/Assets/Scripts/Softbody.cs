using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace Softbodies
{
    [RequireComponent(typeof(MeshFilter))]
    public class Softbody : MonoBehaviour
    {
        public float bounciness;
        public float stiffness;
        public float onCollisionReaction;

        private Mesh _originalMesh;
        private Vector3[] _originalVertices;
        private Vector3[] _deformedVertices;
        private SoftVertex[] _softVertices;
        private int _vCount;

        // Start is called before the first frame update
        void Start()
        {
            _originalMesh = GetComponent<MeshFilter>().mesh;
            _originalVertices = _originalMesh.vertices;
            _vCount = _originalVertices.Length;
            _softVertices = new SoftVertex[_vCount];
            _deformedVertices = new Vector3[_vCount];

            for (int i = 0; i < _vCount; i++)
            {
                _softVertices[i] = new SoftVertex(i,_originalVertices[i]);
                //_softVertices[i].ApplyPressure(new Vector3(_softVertices[i].vertexPosition.x*_softVertices[i].vertexPosition.x, 0,0) *10000f);
            }
            
        }

        // Update is called once per frame
        void FixedUpdate()
        {

            for (int i = 0; i < _vCount; i++)
            {
                _softVertices[i].UpdateVertex();
                _softVertices[i].UpdateVelocity(bounciness);
                _softVertices[i].Settle(stiffness);
                _deformedVertices[i] = _softVertices[i].vertexPosition;
            }

            _originalMesh.vertices = _deformedVertices;
            _originalMesh.RecalculateBounds();
            //_originalMesh.RecalculateNormals();
            _originalMesh.RecalculateTangents();
        }

        public void ApplyPressure(Vector3 position, float pressure)
        {
            for (int i = 0; i < _vCount; i++)
            {
                _softVertices[i].ApplyPressure(transform,position,pressure);
            }
        }

        public void OnCollisionEnter(Collision coll)
        {
            ContactPoint[] cps = new ContactPoint[coll.contactCount];
            coll.GetContacts(cps);
            foreach (var cp in cps)
            {
                Debug.Log(cp.point);
                ApplyPressure(cp.point, coll.relativeVelocity.magnitude* onCollisionReaction);
            }
        }
    }
}

