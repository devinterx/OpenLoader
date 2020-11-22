using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[SuppressMessage("ReSharper", "CheckNamespace")]
[CustomPropertyDrawer(typeof(SerializeProperty))]
public class SerializePropertyDrawer : PropertyDrawer
{
    private PropertyInfo _propertyFieldInfo;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var target = property.serializedObject.targetObject;

        if (_propertyFieldInfo == null)
        {
            _propertyFieldInfo = target.GetType().GetProperty(((SerializeProperty) attribute).PropertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (_propertyFieldInfo != null)
        {
            var value = _propertyFieldInfo.GetValue(target, null);
            EditorGUI.BeginChangeCheck();
            value = DrawProperty(position, property.propertyType, _propertyFieldInfo.PropertyType, value, label);

            if (!EditorGUI.EndChangeCheck() || _propertyFieldInfo == null) return;
            UnityEditor.Undo.RecordObject(target, "Inspector");

            _propertyFieldInfo.SetValue(target, value, null);
        }
        else
        {
            EditorGUI.LabelField(position, "Error: could not retrieve property.");
        }
    }

    private static object DrawProperty(Rect position, SerializedPropertyType propertyType, Type type, object value,
        GUIContent label)
    {
        switch (propertyType)
        {
            case SerializedPropertyType.Integer:
                return EditorGUI.IntField(position, label, (int) value);
            case SerializedPropertyType.Boolean:
                return EditorGUI.Toggle(position, label, (bool) value);
            case SerializedPropertyType.Float:
                return EditorGUI.FloatField(position, label, (float) value);
            case SerializedPropertyType.String:
                return EditorGUI.TextField(position, label, (string) value);
            case SerializedPropertyType.Color:
                return EditorGUI.ColorField(position, label, (Color) value);
            case SerializedPropertyType.ObjectReference:
                return EditorGUI.ObjectField(position, label, (UnityEngine.Object) value, type, true);
            case SerializedPropertyType.ExposedReference:
                return EditorGUI.ObjectField(position, label, (UnityEngine.Object) value, type, true);
            case SerializedPropertyType.LayerMask:
                return EditorGUI.LayerField(position, label, (int) value);
            case SerializedPropertyType.Enum:
                return EditorGUI.EnumPopup(position, label, (Enum) value);
            case SerializedPropertyType.Vector2:
                return EditorGUI.Vector2Field(position, label, (Vector2) value);
            case SerializedPropertyType.Vector3:
                return EditorGUI.Vector3Field(position, label, (Vector3) value);
            case SerializedPropertyType.Vector4:
                return EditorGUI.Vector4Field(position, label, (Vector4) value);
            case SerializedPropertyType.Rect:
                return EditorGUI.RectField(position, label, (Rect) value);
            case SerializedPropertyType.AnimationCurve:
                return EditorGUI.CurveField(position, label, (AnimationCurve) value);
            case SerializedPropertyType.Bounds:
                return EditorGUI.BoundsField(position, label, (Bounds) value);
            default:
                throw new NotImplementedException("Unimplemented propertyType " + propertyType + ".");
        }
    }
}
