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
    public class ForgeObject 
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

#pragma warning disable 0649

        [SerializeField, HideInInspector]
        private bool _polymorphEnabled;

        [SerializeField, HideInInspector]
        private string _polymorphismType;

        [SerializeField, HideInInspector]
        private string _polymorphismJsonData;

        [SerializeField, HideInInspector]
        private Internal.UnityEngineStringObjectPair[] _pairs;

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
        private static string InsertTargetObjectInstanceID(string json, Internal.UnityEngineStringObjectPair[] pairs)
        {
            string[] jsondata = json.Split('\n');

            for (int i = 0; i < jsondata.Length; i++)
            {
                if (jsondata[i].Contains("{") && jsondata[i].Contains("}"))
                {
                    GetSubStringBetweenChars(jsondata[i], '{', '}', out string full, out string inside);

                    if (inside.Contains("_sceneGuid:") && inside.Contains("_objectID:"))
                    {
                        var pair = Array.Find(pairs, p => p.Key == full);

                        if (pair != null && pair.Value)
                        {
                            int instanceID = pair.Value.GetInstanceID();
                            jsondata[i] = $"\"instanceID\": {instanceID}";
                        }
                    }
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
            var instance = JsonUtility.FromJson<T>(json);
            instance.Init();
            return instance;
        }

        public static ForgeObject CreateFromJson(string json, Type forgeObjectChildType)
        {
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

        public static T Polymorph<T>(T original) where T : ForgeObject
        {
            if (original._polymorphEnabled)
            {
                original._polymorphismJsonData = InsertTargetObjectInstanceID(original._polymorphismJsonData, original._pairs);

                var clone = (T)CreateFromJson(original._polymorphismJsonData, Type.GetType(original._polymorphismType));

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    clone._polymorphEnabled = original._polymorphEnabled;
                    clone._polymorphismType = original._polymorphismType;
                    clone._polymorphismJsonData = original._polymorphismJsonData;
                    clone._pairs = original._pairs;
                }
#endif
                clone.Init();
                return clone;
            }
            else
            {
                return original;
            }
        }

        public static ForgeObject Polymorph(ForgeObject original, Type forgeObjectChildType)
        {
            if (original._polymorphEnabled)
            {
                original._polymorphismJsonData = InsertTargetObjectInstanceID(original._polymorphismJsonData, original._pairs);
                var clone = CreateFromJson(original._polymorphismJsonData, forgeObjectChildType);

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    clone._polymorphEnabled = original._polymorphEnabled;
                    clone._polymorphismType = original._polymorphismType;
                    clone._polymorphismJsonData = original._polymorphismJsonData;
                    clone._pairs = original._pairs;
                }
#endif
                clone.Init();
                return clone;
            }
            else
            {
                return original;
            }
        }

#if UNITY_EDITOR
        private void EditorGetUnityEngineObjectIdenfication(int i, List<string> json, UnityEngine.Object target, MethodInfo getSceneGuidAndObjectIDMethod)
        {
            if (target != null)
            {
                List<int> removeAt = new List<int>();
                long objectID = 0;
                string sceneGuid = "";
                object[] args = new object[3] { target, sceneGuid, objectID };

                getSceneGuidAndObjectIDMethod.Invoke(null, args);

                for (int t = i + 2; t < json.Count; t++)
                {
                    if (json[t].Contains("}"))
                    {
                        break;
                    }
                    else
                    {
                        removeAt.Add(t);
                    }
                }

                for (int t = removeAt.Count - 1; t >= 0; t--)
                {
                    json.RemoveAt(removeAt[t]);
                }

                json[i + 1] = $"{{_sceneGuid:{args[1]}, _objectID:{args[2]}}}";
            }
        }
#endif

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
                                    EditorGetUnityEngineObjectIdenfication(i, json, (UnityEngine.Object)target, getSceneGuidAndObjectIDMethod);
                                }
                            }
                            else
                            {
                                UnityEngine.Object target = (UnityEngine.Object)field.GetValue(this);

                                EditorGetUnityEngineObjectIdenfication(i, json, target, getSceneGuidAndObjectIDMethod);                                
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
    }
}