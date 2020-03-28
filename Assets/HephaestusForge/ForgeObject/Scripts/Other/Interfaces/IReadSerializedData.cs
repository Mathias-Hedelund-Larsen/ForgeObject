using UnityEngine;

namespace HephaestusForge
{
    public interface IReadSerializedData
    {
        T ReadByGeneric<T>(bool moveReadPos = true);

        T ReadUnityObject<T>(bool moveReadPos = true) where T : Object;

        T[] ReadUnityObjectArray<T>(bool moveReadPos = true) where T : Object;

        T ReadForgeObject<T>(bool moveReadPos = true) where T : ForgeObject, new();

        T[] ReadForgeObjects<T>(bool moveReadPos = true) where T : ForgeObject, new();

        byte ReadByte(bool moveReadPos = true);

        byte[] ReadBytes(int length, bool moveReadPos = true);      

        short ReadShort(bool moveReadPos = true);

        short[] ReadShortArray(bool moveReadPos = true);

        int ReadInt(bool moveReadPos = true);

        int[] ReadIntArray(bool moveReadPos = true);

        long ReadLong(bool moveReadPos = true);

        long[] ReadLongArray(bool moveReadPos = true);

        float ReadFloat(bool moveReadPos = true);

        float[] ReadFloatArray(bool moveReadPos = true);

        double ReadDouble(bool moveReadPos = true);

        double[] ReadDoubleArray(bool moveReadPos = true);

        bool ReadBool(bool moveReadPos = true);

        bool[] ReadBoolArray(bool moveReadPos = true);

        string ReadString(bool moveReadPos = true);

        string[] ReadStringArray(bool moveReadPos = true);

        Vector2 ReadVector2(bool moveReadPos = true);

        Vector2[] ReadVector2Array(bool moveReadPos = true);

        Vector2Int ReadVector2Int(bool moveReadPos = true);

        Vector2Int[] ReadVector2IntArray(bool moveReadPos = true);

        Vector3 ReadVector3(bool moveReadPos = true);

        Vector3[] ReadVector3Array(bool moveReadPos = true);

        Vector3Int ReadVector3Int(bool moveReadPos = true);

        Vector3Int[] ReadVector3IntArray(bool moveReadPos = true);

        Vector4 ReadVector4(bool moveReadPos = true);

        Vector4[] ReadVector4Array(bool moveReadPos = true);

        Quaternion ReadQuaternion(bool moveReadPos = true);

        Quaternion[] ReadQuaternionArray(bool moveReadPos = true);

        Color ReadColor(bool moveReadPos = true);

        Color[] ReadColorArray(bool moveReadPos = true);

        Color32 ReadColor32(bool moveReadPos = true);

        Color32[] ReadColor32Array(bool moveReadPos = true);
    }
}