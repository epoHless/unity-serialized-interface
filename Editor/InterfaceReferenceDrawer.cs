using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(InterfaceReference<>))]
[CustomPropertyDrawer(typeof(InterfaceReference<,>))]
public class InterfaceReferenceDrawer : PropertyDrawer 
{
    const string underlyingValuePropertyName = "underlyingValue";
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var underlyingValueProperty = property.FindPropertyRelative(underlyingValuePropertyName);
        var args = GetArguments( fieldInfo );
        
        EditorGUI.BeginProperty(position, label, property);
        
        var assignedObject = EditorGUI.ObjectField(position, label, underlyingValueProperty.objectReferenceValue, typeof(UnityEngine.Object), true);

        if ( assignedObject != null )
        {
            Object component = null;
            
            if ( assignedObject is GameObject gameObject )
            {
                component = gameObject.GetComponent( args.ObjectType );
            }
            else if (args.InterfaceType.IsAssignableFrom(assignedObject.GetType()))
            {
                component = assignedObject;
            }
            
            if ( component != null )
            {
                ValidateAndAssignObject( underlyingValueProperty, component, component.name, args.InterfaceType.Name );
            }
            else
            {
                underlyingValueProperty.objectReferenceValue = null;
            }
        }
        else
        {
            underlyingValueProperty.objectReferenceValue = null;
        }
        
        EditorGUI.EndProperty();
        
        InterfaceReferenceUtils.OnGUI( position, property, label, args );
    }
    
    static InterfaceArgs GetArguments( System.Reflection.FieldInfo fieldInfo )
    {
        Type objectType = null, interfaceType = null;
        Type fieldType = fieldInfo.FieldType;
        
        bool TryGetTypesFromInterfaceReference( Type type, out Type objType, out Type intfType )
        {
            objType = intfType = null;
            
            if (type?.IsGenericType != true) return false;
            
            var genericType = type.GetGenericTypeDefinition();

            if ( genericType == typeof(InterfaceReference<>) ) type = type.BaseType;
            
            if ( type?.GetGenericTypeDefinition() == typeof(InterfaceReference<,>) ) 
            {
                var types = type.GetGenericArguments();
                intfType = types[0];
                objType = types[1];
                return true;
            }
            
            return false;
        }
        
        void GetTypesFromList( Type type, out Type objType, out Type intfType )
        {
            objType = intfType = null;
            
            var listInterface = type.GetInterfaces()
                                    .FirstOrDefault( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>) );

            if ( listInterface != null )
            {
                var elementType = listInterface.GetGenericArguments()[0];
                TryGetTypesFromInterfaceReference( elementType, out objType, out intfType );
            }
        }
        
        if ( !TryGetTypesFromInterfaceReference( fieldType, out objectType, out interfaceType ) )
        {
            GetTypesFromList( fieldType, out objectType, out interfaceType );
        }
        
        return new InterfaceArgs( objectType, interfaceType );
    }

    static void ValidateAndAssignObject( SerializedProperty property, Object targetObject, string componentNameOrType, string intergaceName = null )
    {
        if ( targetObject != null )
        {
            property.objectReferenceValue = targetObject;
        }
        else
        {
            Debug.LogWarning( @$"The {(intergaceName != null 
                ? $"GameObject '{componentNameOrType}'"
                : $"assigned object")} does not have a component that implements '{componentNameOrType}'." );
            
            property.objectReferenceValue = null;
        }
    }
}

public struct InterfaceArgs
{
    public readonly Type ObjectType;
    public readonly Type InterfaceType;
    
    public InterfaceArgs( Type objectType, Type interfaceType )
    {
        Debug.Assert( typeof(Object).IsAssignableFrom(objectType), $"{nameof(objectType)} must be a subclass of {nameof(Object)}" );
        Debug.Assert( interfaceType.IsInterface, $"{nameof(interfaceType)} must be an interface" );
        
        ObjectType = objectType;
        InterfaceType = interfaceType;
    }
}