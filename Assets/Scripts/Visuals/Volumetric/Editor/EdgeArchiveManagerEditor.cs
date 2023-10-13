using UnityEngine;
using UnityEditor;
using UnityTemplateProjects.Visuals;

namespace Visuals.Volumetric.Editor
{
    [CustomEditor(typeof(EdgeArchiveManager))]
    public class EdgeArchiveManagerEditor : UnityEditor.Editor
    {
        private EdgeArchiveManager  manager;
        
        void OnEnable()
        {
            manager = (EdgeArchiveManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Get All Edges On Scenes"))
            {
                manager.GetAllEdgesOnScene();
            }

            //GUILayout.Label ("This is a Label in a Custom Editor");
        }
        
        // public override void OnInspectorGUI()
        // {
        //     
        //     //return base.CreateInspectorGUI();
        // }
    }
}