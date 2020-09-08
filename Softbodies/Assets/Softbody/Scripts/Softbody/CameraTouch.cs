using System.Collections;
using System.Collections.Generic;
using Softbodies;
using UnityEngine;

namespace Softbodies
{

    public class CameraTouch : MonoBehaviour
    {
        public float pressure;


        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();
            }
        }

        public void HandleInput()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit))
            {
                Softbody sb = hit.collider.GetComponent<Softbody>();
                if (sb)
                {
                    sb.ApplyPressure(hit.point, pressure);
                }
            }
        }
    }
}