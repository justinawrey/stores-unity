using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Persistence;
using UnityEditor;
using UnityEngine;

namespace Stores
{
    [CustomEditor(typeof(Store), editorForChildClasses: true)]
    public class StoreEditor : Editor
    {
        private const BindingFlags allBindingFlags = (BindingFlags)(-1);
        private readonly string[] propertyExclusionList = new string[] { "m_Script" };

        private string prettyString = null;
        private readonly float verticalSpace = 10;
        private Store store;

        public bool Persisted => JsonPersistence.JsonExists(store.FileName);
        public string PersistencePath => JsonPersistence.GetPersistencePath(store.FileName);

        private void OnEnable()
        {
            // Performance hell but FUCK IT its editor magic!
            // Doing this primes the cache which ensures no double OnGet calls,
            // Which means its actually safe to do OnGet subscribes
            // even when entering/exiting playmode
            store = Store.GetDynamic(target.GetType());
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeaderBar("Data Fields");

            foreach (var child in GetDirectChildren(serializedObject))
            {
                DrawChildProperty(child);
            }

            EditorGUILayout.Space(verticalSpace);
            DrawHeaderBar("Persistence Settings");
            DrawPersistenceState();
            DrawPersistenceButtons();

            if (prettyString == null && Persisted && !Application.isPlaying)
            {
                Load();
            }

            if (Persisted)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(prettyString);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderBar(string label, int lineOffset = 0, bool drawLabel = true)
        {
            Color c = new(0.1f, 0.1f, 0.1f, 0.1f);
            Rect r = EditorGUILayout.GetControlRect();
            r.y +=
                lineOffset
                * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            EditorGUI.DrawRect(r, c);

            if (drawLabel)
            {
                EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
            }
        }

        private void DrawChildProperty(SerializedProperty child)
        {
            if (propertyExclusionList.Contains(child.name))
            {
                return;
            }

            bool persisted = GetAttributes<JsonPropertyAttribute>(child).Length > 0;
            string label = ObjectNames.NicifyVariableName(child.name);
            GUIContent guiContent = new(persisted ? $"{label} [P]" : label);
            EditorGUILayout.PropertyField(child, guiContent);
        }

        private void DrawPersistenceState()
        {
            if (Persisted)
            {
                EditorGUILayout.LabelField($"Persisted at path: {PersistencePath}");
            }
            else
            {
                EditorGUILayout.LabelField("Not persisted");
            }
        }

        private async void DrawPersistenceButtons()
        {
            bool savePressed = GUILayout.Button("Save");
            bool deletePressed = false;
            bool loadPressed = false;

            if (Persisted)
            {
                deletePressed = GUILayout.Button("Delete");
                loadPressed = GUILayout.Button("Load");
            }

            if (savePressed)
            {
                prettyString = await Store.PersistToDisk(store);
            }

            if (loadPressed)
            {
                // NOTE: deserialize into an object, and then re-serialize into a formatted string.
                // inefficient, but fine in the editor.
                Load();
            }

            if (deletePressed)
            {
                File.Delete(PersistencePath);
                prettyString = null;
            }
        }

        private async void Load()
        {
            await Store.LoadFromDisk(store);

            // extra persist is not pretty, but w/e, I gotta get dat visual lol.
            prettyString = await Store.PersistToDisk(store);
        }

        private IEnumerable<SerializedProperty> GetDirectChildren(SerializedObject serializedObject)
        {
            SerializedProperty prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            do
            {
                yield return prop;
            } while (prop.NextVisible(false));
        }

        private T[] GetAttributes<T>(SerializedProperty serializedProperty, bool inherit = false)
            where T : Attribute
        {
            if (serializedProperty == null)
            {
                throw new ArgumentNullException(nameof(serializedProperty));
            }

            var targetObjectType = serializedProperty.serializedObject.targetObject.GetType();

            if (targetObjectType == null)
            {
                throw new ArgumentException(
                    $"Could not find the {nameof(targetObjectType)} of {nameof(serializedProperty)}"
                );
            }

            foreach (var pathSegment in serializedProperty.propertyPath.Split('.'))
            {
                var fieldInfo = targetObjectType.GetField(pathSegment, allBindingFlags);
                if (fieldInfo != null)
                {
                    return (T[])fieldInfo.GetCustomAttributes<T>(inherit);
                }

                var propertyInfo = targetObjectType.GetProperty(pathSegment, allBindingFlags);
                if (propertyInfo != null)
                {
                    return (T[])propertyInfo.GetCustomAttributes<T>(inherit);
                }
            }

            throw new ArgumentException(
                $"Could not find the field or property of {nameof(serializedProperty)}"
            );
        }
    }
}
