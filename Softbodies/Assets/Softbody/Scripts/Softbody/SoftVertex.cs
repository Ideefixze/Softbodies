using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Softbodies
{
    public class SoftVertex
    {
        public int id;
        private Vector3 _initialVertexPosition;
        private Vector3 _vertexPosition;
        private Vector3 _velocity;

        public SoftVertex(int id, Vector3 v)
        {
            this.id = id;
            _initialVertexPosition = v;
            _vertexPosition = v;
            _velocity = new Vector3(0,0,0);
        }

        public Vector3 vertexPosition => _vertexPosition;

        public Vector3 CurrentDisplacement()
        {
            return _vertexPosition - _initialVertexPosition;
        }

        public void Settle(float stiffness)
        {
            _velocity *= Mathf.Max(1f - stiffness * Time.deltaTime, 0f);
        }

        public void ApplyPressure(Transform transform, Vector3 position, float pressure)
        {
            Vector3 point = _vertexPosition - transform.InverseTransformPoint(position);
            pressure = pressure / (1f + point.sqrMagnitude*4f);
            _velocity += point.normalized * pressure * Time.deltaTime;
        }

        public void UpdateVelocity(float bounciness)
        {
            _velocity = _velocity - CurrentDisplacement() * bounciness * Time.deltaTime;
        }

        public void UpdateVertex()
        {
            _vertexPosition += _velocity * Time.deltaTime;
        }
    }
}

