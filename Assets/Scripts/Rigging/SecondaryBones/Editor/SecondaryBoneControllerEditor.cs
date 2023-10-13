using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SecondaryBoneController))]
public class SecondaryBoneControllerEditor : Editor
{
    SecondaryBoneController controller;

    private void OnEnable()
    {
        controller = target as SecondaryBoneController;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Find and assign bones"))
        {
            controller.FindBones();
        }
        if (GUILayout.Button("Clear bones"))
        {
            controller.ClearBones();
        }
    }
}
