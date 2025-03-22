using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomPropertyDrawer( typeof(RequireInterfaceAttribute) )]
public class RequireInterfaceDrawer : PropertyDrawer
{
    RequireInterfaceAttribute Attribute => (RequireInterfaceAttribute) attribute;
    
    public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
    {
        var interfaceType = Attribute.InterfaceType;
        
        EditorGUI.BeginProperty( position, label, property );
        
        if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
        {
            DrawArrayField( position, property, label, interfaceType );
        }
        else
        {
            DrawInterfaceObjectField( position, property, label, interfaceType );
        }
        
        EditorGUI.EndProperty();
        
        var args = new InterfaceArgs( GetTypeOrElementType( fieldInfo.FieldType ), interfaceType );
        InterfaceReferenceUtils.OnGUI( position, property, label, args );
    }

    void DrawArrayField( Rect position, SerializedProperty property, GUIContent label, Type interfaceType )
    {
        property.arraySize = EditorGUI.IntField( new Rect( position.x, position.y, position.width, EditorGUIUtility.singleLineHeight ), label.text + " Size", property.arraySize );
        
        float yOffset = EditorGUIUtility.singleLineHeight;

        for ( int i = 0; i < property.arraySize; i++ )
        {
            var element = property.GetArrayElementAtIndex( i );
            var elementRect = new Rect( position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight );
            DrawInterfaceObjectField( elementRect, element, new GUIContent( label.text + $" Element {i}" ), interfaceType );
            yOffset += EditorGUIUtility.singleLineHeight;
        }
    }

    void DrawInterfaceObjectField( Rect position, SerializedProperty property, GUIContent label, Type interfaceType )
    {
        var oldReference = property.objectReferenceValue;
        var newReference = EditorGUI.ObjectField( position, label, oldReference, typeof( Object ), true );

        if ( newReference != null && newReference != oldReference )
        {
            ValidateAndAssignObject( property, newReference, interfaceType );
        }
        else if ( newReference == null )
        {
            property.objectReferenceValue = null;
        }
    }

    void ValidateAndAssignObject( SerializedProperty property, Object newReference, Type interfaceType )
    {
        if ( newReference is GameObject gameObject )
        {
            var component = gameObject.GetComponent( interfaceType );

            if ( component != null )
            {
                property.objectReferenceValue = component;
                return;
            }
        }
        else if ( interfaceType.IsAssignableFrom( newReference.GetType() ) )
        {
            property.objectReferenceValue = newReference;
            return;
        }
        
        Debug.LogWarning( $"Object {newReference} does not implement {interfaceType}." );
        property.objectReferenceValue = null;
    }

    Type GetTypeOrElementType( Type type )
    {
        if (type.IsArray) return type.GetElementType();
        if (type.IsGenericType) return type.GetGenericArguments()[0];
        return type;
    }
}