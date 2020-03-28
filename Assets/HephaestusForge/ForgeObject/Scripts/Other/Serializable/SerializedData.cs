using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HephaestusForge
{
    [Serializable]
    public class SerializedData : ISerializationCallbackReceiver, IWriteSerializedData, IReadSerializedData
    {
        [SerializeField]
        private List<byte> _buffer;

        [SerializeField]
        private List<UnityEngine.Object[]> _objects;

        [NonSerialized]
        private byte[] _readableBuffer;

        [NonSerialized]
        private int _readPos;

        [NonSerialized]
        private int _readObjectPos;

        public void OnBeforeSerialize()
        {                        
        }

        public void OnAfterDeserialize()
        {
            if(_readableBuffer == null)
            {
                _readableBuffer = _buffer.ToArray();
            }
        }

        public SerializedData()
        {
            _buffer = new List<byte>(); // Initialize buffer
            _readPos = 0; // Set readPos to 0
            _readObjectPos = 0;
            _readableBuffer = new byte[0];
            _objects = new List<UnityEngine.Object[]>();
        }

        /// <summary>Creates a serialized data from which data can be read. Used for receiving.</summary>
        /// <param name="_data">The bytes to add to the serialized data.</param>
        public SerializedData(byte[] _data) : this()
        {
            SetBytes(_data);
        }

        #region Functions

        /// <summary>Sets the serialized data's content and prepares it to be read.</summary>
        /// <param name="_data">The bytes to add to the serialized data.</param>
        private void SetBytes(byte[] _data)
        {
            _buffer.AddRange(_data);
            _readableBuffer = _buffer.ToArray();
        }

        /// <summary>Inserts the length of the serialized data's content at the start of the buffer.</summary>
        public void WriteLength()
        {
            _buffer.InsertRange(0, BitConverter.GetBytes(_buffer.Count)); // Insert the byte length of the serialized data at the very beginning
        }

        /// <summary>Inserts the given int at the start of the buffer.</summary>
        /// <param name="value">The int to insert.</param>
        public void InsertInt(int value)
        {
            _buffer.InsertRange(0, BitConverter.GetBytes(value)); // Insert the int at the start of the buffer
        }

        /// <summary>Gets the serialized data's content in array form.</summary>
        public byte[] ToArray()
        {
            _readableBuffer = _buffer.ToArray();
            return _readableBuffer;
        }

        /// <summary>Gets the length of the serialized data's content.</summary>
        public int Length()
        {
            return _buffer.Count; // Return the length of buffer
        }

        /// <summary>Gets the length of the unread data contained in the serialized data.</summary>
        public int UnreadLength()
        {
            return Length() - _readPos; // Return the remaining length (unread)
        }

        /// <summary>Resets the serialized data instance to allow it to be reused.</summary>
        /// <param name="shouldReset">Whether or not to reset the serialized data.</param>
        public void Reset(bool shouldReset = true)
        {
            if (shouldReset)
            {
                _buffer.Clear(); // Clear buffer
                _readableBuffer = null;
                _readPos = 0; // Reset readPos
            }
            else
            {
                _readPos -= 4; // "Unread" the last read int
            }
        }
        #endregion

        #region Write Data
       
        /// <summary>Adds data for a UnityEngine.Object to the serialized data.</summary>
        /// <param name="value">The object to add.</param>
        public void WriteUnityObject<T>(T value) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Write(JsonUtility.ToJson((UnityObjectStorage<T>)value));
            }
            else
            {
                _objects.Add((UnityObjectStorage<T>)value);
            }
#else
            Write(JsonUtility.ToJson((UnityObjectStorage<T>)value));
#endif
        }

        /// <summary>Adds data for a UnityEngine.Object array to the serialized data.</summary>
        /// <param name="value">The objects to add.</param>
        public void WriteUnityObjects<T>(T[] value) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Write(JsonUtility.ToJson((UnityObjectStorage<T>)value));
            }
            else
            {
                _objects.Add(value);
            }
