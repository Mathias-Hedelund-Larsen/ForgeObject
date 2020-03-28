using System;
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
        private ForgeObject _object;

        [SerializeField]
        private Child forgeObject;

        void Start()
        {            
            //s = _d.ToJsonString();
            forgeObject = ForgeObject.Instantiate(forgeObject);
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
        protected int _time;

        [SerializeField]
        protected int _timer;

        [SerializeField]
        protected Transform _someTransform;

        protected override void Init(IReadSerializedData reader)
        {
            _timer = _time;
        }

        public virtual void Transform()
        {
            if(_timer <= 0)
            {
                Debug.Log("Child");
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

        protected override void Serialize(IWriteSerializedData writer)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class GrandChild : Child
    {
        [SerializeField]
        private string _name;

        protected override void Init(IReadSerializedData reader)
        {
            _timer = _time;
            base.Init(reader);
        }

        public override void Transform()
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
