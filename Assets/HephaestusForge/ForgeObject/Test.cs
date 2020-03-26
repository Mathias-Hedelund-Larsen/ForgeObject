using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SomeTest: HephaestusForge.ForgeObject
{
    [SerializeField]
    private int val = 10;

    [SerializeField]
    private Transform trans;

    [SerializeField]
    private SomeTestv testv;
}

[Serializable]
public class SomeTestv
{
    [SerializeField]
    private Button button;
}

public class Test : MonoBehaviour
{
    [SerializeField]
    private SomeTest test;

    // Start is called before the first frame update
    void Start()
    {
        string te = test.ToJsonString();
        var t = HephaestusForge.ForgeObject.CreateFromJson<SomeTest>(te);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
