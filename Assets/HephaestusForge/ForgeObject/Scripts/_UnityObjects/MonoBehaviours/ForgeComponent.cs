using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HephaestusForge
{
    public abstract class ForgeComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {

        }
    }
}