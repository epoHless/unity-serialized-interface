using System;
using UnityEngine;

[AttributeUsage( AttributeTargets.Field )]
public class RequireInterfaceAttribute : PropertyAttribute
{
    public readonly Type InterfaceType;
    
    public RequireInterfaceAttribute( Type interfaceType )
    {
        Debug.Assert( interfaceType.IsInterface, $"{nameof(interfaceType)} must be an interface" );
        InterfaceType = interfaceType;
    }
}