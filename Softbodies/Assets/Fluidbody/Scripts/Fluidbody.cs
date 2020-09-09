using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fluidbody : MonoBehaviour
{
    [SerializeField]
    private int _dropletCount;
    [SerializeField]
    private GameObject _droplet;
    [SerializeField]
    private float _spring;
    [SerializeField]
    private float _spread;

    private GameObject[] _droplets;

    // Start is called before the first frame update
    void Start()
    {
        _droplets = new GameObject[_dropletCount];
        for (int i = 0; i < _dropletCount; i++)
        {
            GameObject d = Instantiate(_droplet);
            //d.GetComponent<Shape>().colour = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f), 1);
            d.transform.position = transform.position + new Vector3(Random.Range(-_spread, _spread), Random.Range(-_spread, _spread), Random.Range(-_spread, _spread));
            _droplets[i] = d;
        }

        /*
        for (int i = 0; i < _dropletCount; i++)
        {
            for (int j = 0; j < _dropletCount; j++)
            {
                if(i!=j)
                {
                    CreateSpring(i, j);
                }
                
            }
        }*/

        for (int i = 0; i < _dropletCount; i++)
        {
            CreateSpringWithCore(i);
        }
    }

    private void CreateSpring(int a, int b)
    {
        SpringJoint sj = _droplets[a].AddComponent<SpringJoint>();
        sj.connectedBody = _droplets[b].GetComponent<Rigidbody>();
        //sj.autoConfigureConnectedAnchor = false;
        //sj.connectedAnchor = transform.position;
        sj.spring = _spring;
    }

    private void CreateSpringWithCore(int a)
    {
        SpringJoint sj = gameObject.AddComponent<SpringJoint>();
        sj.connectedBody = _droplets[a].GetComponent<Rigidbody>();
        sj.autoConfigureConnectedAnchor = false;
        sj.connectedAnchor = transform.position;
        sj.spring = _spring;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
