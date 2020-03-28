using System;
using UnityEngine;

namespace HephaestusForge
{
    [Serializable]
    public sealed class UnityObjectStorage<T> where T : UnityEngine.Object 
    {
#pragma warning disable 0649

        [SerializeField]
        private T[] _objects;

#pragma warning restore 0649

        public T[] Objects { get => _objects;  }

        public static implicit operator T[](UnityObjectStorage<T> source) => source._objects;
        public static implicit operator T(UnityObjectStorage<T> source) => source._objects.Length > 0 ? source._objects[0] : null;

        public static explicit operator UnityObjectStorage<T>(T[] source) => new UnityObjectStorage<T>() { _objects = source };
        public static explicit operator UnityObjectStorage<T>(T source) => new UnityObjectStorage<T>() { _objects = new T[] { source } };
    }
}
