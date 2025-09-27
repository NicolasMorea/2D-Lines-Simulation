/// <summary>
///* To attach object to dynamic lines, they follow the line and its rotation
/// </summary>

using UnityEngine;

namespace LineSimulation
{
    public class LineObject : MonoBehaviour
    {
        [SerializeField] private DynamicLine line;
        [SerializeField, Min(0f)] private float position;
        [SerializeField] bool rotateWithLine = true;
        private float recordedPosition = -1f;
        private int nodeIndex = -1;
        private float nodeLerp = 0f;

        public void SetLine(DynamicLine newLine, float newPosition, bool rotate = true)
        {
            line = newLine;
            position = newPosition;
            rotateWithLine = rotate;
            PositionObject();
        }

        void Update()
        {
            if (recordedPosition != position) SetPosition(position);
            PositionObject();
        }

        public void SetPosition(float newPos) //* we record the node index and lerp necessary to set the position and orientation, to gain performance
        {
            recordedPosition = newPos;
            nodeIndex = DynamicLineManager.GetNodeIndexAtLength(position, line.Points);
            nodeLerp = DynamicLineManager.LengthAtIndex(nodeIndex, line.Points);
        }

        public void PositionObject()
        {
            if (line == null) return;
            if(line.Points.Length == 0) return;

            if(nodeIndex < 0) //* no optimizations, the version that runs in the editor
            {
                transform.localPosition = DynamicLineManager.GetLocalPosAtLength(position, line.Points);
                if(rotateWithLine) transform.up = -  DynamicLineManager.GetDirectionAtLength(position, line.Points);
                else transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            else //* more optimized, we dont loop on all the points
            {
                if(nodeIndex >= line.Points.Length - 1)
                {
                    transform.localPosition = line.Points[line.Points.Length - 1];
                    if(rotateWithLine) transform.up = line.Points[line.Points.Length - 2] - line.Points[line.Points.Length - 1];
                }
                else
                {
                    transform.localPosition = Vector2.Lerp(line.Points[nodeIndex], line.Points[nodeIndex + 1], nodeLerp);
                    if(rotateWithLine) transform.up = line.Points[nodeIndex] - line.Points[nodeIndex + 1];
                }
            }
        }

        void OnValidate()
        {
            PositionObject();
        }
    }
}