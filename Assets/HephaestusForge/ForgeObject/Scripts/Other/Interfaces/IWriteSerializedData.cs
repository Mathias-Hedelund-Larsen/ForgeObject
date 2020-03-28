using UnityEngine;

namespace HephaestusForge
{
    public interface IWriteSerializedData
    {
        void WriteUnityObject<T>(T value) where T : Object;

        void WriteUnityObjects<T>(T[] value) where T : Object;

        void Write<T>(T value) where T : ForgeObject;

        void Write<T>(T[] value) where T : ForgeObject;

        void Write(byte value);

        void Write(byte[] value);

        void Write(short value);

        void Write(short[] value);

        void Write(int value);

        void Write(int[] value);

        void Write(long value);

        void Write(long[] value);

        void Write(float value);

        void Write(float[] value);

        void Write(double value);

        void Write(double[] value);

        void Write(bool value);

        void Write(bool[] value);

        void Write(string value);

        void Write(string[] value);

        void Write(Vector2 value);

        void Write(Vector2[] value);

        void Write(Vector2Int value);

        void Write(Vector2Int[] value);

        void Write(Vector3 value);

        void Write(Vector3[] value);

        void Write(Vector4 value);

        void Write(Quaternion value);

        void Write(Quaternion[] value);

        void Write(Color value);

        void Write(Color[] value);

        void Write(Color32 value);

        void Write(Color32[] value);
    }
}