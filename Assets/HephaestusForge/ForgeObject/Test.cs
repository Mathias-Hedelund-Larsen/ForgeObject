using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SomeTest: HephaestusForge.ForgeObject
{
    [SerializeField]
    private int val = 10;

    [SerializeField]
    private Transform trans;
}

public class Test : MonoBehaviour
{
    [SerializeField]
    private SomeTest test;

    // Start is called before the first frame update
    void Start()
    {
        var data = test.ToJsonString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
