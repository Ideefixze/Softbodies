using System.Collections;
using System.Collections.Generic;
using Softbodies;
using UnityEngine;

namespace Softbodies
{

    public class CameraTouchRandomForce : MonoBehaviour
    {
        [SerializeField]
        private float force;


        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
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
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.AddForce(new Vector3(Random.Range(-force,force), Random.Range(-force, force), Random.Range(-force, force)));
                }
            }
        }
    }
}