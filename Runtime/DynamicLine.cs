/// <summary>
///* To simulate a dynamic line that interacts with gravity
///* Objects can be attached to the line and will follow its shape
///* Or just have a line renderer
/// </summary>

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineSimulation
{
    public class DynamicLine : MonoBehaviour
    {
        #region Variables
        [SerializeField] Transform end;
        public bool hasEnd => end != null;
        public DynamicLineEditType Type = DynamicLineEditType.Straight;
        [HideInInspector] public bool edit = false;
        public LineParams lineParams;
        public Vector2[] Points;
        public int ID, startIndex;
        [SerializeField, HideInInspector] private float straightPointsDist = 0.3f;
        [SerializeField, HideInInspector] private float lineLength = 5f;

        #endregion

        #region Visuals variables

        [SerializeField] private LineRenderer lineRenderer;

        #endregion

        #region LineTypes
        void OnValidate()
        {
            if (Application.isPlaying) return;
            ComputePoints();
            UpdateVisuals();
        }

        public void ComputePoints()
        {
            Points ??= new Vector2[16];
            switch (Type)
            {
                case DynamicLineEditType.Straight:
                    Points = new Vector2[16];
                    Points[0] = Vector2.zero;
                    for (int i = 1; i < Points.Length; i++)
                    {
                        if (hasEnd) Points[i] = GetCurvePos(Vector2.zero, end.localPosition, lineLength * i / (Points.Length - 1), lineLength);
                        else Points[i] = Points[i - 1] - (Vector2)transform.up * straightPointsDist;
                    }
                    break;
                case DynamicLineEditType.Custom:
                    break;
                default:
                    return;
            }
        }

        void Update()
        {
            UpdateVisuals();
        }

        #endregion

        public void UpdateVisuals()
        {
            UpdateLineRenderer();
        }

        void UpdateLineRenderer()
        {
            if (lineRenderer == null) return;

            lineRenderer.positionCount = Points.Length;
            Vector3[] points3D = System.Array.ConvertAll(Points, point => new Vector3(point.x, point.y, 0) + transform.position);
            lineRenderer.SetPositions(points3D);
        }

        #region Simulation

        void Awake()
        {
            DynamicLineManager.lines ??= new List<DynamicLine>();

            if (!DynamicLineManager.lines.Contains(this))
            {
                DynamicLineManager.lines.Add(this);
            }
        }

        public void SetData(int Index, ref int nNodes, DynamicLineManager manager)
        {
            ID = Index;
            startIndex = nNodes;

            lineParams.worldPos = (Vector2)transform.position;
            lineParams.lineZ = transform.position.z;

            manager.lineParamsArray[ID] = lineParams;
            manager.linePivotsArray[ID] = (Vector2)transform.position;
            manager.lineEndsPivotsArray[ID] = GetEnd();

            for (int i = 0; i < Points.Length; i++)
            {
                LineNode node = new LineNode();
                node.x = Points[i].x + transform.position.x;
                node.y = Points[i].y + transform.position.y;
                node.parent = i == 0 ? -1 : nNodes + i - 1;
                if (i < Points.Length - 1) node.targetDist = Vector2.Distance(Points[i + 1], Points[i]);
                if (startIndex + i >= manager.lineNodesArray.Length)
                {
                    Debug.LogError("Max number of nodes for dynamic lines reached");
                    Debug.LogError("Node index: " + (startIndex + i));
                    Debug.LogError("lineNodesArray length: " + manager.lineNodesArray.Length);
                    return;
                }

                manager.lineNodesArray[startIndex + i] = node;
            }
            nNodes += Points.Length;
        }


        public Vector2 GetEnd()
        {
            if (!hasEnd) return Vector2.zero;
            return end.position;
        }

        #endregion

        #region Utility

        public Vector2 GetCurvePos(Vector2 start, Vector2 end, float t, float length)
        {
            Vector2 gravity = Vector2.up * 10f;
            Vector2 launchVelocity = ((end - start) / length) - gravity * length / 2.0f;

            return start + launchVelocity * t + gravity * t * t / 2.0f;

        }

        public Vector2 GetPositionOnCurve(Vector2 begin, Vector2 end, float t, float targetLength)
        {
            float resolution = 50f;
            float tolerance = 0.01f;
            // Midpoint between begin and end
            Vector2 mid = (begin + end) * 0.5f;

            // Initial vertical offset for control point (downward)
            float yOffset = -Mathf.Abs(end.y - begin.y);
            Vector2 control = new Vector2(mid.x, mid.y + yOffset);

            // Iteratively adjust control point to match curve length
            int maxIterations = 1000;
            float step = 0.1f;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Approximate length by sampling the curve
                float length = 0f;
                Vector2 prev = begin;
                for (int i = 1; i <= resolution; i++)
                {
                    float ti = i / (float)resolution;
                    Vector2 pt = EvaluateBezier(begin, control, end, ti);
                    length += Vector2.Distance(prev, pt);
                    prev = pt;
                }

                if (Mathf.Abs(length - targetLength) < tolerance)
                    break;

                // Adjust control point vertically to increase/decrease length
                if (length < targetLength)
                    control.y -= step;
                else
                    control.y += step;
            }

            // Now return the point on the curve at t
            return EvaluateBezier(begin, control, end, t);

        }

        private static Vector2 EvaluateBezier(Vector2 P0, Vector2 P1, Vector2 P2, float t)
        {
            return Mathf.Pow(1 - t, 2) * P0 +
                2 * (1 - t) * t * P1 +
                Mathf.Pow(t, 2) * P2;
        }

        #endregion

#if UNITY_EDITOR
        #region Gizmos
        void OnDrawGizmos()
        {
            Gizmos.color = Color.gray * 0.3f;
            for (int i = 0; i < Points.Length - 1; i++)
            {
                Gizmos.DrawLine(Points[i] + (Vector2)transform.position, Points[i + 1] + (Vector2)transform.position);
            }
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < Points.Length; i++)
            {
                Handles.DrawWireDisc(Points[i] + (Vector2)transform.position, Vector3.forward, 0.1f);
            }
            for (int i = 0; i < Points.Length - 1; i++)
            {
                Gizmos.DrawLine(Points[i] + (Vector2)transform.position, Points[i + 1] + (Vector2)transform.position);
            }
        }
        #endregion
#endif

    }

}
