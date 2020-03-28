using System;
using UnityEngine;

namespace HephaestusForge.Internal
{
    [Serializable]
    public sealed class UnityObjectStorage 
    {
#pragma warning disable 0649

        [SerializeField]
        private string _key;

        [SerializeField]
        private string _fieldPath;

        [SerializeField]
        private UnityEngine.Object _value;

#pragma warning restore 0649

        public string Key { get => _key; }
        public string FieldPath { get => _fieldPath; }
        public UnityEngine.Object Value { get => _value;  }
    }
}
