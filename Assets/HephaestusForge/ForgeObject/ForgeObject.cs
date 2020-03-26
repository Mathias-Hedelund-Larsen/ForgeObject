using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace HephaestusForge
{
    [Serializable]
    public abstract class ForgeObject
    {

#if UNITY_EDITOR 
        [NonSerialized]
        private string _editorJsonData;
#endif

        [NonSerialized]
        private string[] _fieldNames;

        [NonSerialized]
        private FieldInfo[] _jsonFields;

        [NonSerialized]
        private Dictionary<FieldInfo, FieldInfo> _childParentMap;

        public static T CreateUninitialized<T>() where T : ForgeObject
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        public static ForgeObject CreateUninitialized(Type forgeObjectChildType)
        {
            return (ForgeObject)FormatterServices.GetUninitializedObject(forgeObjectChildType);
        }

        public static T CreateFromJson<T>(string json) where T : ForgeObject
        {
            return JsonUtility.FromJson<T>(json);
        }

        public static ForgeObject CreateFromJson(string json, Type forgeObjectChildType)
        {
            return (ForgeObject)JsonUtility.FromJson(json, forgeObjectChildType);
        }

        public static T Clone<T>(T original) where T : ForgeObject
        {
            return (T)original.MemberwiseClone();
        }

        public static ForgeObject Clone(ForgeObject forgeObject)
        {
            return (ForgeObject)forgeObject.MemberwiseClone();
        }

        public static T Create<T>() where T : ForgeObject, new()
        {
            return new T();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forgeObjectChildType"></param>
        /// <returns>An instance of the child type of the </returns>
        public static ForgeObject Create(Type forgeObjectChildType)
        {
#if UNITY_EDITOR
            if (forgeObjectChildType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new MissingMethodException("Cant find a parameterless constructor, please make sure that the ForgeObject child class has a parameterless constructor.");
            }
#endif

            return (ForgeObject)Activator.CreateInstance(forgeObjectChildType);
        }

        /// <summary>
        /// Get the data of this class as json string
        /// </summary>
        /// <returns>A json representation of the class.</returns>
        public override string ToString()
        {
            return ToJsonString();
        }

        /// <summary>
        /// Get the data of this class as json string, in the editor UnityEngine.Object and derived classes will be shown with a sceneGuid and an objectID
        /// </summary>
        /// <returns>A json representation of the class.</returns>
        public string ToJsonString()
        {
#if UNITY_EDITOR
            _jsonFields = GetForgeObjectJsonFields();            

            if (_editorJsonData == null && _jsonFields.Any(f => f.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || f.FieldType == typeof(UnityEngine.Object)))
            {
                Type _editorObjectExtensions = Type.GetType("HephaestusForge.UnityEditorObjectExtensions, Assembly-CSharp-Editor");
                MethodInfo _getSceneGuidAndObjectIDMethod = _editorObjectExtensions.GetMethod("GetSceneGuidAndObjectID", BindingFlags.Public | BindingFlags.Static);
                
                var json = JsonUtility.ToJson(this, true).Split('\n').ToList();

                for (int i = json.Count - 1; i >= 0 ; i--)
                {
                    if(_jsonFields.Any(f => json[i].Contains(f.Name)))
                    {
                        var field = Array.Find(_jsonFields, f => json[i].Contains(f.Name));

                        if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || field.FieldType == typeof(UnityEngine.Object))
                        {
                            if (_childParentMap.ContainsKey(field))
                            {
                                var nestedRoot = GetNestedRoot(field).ToArray().Reverse();
                            }
                            else
                            {
                                UnityEngine.Object obj = (UnityEngine.Object)field.GetValue(this);

                                if (obj)
                                {
                                    int objectID = 0;
                                    string sceneGuid = "";
                                    var args = new object[3] { obj, sceneGuid, objectID };

                                    _getSceneGuidAndObjectIDMethod.Invoke(null, args);

                                    for (int t = i + 1; t < json.Count; t++)
                                    {
                                        if (json[t].Contains("}"))
                                        {
                                            json.RemoveAt(t);
                                            break;
                                        }
                                        else
                                        {
                                            json.RemoveAt(t);
                                        }
                                    }

                                    json[i] = $"{{{sceneGuid}:{objectID}}}";
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _editorJsonData = JsonUtility.ToJson(this, true);
            }

            return _editorJsonData;
#else
            return JsonUtility.ToJson(this, true);
#endif
        }

        private IEnumerable<FieldInfo> GetNestedRoot(FieldInfo field)
        {
            while (_childParentMap.ContainsKey(field))
            {
                yield return field;
                field = _childParentMap[field];
            }
        }

        /// <summary>
        /// Get the names of fields that Json utility can access.
        /// Dont use { or } in string fields.
        /// </summary>
        /// <returns>The names of fields that the JsonUtility can access.</returns>
        public string[] GetJsonFieldNames()
        {
            if (_fieldNames == null)
            {
                string[] json = JsonUtility.ToJson(this, true).Split('\n');

                List<string> fieldNames = new List<string>();
                int startedSubClasses = 0;
                string subClassFields = "";

                for (int t = 0; t < json.Length; t++)
                {
                    var fieldName = json[t].Split(':')[0].Replace("\"", "").Trim();

                    if (!fieldName.Contains("{") && !fieldName.Contains("}"))
                    {
                        fieldNames.Add(subClassFields + fieldName);

                        if (json[t].Split(':')[1].Contains("{"))
                        {
                            startedSubClasses++;
                            subClassFields = $"{fieldName}.";
                        }
                    }

                    if (fieldName.Contains("}") && startedSubClasses > 0)
                    {
                        startedSubClasses--;

                        if (startedSubClasses == 0)
                        {
                            subClassFields = "";
                        }
                        else
                        {
                            var split = subClassFields.Split('.').ToList();
                            split.RemoveAt(split.Count - 1);
                            subClassFields = string.Join(".", split);
                        }
                    }
                }

                _fieldNames = fieldNames.ToArray();
            }

            return _fieldNames;
        }

        /// <summary>
        /// This will get all fields that the Json utility can access in this ForgeObject and nested ForgeObjects.
        /// Wont get fields from non nested ForgeObjects.
        /// </summary>
        /// <returns>Info about all fields which the JsonUtility can access.</returns>
        public FieldInfo[] GetForgeObjectJsonFields()
        {
            if (_jsonFields == null)
            {
                _childParentMap = new Dictionary<FieldInfo, FieldInfo>();
                List<FieldInfo> fieldInfos = new List<FieldInfo>();

                _fieldNames = GetJsonFieldNames();

                for (int i = 0; i < _fieldNames.Length; i++)
                {
                    FieldInfo field;
                    Type current = GetType();
                    string fieldName = _fieldNames[i];

                    do
                    {
                        field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                        current = current.BaseType;                       

                    } while (field == null && current != typeof(ForgeObject));

                    if (field != null)
                    {
                        if (field.FieldType.IsSubclassOf(typeof(ForgeObject)))
                        {
                            ForgeObject nested = (ForgeObject)field.GetValue(this);

                            var nestedFields = nested.GetForgeObjectJsonFields();
                            fieldInfos.AddRange(nestedFields);

                            for (int t = 0; t < nestedFields.Length; t++)
                            {
                                _childParentMap.Add(nestedFields[t], field);
                            }
                        }

                        fieldInfos.Add(field);
                    }
                }
            }

            return _jsonFields;
        }
    }
}