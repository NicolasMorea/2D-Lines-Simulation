using UnityEngine;

namespace LineSimulation
{
    [System.Serializable]
    public struct LineNode
    {
        public float x, y;
        public float vx, vy;
        public int dvx, dvy;
        public float targetDist;
        public int parent; // 0 std, fixed 1
    }

    [System.Serializable]
    public struct LineParams
    {
        [HideInInspector] public Vector2 worldPos;
        public float stiffness;
        public float velocityScale;
        public Vector2 windForce;
        [HideInInspector] public float lineZ;
        [HideInInspector] float dummy;
    }

    public enum DynamicLineEditType
    {
        Straight,
        Custom,
    }

    public struct ColliderData
    {
        public Vector2 position;
        public float radius;
        public int impulseX, impulseY;
    }
    
    [System.Serializable]
    public struct BoxColliderData
    {
        public Vector2 position;
        public float halfWidth, halfHeight;
        public int dummy1, dummy2, dummy3, dummy4;
    }
}