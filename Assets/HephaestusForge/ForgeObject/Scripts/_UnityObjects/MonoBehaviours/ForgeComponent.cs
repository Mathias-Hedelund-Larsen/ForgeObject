using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ForgeComponent : MonoBehaviour, ISerializationCallbackReceiver
{


    public void OnAfterDeserialize()
    {
        
    }

    public void OnBeforeSerialize()
    {
        
    }
}
