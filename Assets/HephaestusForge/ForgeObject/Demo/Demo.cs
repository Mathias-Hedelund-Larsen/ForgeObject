﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HephaestusForge
{
    public class Demo : MonoBehaviour
    {
        [SerializeField]
        private Child forgeObject;

        void Start()
        {
            //s = _d.ToJsonString();
            forgeObject = ForgeObject.Polymorph(forgeObject);            
        }

        // Update is called once per frame
        void Update()
        {
            forgeObject.Transform();
        }
    }

    [Serializable]
    public class Child : ForgeObject
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        protected int _time;

        [SerializeField]
        protected int _timer;

        [SerializeField]
        protected Transform _someTransform;        

        public virtual void Transform()
        {
            if(_timer <= 0)
            {
                _timer = _time;
            }
            else
            {
                _timer -= 1;

                if(_timer <= 0)
                {
                    _timer = _time;
                    _someTransform.Translate(Vector3.one);
                }
            }
        }
    }

    [Serializable]
    public class GrandChild : Child
    {
        public override void Transform()
        {
            if (_timer <= 0)
            {
                _timer = _time;
            }
            else
            {
                _timer -= 2;

                if (_timer <= 0)
                {
                    _timer = _time;
                    _someTransform.Translate(Vector3.one * 3);
                }
            }
        }
    }
}