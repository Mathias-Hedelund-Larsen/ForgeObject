using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
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

        private static string InsertObjectID(string json)
        {
            string[] jsondata = json.Split('\n');

            for (int i = 0; i < jsondata.Length; i++)
            {
                if (jsondata[i].Contains("{") && jsondata[i].Contains("}"))
                {
                    GetSubStringBetweenChars(jsondata[i], '{', '}', out string full, out string inside);

                    var split = inside.Split(':');

                    Type editorObjectExtensions = Type.GetType("HephaestusForge.UnityEditorObjectExtensions, Assembly-CSharp-Editor");
                    MethodInfo getObjectByInstanceIDMethod = editorObjectExtensions.GetMethod("GetObjectByInstanceID", BindingFlags.Public | BindingFlags.Static);

                    var obj = (UnityEngine.Object)getObjectByInstanceIDMethod.Invoke(null, new object[2] { int.Parse(split[1]), split[0] });

                    jsondata[i] = $"\"instanceID\":{obj.GetInstanceID()}";
                }
            }

            return string.Join("\n", jsondata);
        }

        private static void GetSubStringBetweenChars(string origin, char start, char end, out string fullMatch, out string insideEncapsulation)
        {
            var match = Regex.Match(origin, string.Format(@"\{0}(.*?)\{1}", start, end));
            fullMatch = match.Groups[0].Value;
            insideEncapsulation = match.Groups[1].Value;
        }

        public static T CreateUninitialized<T>() where T : ForgeObject
        {
            var instance = (T)FormatterServices.GetUninitializedObject(typeof(T));
            instance.Init();
            return instance;
        }

        public static ForgeObject CreateUninitialized(Type forgeObjectChildType)
        {
            var instance = (ForgeObject)FormatterServices.GetUninitializedObject(forgeObjectChildType);
            instance.Init();
            return instance;
        }

        public static T CreateFromJson<T>(string json) where T : ForgeObject
        {
#if UNITY_EDITOR
            json = InsertObjectID(json);
#endif

            var instance = JsonUtility.FromJson<T>(json);
            instance.Init();
            return instance;
        }

        public static ForgeObject CreateFromJson(string json, Type forgeObjectChildType)
        {
#if UNITY_EDITOR
            json = InsertObjectID(json);
#endif

            var instance = (ForgeObject)JsonUtility.FromJson(json, forgeObjectChildType);
            instance.Init();
            return instance;
        }

        public static T Clone<T>(T original) where T : ForgeObject
        {
            var clone = (T)original.MemberwiseClone();
            clone.Init();
            return clone;
        }

        public static ForgeObject Clone(ForgeObject forgeObject)
        {
            var clone = (ForgeObject)forgeObject.MemberwiseClone();
            clone.Init();
            return clone;
        }

        public static T Create<T>() where T : ForgeObject, new()
        {
            var instance = new T();
            instance.Init();
            return instance;
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
            var instance = (ForgeObject)Activator.CreateInstance(forgeObjectChildType);
            instance.Init();
            return instance;
        }

        private void GetUnityEngineObjectIdenfication(int i, List<string> json, object target, MethodInfo getSceneGuidAndObjectIDMethod)
        {
            if (target != null)
            {
                int objectID = 0;
                string sceneGuid = "";
                var args = new object[3] { target, sceneGuid, objectID };

                getSceneGuidAndObjectIDMethod.Invoke(null, args);

                for (int t = i + 2; t < json.Count; t++)
                {
                    if (json[t].Contains("}"))
                    {
                        break;
                    }
                    else
                    {
                        json.RemoveAt(t);
                    }
                }

                json[i + 1] = $"{{{args[1]}:{args[2]}}}";
            }
        }

        private IEnumerable<FieldInfo> GetNestedRoot(FieldInfo field)
        {
            while (_childParentMap.ContainsKey(field))
            {
                yield return field;
                field = _childParentMap[field];
            }

            yield return field;
        }

        protected virtual void Init() { }

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
                Type editorObjectExtensions = Type.GetType("HephaestusForge.UnityEditorObjectExtensions, Assembly-CSharp-Editor");
                MethodInfo getSceneGuidAndObjectIDMethod = editorObjectExtensions.GetMethod("GetSceneGuidAndObjectID", BindingFlags.Public | BindingFlags.Static);
                
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
                                var nestedRoot = GetNestedRoot(field).Reverse().ToArray();

                                var target = (object)this;

                                for (int t = 0; t < nestedRoot.Length; t++)
                                {
                                    target = nestedRoot[t].GetValue(target);
                                }

                                GetUnityEngineObjectIdenfication(i, json, target, getSceneGuidAndObjectIDMethod);
                            }
                            else
                            {
                                UnityEngine.Object target = (UnityEngine.Object)field.GetValue(this);

                                GetUnityEngineObjectIdenfication(i, json, target, getSceneGuidAndObjectIDMethod);                                
                            }
                        }
                    }
                }

                _editorJsonData = string.Join("\n", json);
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

                _jsonFields = fieldInfos.ToArray();
            }

            return _jsonFields;
        }
    }
}