using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HephaestusForge
{
    [CustomPropertyDrawer(typeof(ForgeObject), true)]
    public class ForgeObjectPropertyDrawer : PropertyDrawer
    {
        private const string DONT_POLYMORPH = "Dont polymorph";

        private string[] _derivedtypes;
        private string[] _displayDerivedType;
        private Dictionary<string, Tuple<ForgeObject, FieldInfo[]>> _initialized = new Dictionary<string, Tuple<ForgeObject, FieldInfo[]>>();

        private void Init(SerializedProperty property)
        {
            Selection.selectionChanged -= OnEditorLostFocus;
            Selection.selectionChanged += OnEditorLostFocus;

            if(_derivedtypes == null)
            {
                List<string> derivedtypes = new List<string>();

                if (fieldInfo.FieldType.IsArray)
                {
                    derivedtypes = ForgeObject.EditorFindDerivedTypeNames(fieldInfo.FieldType.GetElementType()).ToList();
                }
                else
                {
                    derivedtypes = ForgeObject.EditorFindDerivedTypeNames(fieldInfo.FieldType).ToList();
                }

                if(derivedtypes.Count > 0)
                {
                    derivedtypes.Insert(0, DONT_POLYMORPH);
                }

                _derivedtypes = derivedtypes.ToArray();
                _displayDerivedType = new string[_derivedtypes.Length];

                for (int i = 0; i < _derivedtypes.Length; i++)
                {
                    _displayDerivedType[i] = _derivedtypes[i].Split(',')[0];
                }
            }

            var type = property.FindPropertyRelative("_polymorphismType");

            if (!string.IsNullOrEmpty(type.stringValue) && type.stringValue != DONT_POLYMORPH)
            {
                var forgeObjectType = Type.GetType(type.stringValue);

                ForgeObject forgeObject = null;

                var polymorphJsonData = property.FindPropertyRelative("_polymorphismJsonData");

                if (!string.IsNullOrEmpty(polymorphJsonData.stringValue))
                {
                    var original = fieldInfo.GetValue(property.serializedObject.targetObject);

                    forgeObject = ForgeObject.Polymorph((ForgeObject)original, forgeObjectType);
                }
                else
                {
                    try
                    {
                        forgeObject = ForgeObject.Create(forgeObjectType);
                    }
                    catch (MissingMethodException)
                    {
                        forgeObject = ForgeObject.CreateUninitialized(forgeObjectType);
                    }
                }

                _initialized.Add(property.propertyPath, new Tuple<ForgeObject, FieldInfo[]>(forgeObject, forgeObject.GetForgeObjectJsonFields()));
            }
            else
            {
                _initialized.Add(property.propertyPath, null);
            }
        }

        private void OnEditorLostFocus()
        {
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_initialized.ContainsKey(property.propertyPath))
            {
                Init(property);
            }

            if (property.isExpanded && _derivedtypes.Length > 0)
            {
                float height = EditorGUIUtility.singleLineHeight * 2;

                if (fieldInfo.FieldType != typeof(ForgeObject))
                {
                    var iterator = property.Copy();

                    while (iterator.NextVisible(true))
                    {
                        height += EditorGUIUtility.singleLineHeight;
                    }
                }
                else if (_initialized[property.propertyPath] != null)
                {
                    for (int i = 0; i < _initialized[property.propertyPath].Item2.Length; i++)
                    {
                        height += EditorGUIUtility.singleLineHeight;
                    }
                }               

                return height;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_initialized.ContainsKey(property.propertyPath))
            {
                Init(property);
            }

            position.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(position, property, label, false);

            if (property.isExpanded && _derivedtypes.Length > 0)
            {
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.indentLevel++;

                var type = property.FindPropertyRelative("_polymorphismType");

                int selected = Array.IndexOf(_derivedtypes, type.stringValue);

                if(selected < 0)
                {
                    selected = 0;
                }

                EditorGUI.BeginChangeCheck();

                GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
                selected = EditorGUI.Popup(position, "Polymorph", selected, _displayDerivedType);
                GUI.enabled = true;
                position.y += EditorGUIUtility.singleLineHeight;
                type.stringValue = _derivedtypes[selected];

                if(EditorGUI.EndChangeCheck() && selected > 0)
                {
                    property.FindPropertyRelative("_polymorphEnabled").boolValue = true;
                    property.FindPropertyRelative("_polymorphismJsonData").stringValue = string.Empty;
                    property.FindPropertyRelative("_pairs").arraySize = 0;

                    property.serializedObject.ApplyModifiedProperties();
                    var forgeObjectType = Type.GetType(type.stringValue);
                    ForgeObject forgeObject = null;

                    try
                    {
                        forgeObject = ForgeObject.Create(forgeObjectType);
                    }
                    catch (MissingMethodException)
                    {
                        forgeObject = ForgeObject.CreateUninitialized(forgeObjectType);
                    }

                    _initialized[property.propertyPath] = new Tuple<ForgeObject, FieldInfo[]>(forgeObject, forgeObject.GetForgeObjectJsonFields());

                    Draw(position, property);                    
                }
                else if(selected > 0)
                {
                    Draw(position, property);
                }
                else
                {
                    if(fieldInfo.FieldType != typeof(ForgeObject))
                    {
                        var iterator = property.Copy();

                        while (iterator.NextVisible(true))
                        {
                            EditorGUI.PropertyField(position, iterator);
                            position.y += EditorGUIUtility.singleLineHeight;
                        }
                    }

                    property.FindPropertyRelative("_polymorphEnabled").boolValue = false;
                    property.FindPropertyRelative("_polymorphismJsonData").stringValue = string.Empty;
                    property.FindPropertyRelative("_pairs").arraySize = 0;

                    _initialized[property.propertyPath] = null;
                    property.serializedObject.ApplyModifiedProperties();                    
                }
            }
        }

        private void Draw(Rect position, SerializedProperty property)
        {
            FieldInfo[] fields = _initialized[property.propertyPath].Item2;
            Type forgeObjectType = _initialized[property.propertyPath].Item1.GetType();

            do
            {
                var fieldsOnCurrentType = fields.Where(f => f.DeclaringType == forgeObjectType).ToArray();

                for (int i = 0; i < fieldsOnCurrentType.Length; i++)
                {
                    if (fieldsOnCurrentType[i].FieldType.IsArray)
                    {
                        DrawCollection(property, _initialized[property.propertyPath].Item1, fieldsOnCurrentType[i], ref position);
                    }
                    else
                    {
                        DrawSingleElement(property, _initialized[property.propertyPath].Item1, fieldsOnCurrentType[i], ref position);
                    }
                }

                forgeObjectType = forgeObjectType.BaseType;

            } while (forgeObjectType != typeof(ForgeObject));
        }

        private void DrawCollection(SerializedProperty property, ForgeObject forgeObject, FieldInfo fieldInfo, ref Rect position)
        {
        }

        private void DrawSingleElement(SerializedProperty property, ForgeObject forgeObject, FieldInfo fieldInfo, ref Rect position)
        {
            if(fieldInfo.FieldType == typeof(int))
            {
                DrawIntElement(property, forgeObject, fieldInfo, ref position);
            }
            else if (fieldInfo.FieldType == typeof(string))
            {
                DrawStringElement(property, forgeObject, fieldInfo, ref position);
            }
            else if(fieldInfo.FieldType == typeof(UnityEngine.Object) || fieldInfo.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                DrawObjectElement(property, forgeObject, fieldInfo, ref position);
            }
        }

        private void DrawObjectElement(SerializedProperty property, ForgeObject forgeObject, FieldInfo fieldInfo, ref Rect position)
        {
            UnityEngine.Object current = (UnityEngine.Object)fieldInfo.GetValue(forgeObject);

            EditorGUI.BeginChangeCheck();

            current = EditorGUI.ObjectField(position, fieldInfo.Name, current, fieldInfo.FieldType, !property.serializedObject.targetObject.IsAsset());

            if (EditorGUI.EndChangeCheck())
            {
                fieldInfo.SetValue(forgeObject, current);
                current.GetSceneGuidAndObjectID(out string sceneGuid, out long objectID);

                var pairs = property.FindPropertyRelative("_pairs");
                string fieldPath = $"{fieldInfo.DeclaringType}.{fieldInfo.Name}";
                var element = pairs.FindInArray(s => s.FindPropertyRelative("_fieldPath").stringValue == fieldPath, out int index);

                if (index == -1)
                {
                    pairs.arraySize++;
                    var newestElement = pairs.GetArrayElementAtIndex(pairs.arraySize - 1);

                    newestElement.FindPropertyRelative("_key").stringValue = $"{{_sceneGuid:{sceneGuid}, _objectID:{objectID}}}";
                    newestElement.FindPropertyRelative("_value").objectReferenceValue = current;
                    newestElement.FindPropertyRelative("_fieldPath").stringValue = fieldPath;
                }
                else
                {
                    element.FindPropertyRelative("_key").stringValue = $"{{_sceneGuid:{sceneGuid}, _objectID:{objectID}}}";
                    element.FindPropertyRelative("_value").objectReferenceValue = current;
                }

                property.FindPropertyRelative("_polymorphismJsonData").stringValue = forgeObject.ToJsonString();
                property.serializedObject.ApplyModifiedProperties();
            }

            position.y += EditorGUIUtility.singleLineHeight;
        }

        private void DrawStringElement(SerializedProperty property, ForgeObject forgeObject, FieldInfo fieldInfo, ref Rect position)
        {
            string current = (string)fieldInfo.GetValue(forgeObject);

            EditorGUI.BeginChangeCheck();

            current = EditorGUI.TextField(position, fieldInfo.Name, current);

            if (EditorGUI.EndChangeCheck())
            {
                fieldInfo.SetValue(forgeObject, current);

                property.FindPropertyRelative("_polymorphismJsonData").stringValue = forgeObject.ToJsonString();
                property.serializedObject.ApplyModifiedProperties();
            }

            position.y += EditorGUIUtility.singleLineHeight;
        }

        private void DrawIntElement(SerializedProperty property, ForgeObject forgeObject, FieldInfo fieldInfo, ref Rect position)
        {
            int current = (int)fieldInfo.GetValue(forgeObject);

            EditorGUI.BeginChangeCheck();

            current = EditorGUI.IntField(position, fieldInfo.Name, current);

            if (EditorGUI.EndChangeCheck())
            {
                fieldInfo.SetValue(forgeObject, current);

                property.FindPropertyRelative("_polymorphismJsonData").stringValue = forgeObject.ToJsonString();
                property.serializedObject.ApplyModifiedProperties();
            }

            position.y += EditorGUIUtility.singleLineHeight;
        }
    }
}