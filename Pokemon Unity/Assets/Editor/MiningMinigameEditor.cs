using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

[CustomEditor(typeof(MiningGameHandler))]
public class MiningGameHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var t = (MiningGameHandler)target;
        if (GUILayout.Button("Reinitialize Board"))
            t.ResetBoard();
    }
}

[CustomEditor(typeof(BoardShape))]
public class BoardShapeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var t = (BoardShape)target;
        //debug buttons
        if (GUILayout.Button("rotate 90")) 
        { t.Rotate(90);
        }if (GUILayout.Button("rotate 180")) 
        { t.Rotate(180);
        }if (GUILayout.Button("rotate 270")) 
        { t.Rotate(270);
        }
        if (t.baseShape == null || t.baseShape.Length != t.width * t.height)
        {
            //set and initialize the 2D array
            t.baseShape = new bool[t.width * t.height];
            for(int j = 0; j < t.height; j++)
            {
                for (int i = 0; i < t.width; i++)
                {
                    t.baseShape[j * t.width + i] = true;
                }
            }
        }
        GUILayout.Label("Shape");
        if (t.baseShape != null)
        {
            for (int y = 0; y < t.height; y++)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(t.width * 20));
                for (int x = 0; x < t.width; x++)
                {
                    t.baseShape[y * t.width + x] = GUILayout.Toggle(t.baseShape[y * t.width + x], "");
                }
                GUILayout.EndHorizontal();
            }
        }
        t.Item = EditorGUILayout.Popup(t.Item, ItemDatabase.ItemsNameList());
    }
}

//[CustomPropertyDrawer(typeof(BoardShape))]
//public class ShapeEditor : PropertyDrawer
//{
//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        var shapeProperty = property.FindPropertyRelative("baseShape");
//        var width = property.FindPropertyRelative("width").intValue;
//        var height = property.FindPropertyRelative("height").intValue;
//        //if (shapeProperty == null)
//        //{
//        //    var width = property.FindPropertyRelative("width");
//        //    var height = property.FindPropertyRelative("height");
//        //    var newShape = new bool[width.intValue, height.intValue];
//        //    shapeProperty = newShape;
//        //}
//        EditorGUI.PrefixLabel(position, label);
//        EditorGUI.PropertyField(position, property, GUIContent.none);
//        if (shapeProperty == null || shapeProperty.arraySize < height * width)
//            shapeProperty = new bool[height * width];
//        for (int y = 0; y < height; y++)
//        {
//            EditorGUILayout.BeginHorizontal();
//            for (int x = 0; x < width; x++)
//            {

//                EditorGUI.PropertyField(position, property.FindProperty($"baseShape[{y * x + x}]"), GUIContent.none);
//            }
//        EditorGUILayout.EndHorizontal();
//        }
//    }
//}

