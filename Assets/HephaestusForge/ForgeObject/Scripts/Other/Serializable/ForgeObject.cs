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
        [Serializable]
        public abstract class LightWeight
        {
            public static T Instantiate<T>(IReadSerializedData reader) where T : LightWeight, new()
            {
                T instance = new T();
                instance.Init(reader);
                return instance;
            }

            protected abstract void Init(IReadSerializedData reader);
        }

        [Serializable]
        public sealed class Reference
        {
            [SerializeField]
            private string _guid;

            public void LazyReferenceForgeObject<T>(Action<T> action) where T : ForgeObject, new()
            {

            }

            //public T GetReferenceOrInstantiate<T>()
            //{

            //}
        }

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

#pragma warning disable 0649

        [SerializeField, HideInInspector]
        private bool _polymorphEnabled;

        [SerializeField, HideInInspector]
        private string _polymorphismType;

        [SerializeField, HideInInspector]
        private string _polymorphismJsonData;

        [SerializeField, HideInInspector]
        private SerializedData _polymorphismData;

#pragma warning restore 0649

#if UNITY_EDITOR
        public static IEnumerable<string> EditorFindDerivedTypeNames(Type parent)
        {
            var assemblyDefinitions = UnityEditor.AssetDatabase.FindAssets("t:asmdef");
            List<UnityEngine.Object> assemblyObjects = new List<UnityEngine.Object>();

            for (int i = 0; i < assemblyDefinitions.Length; i++)
            {
                assemblyObjects.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(assemblyDefinitions[i])));
            }

            for (int i = assemblyObjects.Count - 1; i >= 0; i--)
            {
                if (assemblyObjects[i].name.Contains("Editor") || assemblyObjects[i].name.Contains("Tests") || assemblyObjects[i].name.Contains("Analytics"))
                {
                    assemblyObjects.RemoveAt(i);
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("Assembly-CSharp") && !a.FullName.Contains("Editor")
                || assemblyObjects.Any(def => def.name.Contains(a.GetName().Name))).ToArray();

            for (int i = 0; i < assemblies.Length; i++)
            {
                var assemblyDerivedTypes = assemblies[i].GetTypes().Where(t => t.IsSubclassOf(parent) && !t.IsAbstract).ToArray();

                for (int t = 0; t < assemblyDerivedTypes.Length; t++)
                {
                    yield return $"{assemblyDerivedTypes[t].AssemblyQualifiedName}";
                }
            }
        }
#endif

        public static T Instantiate<T>(T original) where T : ForgeObject, new()
        {
            T instance = new T();
            instance.Init(original._polymorphismData);
            return instance;
        }

        public static T Instantiate<T>(IReadSerializedData reader) where T : ForgeObject, new()
        {
            T instance = new T();
            instance.Init(reader);
            return instance;
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

        protected abstract void Init(IReadSerializedData reader);

        protected abstract void Serialize(IWriteSerializedData writer);

        /// <summary>
        /// Get the data of this class as json string, in the editor UnityEngine.Object and derived classes will be shown with a sceneGuid and an objectID
        /// </summary>
        /// <returns>A json representation of the class.</returns>
        public string ToJsonString()
        {
#if UNITY_EDITOR
            _jsonFields = GetForgeObjectJsonFields();            

            if (_jsonFields.Any(f => f.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || f.FieldType == typeof(UnityEngine.Object)))
            {
                var json = JsonUtility.ToJson(this, true).Split('\n').ToList();
                Type unityEditorObjectExtensions = Type.GetType("HephaestusForge.UnityEditorObjectExtensions, Assembly-CSharp-Editor");
                MethodInfo getSceneGuidAndObjectIDMethod = unityEditorObjectExtensions.GetMethod("GetSceneGuidAndObjectID", BindingFlags.Public | BindingFlags.Static);

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

                                if (target is UnityEngine.Object)
                                {                                    
                                    //EditorGetUnityEngineObjectIdenfication(i, json, (UnityEngine.Object)target, getSceneGuidAndObjectIDMethod);
                                }
                            }
                            else
                            {
                                UnityEngine.Object target = (UnityEngine.Object)field.GetValue(this);

                                //EditorGetUnityEngineObjectIdenfication(i, json, target, getSceneGuidAndObjectIDMethod);                                
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

                        if (json[t].Split(':')[1].Contains("{") && !json[t].Split(':')[1].Contains("\""))
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

        internal SerializedData Serialized()
        {
            _polymorphismData = new SerializedData();
            Serialize(_polymorphismData);
            return _polymorphismData;
        }
    }
}