#if UNITY_EDITOR
namespace LineSimulation
{
    using UnityEngine;

    using UnityEditor;

    #region Editor

    [CustomEditor(typeof(DynamicLineManager))]
    public class DynamicLineManagerEditor : Editor
    {
        //* Variables for user input (line ID and local node ID)
        private int CutlineID = 0;
        private int CutlocalNodeID = 0;

        public override void OnInspectorGUI()
        {
            //* Draw the default inspector (so you don't lose default behavior)
            DrawDefaultInspector();

            //* Add space for the fields and button
            GUILayout.Space(10);

            //* Input fields for the line ID and local node ID
            CutlineID = EditorGUILayout.IntField("Line ID", CutlineID);
            CutlocalNodeID = EditorGUILayout.IntField("Local Node ID", CutlocalNodeID);

            GUILayout.Space(10);

            //* Reference the DynamicLineManager script (the target)
            DynamicLineManager manager = (DynamicLineManager)target;

            //* Add a button to call SimpleCut
            if (GUILayout.Button("Perform Simple Cut"))
            {
                manager.SliceLine(CutlineID, CutlocalNodeID);  // Call the SimpleCut method
                Debug.Log($"Performed SimpleCut on LineID: {CutlineID}, LocalNodeID: {CutlocalNodeID}");
            }

            if (GUILayout.Button("InvertGrav"))
            {
                manager.gravityForce *= -1;
                manager.shader?.SetFloat("gravityForce", manager.gravityForce);
            }
        }
    }

    #endregion
}

#endif