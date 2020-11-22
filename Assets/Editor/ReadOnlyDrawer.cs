using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

[SuppressMessage("ReSharper", "CheckNamespace")]
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
    {
        string valueString;

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                valueString = prop.intValue.ToString();
                break;
            case SerializedPropertyType.Boolean:
                valueString = prop.boolValue.ToString();
                break;
            case SerializedPropertyType.Float:
                valueString = prop.floatValue.ToString("f");
                break;
            case SerializedPropertyType.String:
                valueString = prop.stringValue;
                break;
            default:
                valueString = "(not supported)";
                break;
        }

        EditorGUI.LabelField(position, label.text, valueString);
    }
}
