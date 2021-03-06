using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Linq;
using Surrogates;

namespace DifferentMethods.Univents
{

    [CustomPropertyDrawer(typeof(Call), true)]
    public class CallDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var h = EditorGUIUtility.singleLineHeight * 2;
            if (label != GUIContent.none)
                h += EditorGUIUtility.singleLineHeight;
            return h + 4;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var error = property.FindPropertyRelative("error");
            var hasLabel = label != GUIContent.none;
            if (Application.isPlaying && error.stringValue != "")
            {
                EditorGUI.HelpBox(position, error.stringValue, MessageType.Error);
                return;
            }

            var width = position.width - 4;
            GUI.Box(position, GUIContent.none);
            position.y += 2;
            position.x += 2;
            position.height -= 4;
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            // EditorGUI.indentLevel = 0;
            var rect = position;
            rect.height = position.height / (hasLabel ? 3 : 2);
            if (hasLabel)
            {
                GUI.Label(rect, label, EditorStyles.boldLabel);
                rect.y += rect.height;
            }
            rect.width = width * 0.2f;

            var gameObject = DrawGameObjectField(rect, property);
            rect.x += rect.width;
            rect.width = width * 0.79f;
            DrawMethodSelector(rect, property, gameObject);
            var metaMethodInfoProperty = property.FindPropertyRelative("metaMethodInfo");
            rect.x = position.x + position.width * 0.2f;
            rect.y += rect.height;
            rect.width = width * 0.55f;
            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                DrawFields(rect, property, metaMethodInfoProperty);
                if (cc.changed)
                {
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private void DrawFields(Rect position, SerializedProperty property, SerializedProperty metaMethodInfoProperty)
        {
            var componentTypeName = metaMethodInfoProperty.FindPropertyRelative("type").stringValue;
            var methodName = metaMethodInfoProperty.FindPropertyRelative("name").stringValue;
            var parameterTypeNames = metaMethodInfoProperty.FindPropertyRelative("parameterTypeNames");
            var parameterTypes = new List<System.Type>();
            for (var i = 0; i < parameterTypeNames.arraySize; i++)
            {
                var typeName = parameterTypeNames.GetArrayElementAtIndex(i).stringValue;
                parameterTypes.Add(System.Type.GetType(typeName));
            }
            var componentType = System.Type.GetType(componentTypeName);
            if (componentType == null) return;
            var mi = componentType.GetMethod(methodName, parameterTypes.ToArray());
            if (mi == null) return;
            var hotCall = property.GetTargetObject() as Call;
            var arguments = hotCall.arguments;
            // GUI.Box(position, GUIContent.none);
            foreach (var p in mi.GetParameters())
            {
                var nameLabel = new GUIContent($"{p.Name}: ");
                GUI.Label(position, nameLabel);
                var size = GUI.skin.label.CalcSize(nameLabel);
                position.x += size.x;
                var obj = arguments.Get(p.Name, p.ParameterType);
                var newObj = obj;
                position.x += DrawFieldEditor(position, p.ParameterType, obj, out newObj).width + 2;
                if (obj != newObj)
                {
                    arguments.Set(p.Name, newObj);
                }
            }
        }

        void DrawMethodSelector(Rect position, SerializedProperty property, GameObject gameObject)
        {
            var niceName = property.FindPropertyRelative("metaMethodInfo").FindPropertyRelative("niceName").stringValue;
            var content = new GUIContent(niceName, niceName);
            if (EditorGUI.DropdownButton(position, content, FocusType.Passive))
            {
                var menu = CreateMenu(gameObject, property);
                menu.DropDown(position);
            }
        }

        GameObject DrawGameObjectField(Rect position, SerializedProperty property)
        {
            var gameObjectProperty = property.FindPropertyRelative("gameObject");
            EditorGUI.PropertyField(position, gameObjectProperty, GUIContent.none);
            var componentProperty = property.FindPropertyRelative("component");
            var component = componentProperty.objectReferenceValue;
            var gameObject = gameObjectProperty.objectReferenceValue as GameObject;
            //make sure component is always child of the selected gameObject
            if (gameObject != null && component != null)
                componentProperty.objectReferenceValue = gameObject.GetComponent(component.GetType());
            return gameObject;
        }

        Rect DrawFieldEditor(Rect position, Type type, object obj, out object newObj)
        {
            var rect = position;
            EditorGUI.BeginChangeCheck();
            if (type == typeof(float))
            {
                rect.width = 32;
                obj = EditorGUI.FloatField(rect, (float)(obj)); ;
            }
            else if (type == typeof(int))
            {
                rect.width = 32;
                obj = EditorGUI.IntField(rect, (int)(obj));
            }
            else if (type == typeof(string))
            {
                rect.width = 96;
                obj = EditorGUI.TextField(rect, (string)obj);
            }
            else if (type == typeof(bool))
            {
                rect.width = 32;
                obj = EditorGUI.Toggle(rect, (bool)(obj));
            }
            else if (type.IsSubclassOf(typeof(System.Enum)))
            {
                rect.width = 96;
                obj = EditorGUI.EnumPopup(rect, (System.Enum)(obj));
            }
            else if (type == typeof(Vector3))
            {
                rect.width = 196;
                obj = EditorGUI.Vector3Field(rect, GUIContent.none, (Vector3)(obj));
            }
            else if (type == typeof(Vector2))
            {
                rect.width = 196 * 0.6666666f;
                obj = EditorGUI.Vector2Field(rect, GUIContent.none, (Vector2)(obj));
            }
            else if (type == typeof(Vector4))
            {
                rect.width = 196 * 1.3333333f;
                obj = EditorGUI.Vector4Field(rect, GUIContent.none, (Vector4)(obj));
            }
            else if (type == typeof(Color))
            {
                rect.width = 48;
                obj = EditorGUI.ColorField(rect, GUIContent.none, (Color)(obj));
            }
            else if (type == typeof(LayerMask))
            {
                rect.width = 96;
                obj = LayerMaskField(rect, (LayerMask)(obj));
            }
            else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                rect.width = 96;
                obj = EditorGUI.ObjectField(rect, GUIContent.none, (UnityEngine.Object)(obj), type, true);
            }
            if (EditorGUI.EndChangeCheck())
            {

            }
            newObj = obj;
            rect.x += rect.width;
            return rect;
        }


