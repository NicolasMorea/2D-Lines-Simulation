/// <summary>
///* To attach regular objects to a dynamic line
/// </summary>

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineSimulation
{
    public class RegularLineObject : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private DynamicLine line;
        [SerializeField] private GameObject obj;
        [SerializeField, Min(0.1f)] private float distanceBetweenObjects = 0.5f;
        [SerializeField] private bool rotateWithLine = true;
        [SerializeField, Min(0f)] private float min = 0.5f;
        [SerializeField, Min(0f)] private float max = 15f;
        [SerializeField] private bool autoReload = false;

        void OnValidate()
        {
            if (autoReload) ReloadObjects();
        }

        public void ReloadObjects()
        {
            if (Application.isPlaying) return;
            if (obj == null) return;
            if (line == null) return;

            float Length = DynamicLineManager.GetLength(line.Points);

            if (max > Length) max = Length;
            if (min > Length) min = Length;

            // if(obj.GetComponent<LineObject>() == null)
            // {
            //     Debug.LogWarning("RegularLineObject : " + obj.name + " is not a LineObject, it will not be attached to the line", this);
            //     return;
            // }

            //* remove children of type lineObject
            LineObject[] lineObjects = GetComponentsInChildren<LineObject>();
            foreach (LineObject lineObject in lineObjects)
            {
                GameObject garbage = lineObject.gameObject;
                EditorApplication.delayCall += () =>
                {
                    if (Application.isPlaying) return;
                    if (garbage != null)
                    {
                        Undo.DestroyObjectImmediate(garbage);
                    }
                };
            }


            float currentLength = min;
            while (currentLength <= max)
            {
                float position = currentLength;
                // GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(obj, line.transform);
                EditorApplication.delayCall += () =>
                {
                    if (Application.isPlaying) return;
                    GameObject newObj = Object.Instantiate(obj);
                    if (newObj == null)
                    {
                        Debug.LogWarning("RegularLineObject : " + obj.name + " is not a prefab, it will not be attached to the line", this);
                        return;
                    }
                    Undo.RegisterCreatedObjectUndo(newObj, "Spawn LineObject");
                    newObj.transform.SetParent(transform);

                    LineObject lineObject = newObj.GetComponent<LineObject>();
                    if (lineObject == null) lineObject = newObj.AddComponent<LineObject>();
                    lineObject.SetLine(line, position, rotateWithLine);
                    lineObject.name = obj.name + " " + position.ToString("F1") + "m";
                    newObj.SetActive(true);
                };
                currentLength += distanceBetweenObjects;
            }
        }
#endif
    }
}