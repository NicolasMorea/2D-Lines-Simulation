#if UNITY_EDITOR
namespace LineSimulation
{
    using UnityEngine;

    using UnityEditor;

    #region Editor

    [CustomEditor(typeof(DynamicLine))]
    public class DynamicLineEditor : Editor
    {
        private SerializedProperty straightPointsDist;
        private SerializedProperty lengthField;
        private SerializedProperty editField;

        private void OnEnable()
        {
            straightPointsDist = serializedObject.FindProperty("straightPointsDist");
            lengthField = serializedObject.FindProperty("lineLength");
            editField = serializedObject.FindProperty("edit");
        }

        private void OnSceneGUI()
        {
            DynamicLine dynamicLine = (DynamicLine)target;

            if (dynamicLine.Type == DynamicLineEditType.Straight)
            {
                return;
            }

            if (!dynamicLine.edit)
            {
                return;
            }
            // Draw and edit points
            for (int i = 0; i < dynamicLine.Points.Length; i++)
            {
                Vector2 point = dynamicLine.Points[i];
                Handles.color = Color.red;

                // Draw the handle
                Vector3 handlePosition = new Vector3(point.x, point.y, 0) + dynamicLine.transform.position;
                Vector3 newPosition = Handles.PositionHandle(handlePosition, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move Point");
                    dynamicLine.Points[i] = new Vector2(newPosition.x, newPosition.y) - (Vector2)dynamicLine.transform.position;
                    EditorUtility.SetDirty(target);
                    dynamicLine.UpdateVisuals();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DynamicLine dynamicLine = (DynamicLine)target;

            serializedObject.Update();

            DrawDefaultInspector();

            if (editField == null || lengthField == null || straightPointsDist == null)
            {
                Debug.LogError("DynamicLineEditor: Missing serialized properties.");
                return;
            }

            if (dynamicLine.Type == DynamicLineEditType.Custom)
            {
                EditorGUILayout.PropertyField(editField, new GUIContent("Edit Shape"));
            }
            else
            {
                // EditorGUILayout.PropertyField(straightPointsCount, new GUIContent("Point Count"));
                if (dynamicLine != null && dynamicLine.hasEnd)
                {
                    EditorGUILayout.PropertyField(lengthField, new GUIContent("Length"));
                }
                else
                {
                    EditorGUILayout.PropertyField(straightPointsDist, new GUIContent("Distance Between Points"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endregion
}
#endif