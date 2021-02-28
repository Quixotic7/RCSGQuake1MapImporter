using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    // Declare type of Custom Editor
    [CustomEditor(typeof(RCSGQ1MapImporter))] //1
    class RCSGQ1MapImporterEditor : Editor
    {
        // OnInspector GUI
        public override void OnInspectorGUI() //2
        {
            if (GUILayout.Button("Import Map"))
            {
                ImportMap();
            }
        }

        private void ImportMap()
        {
            string path = EditorUtility.OpenFilePanel("Import Quake 1 Map", "", "map");
            if (path.Length != 0)
            {
                EditorUtility.DisplayProgressBar("RealtimeCSG: Importing Quake 1 Map", "Parsing Quake 1 Map File (*.map)...", 0.0f);
                var importer = new Quake1Importer.MapImporter();
                var map = importer.Import(path);

                var mapImporter = target as RCSGQ1MapImporter;

                Quake1Importer.MapWorldConverter.Import(mapImporter.transform, map);

                //Importers.Quake1.MapWorldConverter.Import(csgModel, map);



                //BrushFactory.CreateBrushFromPlanes()

                //UnityEditor.Selection.activeGameObject = gameObject;
                //Undo.RegisterCreatedObjectUndo(gameObject, "Created brush");
                InternalCSGModelManager.CheckForChanges();
                InternalCSGModelManager.UpdateMeshes();


            }


            try
            {
                
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Quake 1 Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
            }

            EditorUtility.ClearProgressBar();



            

            //Oper
            //BrushFactory.CreateBrushFromPlanes

        }

    }
}