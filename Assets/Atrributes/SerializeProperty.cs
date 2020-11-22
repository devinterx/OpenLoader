using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "CheckNamespace")]
[AttributeUsage(AttributeTargets.Field)]
public class SerializeProperty : PropertyAttribute
{
    public string PropertyName { get; }

    public SerializeProperty(string propertyName)
    {
        PropertyName = propertyName;
    }
}
