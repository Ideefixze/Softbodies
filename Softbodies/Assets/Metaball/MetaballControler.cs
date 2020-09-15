using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetaballControler : MonoBehaviour
{
    public EffectorMetaball metaball;
    public float radius = 1f;
    // Start is called before the first frame update
    void Start()
    {
        metaball = new EffectorMetaball(transform, radius);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