#else
            Write(JsonUtility.ToJson((UnityObjectStorage<T>)value));
#endif
        }

        /// <summary>Adds a ForgeObject to the serialized data.</summary>
        /// <param name="value">The ForgeObject to add.</param>
        public void Write<T>(T value) where T : ForgeObject
        {
            Write(value.Serialized().ToArray());
        }

        /// <summary>Adds a ForgeObject array to the serialized data.</summary>
        /// <param name="value">The ForgeObject to add.</param>
        public void Write<T>(T[] value) where T : ForgeObject
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i].Serialized().ToArray());
            }
        }

        /// <summary>Adds a byte to the serialized data.</summary>
        /// <param name="value">The byte to add.</param>
        public void Write(byte value)
        {
            _buffer.Add(value);
        }

        /// <summary>Adds an array of bytes to the serialized data.</summary>
        /// <param name="value">The byte array to add.</param>
        public void Write(byte[] value)
        {
            Write(value.Length);
            _buffer.AddRange(value);
        }

        /// <summary>Adds a short to the serialized data.</summary>
        /// <param name="value">The short to add.</param>
        public void Write(short value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a short array to the serialized data.</summary>
        /// <param name="value">The short array to add.</param>
        public void Write(short[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds an int to the serialized data.</summary>
        /// <param name="value">The int to add.</param>
        public void Write(int value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds an int array to the serialized data.</summary>
        /// <param name="value">The int array to add.</param>
        public void Write(int[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a long to the serialized data.</summary>
        /// <param name="value">The long to add.</param>
        public void Write(long value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a long array to the serialized data.</summary>
        /// <param name="value">The long array to add.</param>
        public void Write(long[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a float to the serialized data.</summary>
        /// <param name="value">The float to add.</param>
        public void Write(float value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a float array to the serialized data.</summary>
        /// <param name="value">The float array to add.</param>
        public void Write(float[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a bool to the serialized data.</summary>
        /// <param name="value">The bool to add.</param>
        public void Write(bool value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a bool array to the serialized data.</summary>
        /// <param name="value">The bool array to add.</param>
        public void Write(bool[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a string to the serialized data.</summary>
        /// <param name="value">The string to add.</param>
        public void Write(string value)
        {
            Write(value.Length); // Add the length of the string to the serialized data
            _buffer.AddRange(Encoding.UTF8.GetBytes(value)); // Add the string itself
        }

        /// <summary>Adds a string array to the serialized data.</summary>
        /// <param name="value">The string array to add.</param>
        public void Write(string[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a double to the serialized data.</summary>
        /// <param name="value">The double to add.</param>
        public void Write(double value)
        {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a double array to the serialized data.</summary>
        /// <param name="value">The double array to add.</param>
        public void Write(double[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Vector2 to the serialized data.</summary>
        /// <param name="value">The Vector2 to add.</param>
        public void Write(Vector2 value)
        {
            Write(value.x);
            Write(value.y);
        }

        /// <summary>Adds a Vector2 array to the serialized data.</summary>
        /// <param name="value">The Vector2 array to add.</param>
        public void Write(Vector2[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Vector2Int to the serialized data.</summary>
        /// <param name="value">The Vector2Int to add.</param>
        public void Write(Vector2Int value)
        {
            Write(value.x);
            Write(value.y);
        }

        /// <summary>Adds a Vector2Int array to the serialized data.</summary>
        /// <param name="value">The Vector2Int array to add.</param>
        public void Write(Vector2Int[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Vector3 to the serialized data.</summary>
        /// <param name="value">The Vector3 to add.</param>
        public void Write(Vector3 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        /// <summary>Adds a Vector3 array to the serialized data.</summary>
        /// <param name="value">The Vector3 array to add.</param>
        public void Write(Vector3[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Vector3Int to the serialized data.</summary>
        /// <param name="value">The Vector3Int to add.</param>
        public void Write(Vector3Int value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        /// <summary>Adds a Vector3Int array to the serialized data.</summary>
        /// <param name="value">The Vector3Int array to add.</param>
        public void Write(Vector3Int[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Vector4 to the serialized data.</summary>
        /// <param name="value">The Vector4 to add.</param>
        public void Write(Vector4 value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        /// <summary>Adds a Vector4 array to the serialized data.</summary>
        /// <param name="value">The Vector4 array to add.</param>
        public void Write(Vector4[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Quaternion to the serialized data.</summary>
        /// <param name="value">The Quaternion to add.</param>
        public void Write(Quaternion value)
        {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        /// <summary>Adds a Quaternion array to the serialized data.</summary>
        /// <param name="value">The Quaternion array to add.</param>
        public void Write(Quaternion[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Color to the serialized data.</summary>
        /// <param name="value">The Color to add.</param>
        public void Write(Color value)
        {
            Write(value.a);
            Write(value.b);
            Write(value.g);
            Write(value.r);
        }

        /// <summary>Adds a Color array to the serialized data.</summary>
        /// <param name="value">The Color array to add.</param>
        public void Write(Color[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        /// <summary>Adds a Color32 to the serialized data.</summary>
        /// <param name="value">The Color32 to add.</param>
        public void Write(Color32 value)
        {
            Write(value.a);
            Write(value.b);
            Write(value.g);
            Write(value.r);
        }

        /// <summary>Adds a Color32 array to the serialized data.</summary>
        /// <param name="value">The Color32 array to add.</param>
        public void Write(Color32[] value)
        {
            Write(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                Write(value[i]);
            }
        }

        #endregion

        #region Read Data
        public T ReadByGeneric<T>(bool moveReadPos = true)
        {
            return default;
        }

        /// <summary>Reads an object from the serialized object references.</summary>
        /// <param name="moveReadPos">Whether or not to move the objects read position.</param>
        public T ReadUnityObject<T>(bool moveReadPos = true) where T : UnityEngine.Object
        {
            if (_readObjectPos < _objects.Count)
            {
                return (T)_objects[moveReadPos ? _readObjectPos++ : _readObjectPos][0];
            }
            else
            {
                return JsonUtility.FromJson<UnityObjectStorage<T>>(ReadString(moveReadPos));
            }
        }

        public T[] ReadUnityObjectArray<T>(bool moveReadPos = true) where T : UnityEngine.Object
        {
            if (_readObjectPos < _objects.Count)
            {
                var objects = _objects[moveReadPos ? _readObjectPos++ : _readObjectPos];
                T[] returned = new T[objects.Length];

                for (int i = 0; i < returned.Length; i++)
                {
                    returned[i] = (T)objects[i];
                }

                return returned;
            }
            else
            {
                return JsonUtility.FromJson<UnityObjectStorage<T>>(ReadString(moveReadPos));
            }
        }

        /// <summary>Reads a byte from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte ReadByte(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                byte value = _readableBuffer[_readPos]; // Get the byte at readPos' position
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += 1; // Increase readPos by 1
                }
                return value; // Return the byte
            }
            else
            {
                throw new Exception("Could not read value of type 'byte'!");
            }
        }

        /// <summary>Reads an array of bytes from the serialized data.</summary>
        /// <param name="length">The length of the byte array.</param>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte[] ReadBytes(bool moveReadPos = true)
        {
            var length = ReadInt(moveReadPos);

            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                byte[] value = _buffer.GetRange(_readPos, length).ToArray(); // Get the bytes at readPos' position with a range of _length
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += length; // Increase readPos by _length
                }
                return value; // Return the bytes
            }
            else
            {
                throw new Exception("Could not read value of type 'byte[]'!");
            }
        }

        public T ReadForgeObject<T>(bool moveReadPos = true) where T : ForgeObject, new()
        {
            return ForgeObject.Instantiate<T>(new SerializedData(ReadBytes(moveReadPos)));
        }

        /// <summary>Reads a short from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public short ReadShort(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                short value = BitConverter.ToInt16(_readableBuffer, _readPos); // Convert the bytes to a short
                if (moveReadPos)
                {
                    // If moveReadPos is true and there are unread bytes
                    _readPos += 2; // Increase readPos by 2
                }
                return value; // Return the short
            }
            else
            {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

        /// <summary>Reads an int from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public int ReadInt(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                int value = BitConverter.ToInt32(_readableBuffer, _readPos); // Convert the bytes to an int
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += 4; // Increase readPos by 4
                }
                return value; // Return the int
            }
            else
            {
                throw new Exception("Could not read value of type 'int'!");
            }
        }

        /// <summary>Reads a long from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public long ReadLong(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                long value = BitConverter.ToInt64(_readableBuffer, _readPos); // Convert the bytes to a long
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += 8; // Increase readPos by 8
                }
                return value; // Return the long
            }
            else
            {
                throw new Exception("Could not read value of type 'long'!");
            }
        }

        /// <summary>Reads a float from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public float ReadFloat(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                float value = BitConverter.ToSingle(_readableBuffer, _readPos); // Convert the bytes to a float
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += 4; // Increase readPos by 4
                }
                return value; // Return the float
            }
            else
            {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        /// <summary>Reads a float from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public double ReadDouble(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                double value = BitConverter.ToDouble(_readableBuffer, _readPos); // Convert the bytes to a float
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += 8; // Increase readPos by 8
                }
                return value; // Return the float
            }
            else
            {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        /// <summary>Reads a bool from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public bool ReadBool(bool moveReadPos = true)
        {
            if (_buffer.Count > _readPos)
            {
                // If there are unread bytes
                bool value = BitConverter.ToBoolean(_readableBuffer, _readPos); // Convert the bytes to a bool
                if (moveReadPos)
                {
                    // If moveReadPos is true
                    _readPos += 1; // Increase readPos by 1
                }
                return value; // Return the bool
            }
            else
            {
                throw new Exception("Could not read value of type 'bool'!");
            }
        }

        /// <summary>Reads a string from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public string ReadString(bool moveReadPos = true)
        {
            try
            {
                int _length = ReadInt(); // Get the length of the string
                string value = Encoding.UTF8.GetString(_readableBuffer, _readPos, _length); // Convert the bytes to a string
                if (moveReadPos && value.Length > 0)
                {
                    // If moveReadPos is true string is not empty
                    _readPos += _length; // Increase readPos by the length of the string
                }
                return value; // Return the string
            }
            catch
            {
                throw new Exception("Could not read value of type 'string'!");
            }
        }

        /// <summary>Reads a Vector2 from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector2 ReadVector2(bool moveReadPos = true)
        {
            return new Vector2(ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        /// <summary>Reads a Vector2Int from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector2Int ReadVector2Int(bool moveReadPos = true)
        {
            return new Vector2Int(ReadInt(moveReadPos), ReadInt(moveReadPos));
        }

        /// <summary>Reads a Vector3 from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector3 ReadVector3(bool moveReadPos = true)
        {
            return new Vector3(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        /// <summary>Reads a Vector3Int from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector3Int ReadVector3Int(bool moveReadPos = true)
        {
            return new Vector3Int(ReadInt(moveReadPos), ReadInt(moveReadPos), ReadInt(moveReadPos));
        }

        /// <summary>Reads a Vector4 from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector4 ReadVector4(bool moveReadPos = true)
        {
            return new Vector4(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        /// <summary>Reads a Quaternion from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Quaternion ReadQuaternion(bool moveReadPos = true)
        {
            return new Quaternion(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        /// <summary>Reads a Color from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Color ReadColor(bool moveReadPos = true)
        {
            return new Color(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        /// <summary>Reads a Color from the serialized data.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Color32 ReadColor32(bool moveReadPos = true)
        {
            return new Color32(ReadByte(moveReadPos), ReadByte(moveReadPos), ReadByte(moveReadPos), ReadByte(moveReadPos));
        }

        #endregion
    }
}
