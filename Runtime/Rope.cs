using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LineSimulation
{

    [System.Serializable]
    public class RopeAttachedObject
    {
        [SerializeField] public GameObject prefab;
        [SerializeField] public float offset;
        [SerializeField, Min(0)] public int position;
        [HideInInspector] public Transform obj;
        public bool rotates = false;

        [HideInInspector] public Vector3 lastPos;
    }

    [System.Serializable]
    public class RopeAttachedListObject
    {
        [SerializeField] public GameObject prefab;
        [SerializeField] public float offset;
        [SerializeField, Min(1)] public int numbOfSegmentsBetweenObjs;
        [SerializeField] public bool rotates;
        // [HideInInspector] public RopeAttachedObject[] objs;
        // [HideInInspector] public Vector3[] lastPoss;
    }

    [RequireComponent(typeof(LineRenderer))]
    public class Rope : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        [SerializeField] private Transform start;
        [SerializeField] private Transform end;
        // private float ropeSegLen = 0.25f;
        [SerializeField, Min(0f)] private float length = 1.5f;
        [SerializeField] private Vector2 gravity = new Vector2(0f, -10f);
        [SerializeField] private float windSpeed = 0.5f;
        [SerializeField] private float windForce = 1f;
        [SerializeField, Min(2)] private int segmentNumber = 20;
        [SerializeField, Min(1)] private int simulationComplexity = 5;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private List<RopeAttachedObject> attachedObjects;
        [SerializeField] private List<RopeAttachedListObject> attachedRegularObjects;
        [SerializeField] private GameObject ropeCollider;
        private Transform[] colliders;
        private float randomOffset, lastBreakTime;
        private bool needsUpdate = false;
        private Vector3 launchVelocity;
        private Vector3 storedLaunchVelocity;
        private Vector3[] ropePositions, storedRopePositions;
        private float[] ropeSegLen;
        private Vector2 accell;
        [Range(0f, 1f)] public float breakingPoint;
        public bool breakable = true;
        public bool forceToBreak = false;
        [HideInInspector] public bool broken = false;
        private bool hasEnd = false;
        private bool hasStart = true;
        private Vector2 horizontalDir;

        void Start()
        {
            this.lineRenderer = this.GetComponent<LineRenderer>();
            horizontalDir = Vector2.Perpendicular(gravity).normalized;
            // Vector3 ropeStartPoint = start.position;
            float lineWidth = this.lineWidth;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            accell = (Vector3)(-gravity);
            randomOffset = Random.Range(0f, 1.5f);
            if (broken)
            {
                lastBreakTime = Time.time;
                return;
            }
            lastBreakTime = 0f;
            hasStart = true;
            if (end != null)
            {
                // Vector3 ropeEndPoint = end.position;
                hasEnd = true;
            }
            else
            {
                hasEnd = false;
                ropePositions = new Vector3[this.segmentNumber + 1];
                storedRopePositions = new Vector3[this.segmentNumber + 1];
                ropeSegLen = new float[this.segmentNumber];
                for (int i = 0; i < segmentNumber + 1; i++)
                {
                    if (i < segmentNumber)
                    {
                        ropeSegLen[i] = length / segmentNumber;
                    }
                    Vector3 pos = start.position + i * (Vector3)gravity.normalized * length / segmentNumber;
                    ropePositions[i] = pos;
                    storedRopePositions[i] = pos;
                }
            }

            // accell = hasEnd ? accell : -accell;
            SetRope();
            DrawRope(false);
            ClearObjects();
            CreateObjects();

        }

        private void ClearObjects()
        {
            // for each child of this object delete it if its name isn't "start" or "end"
            foreach (Transform child in transform)
            {
                if (child.name != "start" && child.name != "end")
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void CreateObjects()
        {
            foreach (RopeAttachedObject obj in attachedObjects)
            {
                if (obj.prefab == null)
                {
                    continue;
                }
                ProcessObj(obj);
            }
            foreach (RopeAttachedListObject list in attachedRegularObjects)
            {
                if (list.prefab == null)
                {
                    continue;
                }
                int regularity = list.numbOfSegmentsBetweenObjs;
                // list.objs = new RopeAttachedObject[segmentNumber / regularity];
                // list.lastPoss = new Vector3[segmentNumber / regularity];
                for (int i = 0; i < segmentNumber - regularity / 2; i++)
                {
                    if ((i + regularity / 2) % regularity == 0)
                    {
                        RopeAttachedObject obj = new RopeAttachedObject();
                        obj.rotates = list.rotates;
                        obj.offset = list.offset;
                        obj.prefab = list.prefab;
                        obj.position = i + regularity / 2;
                        attachedObjects.Add(obj);
                        ProcessObj(obj);
                    }
                }
            }
            if (breakable)
            {
                if (ropeCollider == null)
                {
                    Debug.LogWarning("Rope collider prefab is null, can't break");
                    breakable = false;
                    return;
                }
                int len = hasEnd ? segmentNumber + 1 : segmentNumber;
                colliders = new Transform[len];
                for (int i = 0; i < len; i++)
                {
                    GameObject col = Instantiate(ropeCollider, transform);
                    col.transform.position = ropePositions[i];
                    colliders[i] = col.transform;
                    col.GetComponent<RopeCollider>().Set(i, this);
                }
            }
        }

        private void ProcessObj(RopeAttachedObject obj)
        {
            GameObject createdObj = Instantiate(obj.prefab, transform);
            Transform tr = createdObj.transform;
            obj.obj = tr;
            tr.parent = transform;
            tr.up = -gravity;
            // float t = length * obj.position;
            // obj.obj.transform.position = obj.offset * obj.obj.transform.up + start.position + launchVelocity * t + (Vector3)accell * t * t / 2.0f;
            Vector3 startingPos = obj.offset * tr.up + ropePositions[obj.position];
            tr.position = startingPos;
            obj.lastPos = ropePositions[obj.position] - (Vector3)(-gravity);
        }

        void Update()
        {
            if (needsUpdate)
            {
                DrawRope(true);
                needsUpdate = false;
            }
            if (forceToBreak)
            {
                forceToBreak = false;
                BreakRope((int)(segmentNumber * breakingPoint));
            }
        }

        private void FixedUpdate()
        {
            this.Simulate();
        }

        private void Simulate()
        {
            float x_pert = windForce * Mathf.PerlinNoise(Time.time * windSpeed + randomOffset, 0f);
            float y_pert = windForce * Mathf.PerlinNoise(0f, windSpeed * Time.time + randomOffset);
            if (hasEnd)
            {
                accell = (Vector3)(-gravity + new Vector2(x_pert, y_pert));
            }
            else
            {
                Vector2 offset = Vector2.Dot(horizontalDir, new Vector2(x_pert, y_pert)) * horizontalDir;
                accell = (Vector3)(-gravity + new Vector2(x_pert, y_pert));
            }
            this.SetRope();
        }

        private void SetRope()
        {
            if (hasEnd)
            {
                ropePositions = new Vector3[this.segmentNumber + 1];
                launchVelocity = ((end.position - start.position) / length) - (Vector3)accell * length / 2.0f;
                float t;
                for (int i = 0; i < segmentNumber + 1; i++)
                {
                    t = length * i / (segmentNumber);
                    ropePositions[i] = start.position + launchVelocity * t + (Vector3)accell * t * t / 2.0f;
                }
            }
            else
            {
                // launchVelocity = --gravity / 4.0f;
                SimulateFullRope();
            }
            needsUpdate = true;
        }
        private void SimulateFullRope()
        {
            for (int i = hasStart ? 1 : 0; i < segmentNumber + 1; i++)
            {
                Vector3 velocity = ropePositions[i] - storedRopePositions[i];
                storedRopePositions[i] = ropePositions[i];
                ropePositions[i] += velocity;
                float mult = hasStart ? 2.0f : 15.0f;
                ropePositions[i] += -((Vector3)accell / mult) * Time.fixedDeltaTime;
            }

            for (int i = 0; i < simulationComplexity; i++)
            {
                this.ApplyConstraint();
            }
        }

        private void ApplyConstraint()
        {
            if (hasStart)
            {
                ropePositions[0] = start.position;
            }

            for (int i = 0; i < this.segmentNumber; i++)
            {
                // RopeSegment firstSeg = this.ropeSegments[i];
                // RopeSegment secondSeg = this.ropeSegments[i + 1];

                float dist = (ropePositions[i] - ropePositions[i + 1]).magnitude;
                float error = Mathf.Abs(dist - this.ropeSegLen[i]);
                Vector2 changeDir = Vector2.zero;

                if (dist > ropeSegLen[i])
                {
                    changeDir = (ropePositions[i] - ropePositions[i + 1]).normalized;
                }
                else if (dist < ropeSegLen[i])
                {
                    changeDir = (ropePositions[i + 1] - ropePositions[i]).normalized;
                }

                Vector3 changeAmount = (Vector3)changeDir * error;
                if (i != 0)
                {
                    ropePositions[i] -= changeAmount * 0.5f;
                    // this.ropeSegments[i] = firstSeg;
                    ropePositions[i + 1] += changeAmount * 0.5f;
                    // this.ropeSegments[i + 1] = secondSeg;
                }
                else
                {
                    ropePositions[i + 1] += changeAmount;
                    // this.ropeSegments[i + 1] = secondSeg;
                }
            }
        }


        private void DrawRope(bool drawObjects)
        {

            lineRenderer.positionCount = ropePositions.Length;
            lineRenderer.SetPositions(ropePositions);
            if (drawObjects)
            {
                DrawObjects();
            }
        }
        private Vector3 UpdateObjectPos(Vector3 origin, Transform obj, bool rotates, Vector3 lastPos, float offset)
        {
            Vector3 posNow = origin + obj.up * offset;
            Vector3 newPos = posNow;
            if (!rotates)
            {
                obj.position = posNow;
            }
            else
            {
                obj.up = Vector3.Lerp((origin - lastPos).normalized, Vector3.up, 0.09f);
                // Debug.Log((origin - lastPos).normalized);
                newPos = origin + offset * obj.up;
                obj.position = newPos;
                // obj.lastPos = newPos;
            }
            return newPos;
        }
        private void DrawObjects()
        {
            foreach (RopeAttachedObject obj in attachedObjects)
            {
                if (obj.prefab == null || obj.obj == null)
                {
                    continue;
                }
                // float t = length * obj.position;
                // Vector3 origin = start.position + launchVelocity * t + (Vector3)accell * t * t / 2.0f;

                Vector3 origin = ropePositions[obj.position];

                obj.lastPos = UpdateObjectPos(origin, obj.obj, obj.rotates, obj.lastPos, obj.offset);
            }
            int offset = hasStart ? 0 : 1;
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].position = ropePositions[i + offset];
            }

        }

        public void BreakRope(int breakingPoint)
        {
            if (!hasStart || Time.time - lastBreakTime < 0.4f)
            {
                return;
            }
            lastBreakTime = Time.time;
            broken = true;
            bool hadAnEnd = hasEnd;
            hasEnd = false;
            int oldLength = segmentNumber;
            segmentNumber = breakingPoint;
            List<Transform> keepingChildren = new List<Transform>();
            List<Transform> givingChildren = new List<Transform>();
            List<RopeAttachedObject> givingAttachedObjects = new List<RopeAttachedObject>();
            List<RopeAttachedObject> keepingAttachedObjects = new List<RopeAttachedObject>();
            List<Transform> keepingColliders = new List<Transform>();
            List<Transform> givingColliders = new List<Transform>();

            for (int i = 0; i < attachedObjects.Count; i++)
            {
                RopeAttachedObject obj = attachedObjects[i];
                obj.obj.parent = null;
                if (obj.position <= segmentNumber)
                {
                    keepingChildren.Add(obj.obj);
                    keepingAttachedObjects.Add(obj);
                }
                else
                {
                    obj.position = oldLength - obj.position;
                    givingChildren.Add(obj.obj);
                    givingAttachedObjects.Add(obj);
                    attachedObjects.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < colliders.Length; i++)
            {
                if (hasStart && i < segmentNumber || !hasStart && i < segmentNumber - 1)
                {
                    colliders[i].parent = null;
                    keepingColliders.Add(colliders[i]);
                }
                else if (hasStart && i > segmentNumber || !hasStart && i > segmentNumber - 1)
                {
                    colliders[i].parent = null;
                    givingColliders.Add(colliders[i]);
                }
            }
            start.parent = null;
            if (hadAnEnd)
            {
                end.parent = null;
            }
            colliders = keepingColliders.ToArray();
            attachedObjects = keepingAttachedObjects;

            foreach (Transform child in transform)
            {
                child.parent = null;
                Destroy(child.gameObject);
            }

            if (segmentNumber < oldLength)
            {
                GameObject newRope = Instantiate(gameObject, transform.parent);
                newRope.name = "rope_fragment";
                newRope.GetComponent<Rope>().IsFragment(ropePositions, givingAttachedObjects, givingColliders, oldLength - segmentNumber, hadAnEnd);

                foreach (Transform child in givingChildren)
                {
                    child.parent = newRope.transform;
                }
                foreach (Transform child in givingColliders)
                {
                    child.parent = newRope.transform;
                }
                if (hadAnEnd)
                {
                    end.gameObject.name = "start";
                    end.parent = newRope.transform;
                }
            }
            foreach (Transform child in keepingChildren)
            {
                child.parent = transform;
            }
            foreach (Transform child in keepingColliders)
            {
                child.parent = transform;
            }

            if (segmentNumber == 0)
            {
                Destroy(gameObject);
            }
            start.parent = transform;

            end = null;

            storedRopePositions = new Vector3[segmentNumber + 1];
            for (int i = 0; i < segmentNumber + 1; i++)
            {
                storedRopePositions[i] = ropePositions[i];
            }
            ropePositions = new Vector3[segmentNumber + 1];
            ropeSegLen = new float[segmentNumber];
            for (int i = 0; i < segmentNumber + 1; i++)
            {
                ropePositions[i] = storedRopePositions[i];
            }
            for (int i = 0; i < segmentNumber; i++)
            {
                ropeSegLen[i] = (ropePositions[i] - ropePositions[i + 1]).magnitude;
            }

        }

        public void IsFragment(Vector3[] ropePoss, List<RopeAttachedObject> givingAttachedObjects, List<Transform> givingColliders, int newLength, bool hadAnEnd)
        {
            hasEnd = false;
            if (hadAnEnd)
            {
                start = end;
                hasStart = true;
            }
            else
            {
                start = null;
                hasStart = false;
                StartCoroutine(DeleteFragment());
            }
            colliders = givingColliders.ToArray();
            for (int i = 0; i < colliders.Length / 2; i++)
            {
                Transform temp = colliders[i];
                colliders[i] = colliders[colliders.Length - i - 1];
                colliders[colliders.Length - i - 1] = temp;
            }

            attachedObjects = givingAttachedObjects;

            int offset = hasStart ? 0 : 1;
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].gameObject.GetComponent<RopeCollider>().Set(i + offset, this);
            }
            end = null;
            int oldLength = segmentNumber + newLength;
            segmentNumber = newLength;

            storedRopePositions = new Vector3[segmentNumber + 1];
            ropePositions = new Vector3[segmentNumber + 1];
            ropeSegLen = new float[segmentNumber];
            for (int i = 0; i < segmentNumber + 1; i++)
            {
                storedRopePositions[i] = ropePoss[oldLength - i];
                ropePositions[i] = storedRopePositions[i];
            }
            for (int i = 0; i < segmentNumber; i++)
            {
                ropeSegLen[i] = (ropePositions[i] - ropePositions[i + 1]).magnitude;
            }
        }

        public IEnumerator DeleteFragment()
        {
            yield return new WaitForSeconds(5f);
            Destroy(gameObject);
        }

        private void OnValidate()
        {
            foreach (RopeAttachedObject obj in attachedObjects)
            {
                if (obj.position > segmentNumber)
                {
                    obj.position = segmentNumber;
                }
            }
            this.lineRenderer = this.GetComponent<LineRenderer>();
            float lineWidth = this.lineWidth;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            if (end != null)
            {
                hasEnd = true;
            }
            else
            {
                hasEnd = false;
                ropePositions = new Vector3[this.segmentNumber + 1];
                storedRopePositions = new Vector3[this.segmentNumber + 1];
                ropeSegLen = new float[this.segmentNumber];
                for (int i = 0; i < segmentNumber + 1; i++)
                {
                    if (i < segmentNumber)
                    {
                        ropeSegLen[i] = length / segmentNumber;
                    }
                    Vector3 pos = start.position + i * (Vector3)gravity.normalized * length / segmentNumber;
                    ropePositions[i] = pos;
                    storedRopePositions[i] = pos;
                }
            }

            if (start != null && gravity != Vector2.zero && segmentNumber > 1)
            {
                accell = (Vector3)(-gravity);
                SetRope();
                DrawRope(false);
            }

        }

    }
}