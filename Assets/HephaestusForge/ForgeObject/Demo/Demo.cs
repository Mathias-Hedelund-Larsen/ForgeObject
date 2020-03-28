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
            var some = new Some<int>(new List<int>() { 1,2,3});

            var json = JsonUtility.ToJson(some);

            try
            {
                var t = JsonUtility.FromJson(json, typeof(Some<int>));
                //var t = JsonUtility.FromJson<Some<int>>(json);
            }
            catch (Exception)
            {
                Debug.Log("Cant parse with generic");

                try
                {
                   
                }
                catch (Exception)
                {
                    Debug.Log("Cant parse");
                }
            }

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
        protected int _time;

        [SerializeField]
        protected int _timer;

        [SerializeField]
        protected Transform _someTransform;

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
    }

    [Serializable]
    public class GrandChild : Child
    {
        [SerializeField]
        private string _name;

        protected override void Init()
        {
            _timer = _time;
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

    [Serializable]
    public class Some<T>
    {
        [SerializeField]
        private List<T> _list;

        public Some(List<T> list)
        {
            _list = list;
        }
    }
}
