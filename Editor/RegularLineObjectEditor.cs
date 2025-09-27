#if UNITY_EDITOR
namespace LineSimulation
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(RegularLineObject))]
    public class RegularLineObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            RegularLineObject regularLineObject = (RegularLineObject)target;
            if (GUILayout.Button("Reload Objects"))
            {
                regularLineObject.ReloadObjects();
            }
        }
    }
}
#endif