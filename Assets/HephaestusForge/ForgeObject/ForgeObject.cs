using System;
using UnityEngine;

[Serializable]
public abstract class ForgeObject 
{
    /// <summary>
    /// Get the data of this class as json string
    /// </summary>
    /// <returns>A json representation of the class.</returns>
    public override string ToString()
    {
        return ToJsonString();
    }

    /// <summary>
    /// Get the data of this class as json string
    /// </summary>
    /// <returns>A json representation of the class.</returns>
    public string ToJsonString()
    {
        return JsonUtility.ToJson(this);
    }
}
