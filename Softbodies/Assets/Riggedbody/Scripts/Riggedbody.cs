using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds rigidbodies and springs for all objects in _bones array. Use it for rigged model so it will behave like a soft body.
/// </summary>
public class Riggedbody : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _bones;
    [SerializeField]
    private float _mass;
    [SerializeField]
    private float _drag;
    [SerializeField]
    private float _angularDrag;
    [SerializeField]
    private float _spring;

    void Start()
    {
        InitRigidbodies();
        InitSprings();
    }

    void InitRigidbodies()
    {
        foreach(GameObject go in _bones)
        {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = _mass;
            rb.drag = _drag;
            rb.angularDrag = _angularDrag;
            rb.constraints = RigidbodyConstraints.FreezeRotationY;
        }
    }

    void InitSprings()
    {
        foreach (GameObject goA in _bones)
        {
            foreach(GameObject goB in _bones)
            {
                if(goA != goB)
                {
                    SpringJoint sj = goA.AddComponent<SpringJoint>();
                    sj.spring = _spring;
                    sj.connectedBody = goB.GetComponent<Rigidbody>();
                }
            }
        }
    }
}