        public LayerMask LayerMaskField(Rect rect, LayerMask layerMask)
        {
            var layers = InternalEditorUtility.layers;
            var layerNumbers = new List<int>();
            layerNumbers.Clear();

            for (int i = 0; i < layers.Length; i++)
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = EditorGUI.MaskField(rect, GUIContent.none, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;

            return layerMask;
        }

        protected virtual GenericMenu CreateMenu(GameObject go, SerializedProperty property)
        {
            var menu = new GenericMenu();
            var hotCall = property.GetTargetObject() as Call;
            if (go != null)
            {

                var item = "GameObject/";
                foreach (var mi in typeof(GameObject).GetMethods().OrderBy(x => x.Name))
                {
                    if (IsSupportedMethod(mi, property, hotCall))
                    {
                        menu.AddItem(new GUIContent(item + SurrogateEditorExtensions.GetNiceName(typeof(GameObject), mi)), false, AddCall(go, property, null, mi));
                    }
                }
                foreach (var c in go.GetComponents(typeof(Component)).OrderBy(x => x.GetType().Name))
                {
                    var ct = c.GetType();
                    item = $"{ct.Name}/";
                    foreach (var mi in ct.GetMethods().OrderBy(x => x.Name))
                    {
                        if (IsSupportedMethod(mi, property, hotCall))
                        {
                            menu.AddItem(new GUIContent(item + SurrogateEditorExtensions.GetNiceName(ct, mi)), false, AddCall(go, property, c, mi));
                        }
                    }
                }

            }
            return menu;
        }

        protected virtual bool IsSupportedMethod(MethodInfo mi, SerializedProperty property, Call hotCall)
        {
            if (hotCall.GetType() == typeof(MethodCall))
                return SurrogateEditorExtensions.IsSupportedMethod(mi.DeclaringType, mi);
            if (hotCall.GetType() == typeof(PredicateCall))
                return SurrogateEditorExtensions.IsSupportedMethod(mi.DeclaringType, mi);
            return false;
        }

        string Signature(MethodInfo mi)
        {
            return string.Join(", ", (from i in mi.GetParameters() select i.Name));
        }

        protected static GenericMenu.MenuFunction AddCall(GameObject gameObject, SerializedProperty property, Component component, MethodInfo mi)
        {
            return () =>
            {
                var componentType = component == null ? typeof(GameObject) : component.GetType();
                property.FindPropertyRelative("component").objectReferenceValue = component;
                var metaMethodInfo = property.FindPropertyRelative("metaMethodInfo");
                metaMethodInfo.FindPropertyRelative("className").stringValue = SurrogateEditorExtensions.GetClassName(componentType, mi);
                metaMethodInfo.FindPropertyRelative("type").stringValue = componentType.AssemblyQualifiedName;
                metaMethodInfo.FindPropertyRelative("name").stringValue = mi.Name;
                metaMethodInfo.FindPropertyRelative("niceName").stringValue = SurrogateEditorExtensions.GetNiceName(componentType, mi);
                var typeNames = SurrogateEditorExtensions.GetParameterTypeNames(mi);
                var typeNamesProperty = metaMethodInfo.FindPropertyRelative("parameterTypeNames");
                typeNamesProperty.ClearArray();
                foreach (var typeName in typeNames)
                {
                    typeNamesProperty.InsertArrayElementAtIndex(typeNamesProperty.arraySize);
                    typeNamesProperty.GetArrayElementAtIndex(typeNamesProperty.arraySize - 1).stringValue = typeName;
                }
                property.serializedObject.ApplyModifiedProperties();
                // UniventCodeGenerator.Instance.AddMethod(componentType, mi);
            };

        }

    }
}
