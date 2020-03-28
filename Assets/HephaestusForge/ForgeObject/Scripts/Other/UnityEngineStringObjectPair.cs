using System;
using UnityEngine;

namespace HephaestusForge.Internal
{
    [Serializable]
    public sealed class UnityEngineStringObjectPair 
    {
        [SerializeField]
        private string _key;

        [SerializeField]
        private string _fieldPath;

        [SerializeField]
        private UnityEngine.Object _value;

        public string Key { get => _key; }
        public UnityEngine.Object Value { get => _value;  }
    }
}
