/// <summary>
///* compute the physics of all lines in the scene
///* for now it only computes physics but in the future it might also handle the rendering of basics lines in pixel perfect (doesn't work for now, quite complex)
///* It is an instance but reloaded at each scene, it is not on global manager
/// </summary>

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LineSimulation
{
    public class DynamicLineManager : MonoBehaviour
    {
        #region Variables
        public bool debug;

        [Header("Data")]
        public int nNodesPerLine = 16;
        public int nLines = 256;
        public int nColliders;
        public float gravityForce = 0.1f;
        public float simulationSpeed, strengthOfForces;
        public float windForce = 0.1f;

        [Header("Compute shader")]
        [SerializeField] public ComputeShader shader;
        [SerializeField] public RenderTexture gravityRenderTexture;
        [SerializeField] public RenderTexture velocityTexture;

        [Header("Buffers (for Debug)")]
        public LineNode[] lineNodesArray;
        public LineParams[] lineParamsArray;
        public Vector2[] linePivotsArray;
        public Vector2[] lineEndsPivotsArray;
        public float[] debugArray;
        public static List<DynamicLine> lines;

        private int kiCalc, kiVelShare, kiInteractionWithColliders, kiCalcApply, kiOneThreadAction;
        private int kiVisInternodeLines, kiClearTexture, kiPixelsToTexture, kiClearPixels;
        private ComputeBuffer lineNodesBuffer;
        private ComputeBuffer lineParamsBuffer;
        private ComputeBuffer linePivotsBuffer;
        private ComputeBuffer lineEndsPivotsBuffer;
        private ComputeBuffer visBuffer;
	    private ComputeBuffer debugBuffer;
        [Header("Rendering (WIP)")]
        public RenderTexture renderTexture;

        #endregion
        #region Init
        private void Start()
        {
            if(shader == null) 
            {
                Debug.LogError("Shader not assigned in DynamicLineManager");
                return;
            }

            if(lines == null || lines.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            InitTexture();
            InitData();
            InitBuffers();
            InitShader();
        }

        void Reset()
        {
            lines = new List<DynamicLine>();
        }

        void InitTexture()
        {
            renderTexture = new RenderTexture(1024, 1024, 32);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        
        void InitData()
        {
            nLines = lines.Count;

            lineNodesArray = new LineNode[nLines * nNodesPerLine];
            lineParamsArray = new LineParams[nLines];
            linePivotsArray = new Vector2[nLines];
            lineEndsPivotsArray = new Vector2[nLines];

            int nNodes = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i]?.SetData(i, ref nNodes, this);
            }

            if(debug) Debug.Log( "Number of Lines : " + lines.Count + " / " + nLines);
            if(debug) Debug.Log( "Total of " + nNodes + " nodes");

            debugArray = new float[nLines * nNodesPerLine];
            debugArray[0] = -1;

        }

        void InitBuffers()
        {
            lineNodesBuffer = new ComputeBuffer(lineNodesArray.Length, 4 * 8);
            lineNodesBuffer.SetData(lineNodesArray);

            lineParamsBuffer = new ComputeBuffer(lineParamsArray.Length, 4 * 8);
            lineParamsBuffer.SetData(lineParamsArray);

            linePivotsBuffer = new ComputeBuffer(linePivotsArray.Length, 8);
            linePivotsBuffer.SetData(linePivotsArray);

            lineEndsPivotsBuffer = new ComputeBuffer(lineEndsPivotsArray.Length, 8);
            lineEndsPivotsBuffer.SetData(lineEndsPivotsArray);

		    debugBuffer = new ComputeBuffer(debugArray.Length, 4);
            debugBuffer.SetData(debugArray);

            visBuffer = new ComputeBuffer(1024 * 1024, 4);
        }

        void InitShader()
        {
            shader.SetInt("nNodesPerLine", nNodesPerLine);
            shader.SetInt("nLines", nLines);
            shader.SetInt("nBoxColliders", nColliders);
            shader.SetFloat("dPosRate", simulationSpeed);
            shader.SetFloat("dVelRate", strengthOfForces);
            shader.SetFloat("gravityForce", gravityForce);
            shader.SetFloat("windForce", windForce);
            shader.SetInt("F_TO_I", 2 << 17);
            shader.SetFloat("I_TO_F", 1f / (2 << 17));

            Vector2 screenRes = new Vector2(gravityRenderTexture.width, gravityRenderTexture.height);
            shader.SetVector("gravityScreenResolution", screenRes);

            kiCalc = shader.FindKernel("calc");
            shader.SetBuffer(kiCalc, "lineNodesBuffer", lineNodesBuffer);
            shader.SetBuffer(kiCalc, "lineParamsBuffer", lineParamsBuffer);
            shader.SetBuffer(kiCalc, "debugBuffer", debugBuffer);

            kiVelShare = shader.FindKernel("velShare");
            shader.SetBuffer(kiVelShare, "lineNodesBuffer", lineNodesBuffer);
            shader.SetBuffer(kiVelShare, "lineParamsBuffer", lineParamsBuffer);
            shader.SetBuffer(kiVelShare, "debugBuffer", debugBuffer);

            kiCalcApply = shader.FindKernel("calcApply");
            shader.SetBuffer(kiCalcApply, "lineNodesBuffer", lineNodesBuffer);
            shader.SetBuffer(kiCalcApply, "lineParamsBuffer", lineParamsBuffer);
            shader.SetBuffer(kiCalcApply, "linePivotsBuffer", linePivotsBuffer);
            shader.SetBuffer(kiCalcApply, "lineEndsPivotsBuffer", lineEndsPivotsBuffer);
            shader.SetTexture(kiCalcApply, "GravityRT", gravityRenderTexture);
            shader.SetTexture(kiCalcApply, "VelocityRT", velocityTexture);
            shader.SetBuffer(kiCalcApply, "debugBuffer", debugBuffer);

            kiVisInternodeLines = shader.FindKernel("visInternodeLines");
            shader.SetBuffer(kiVisInternodeLines, "lineNodesBuffer", lineNodesBuffer);
            shader.SetBuffer(kiVisInternodeLines, "visBuffer", visBuffer);
            shader.SetBuffer(kiVisInternodeLines, "debugBuffer", debugBuffer);

            kiPixelsToTexture = shader.FindKernel("pixelsToTexture");
            shader.SetTexture(kiPixelsToTexture, "renderTexture", renderTexture);
            shader.SetBuffer(kiPixelsToTexture, "visBuffer", visBuffer);

            kiClearPixels = shader.FindKernel("clearPixels");
            shader.SetBuffer(kiClearPixels, "visBuffer", visBuffer);

            kiClearTexture = shader.FindKernel("clearTexture");
            shader.SetTexture(kiClearTexture, "renderTexture", renderTexture);

            kiOneThreadAction = shader.FindKernel("oneThreadAction");
            shader.SetBuffer(kiOneThreadAction, "lineNodesBuffer", lineNodesBuffer);
            shader.SetBuffer(kiOneThreadAction, "linePivotsBuffer", linePivotsBuffer);
            shader.SetBuffer(kiOneThreadAction, "debugBuffer", debugBuffer);
        }

        #endregion

        #region Computations
        private void FixedUpdate()
        {
            RetreivePosForDebug();

            SendMainPositions();
            doShaderStuff();
        }

        void RetreivePosForDebug()
        {
            debugBuffer.GetData(debugArray);

            lineNodesBuffer.GetData(lineNodesArray);

            // send data to each line

            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].Points ??= new Vector2[nNodesPerLine];
                for (int j = 0; j < nNodesPerLine; j++)
                {
                    lines[i].Points[j] = new Vector2(lineNodesArray[i * nNodesPerLine + j].x, lineNodesArray[i * nNodesPerLine + j].y) - (Vector2)lines[i].transform.position;
                }
            }
        }

        private void SendMainPositions()
        {
            shader.SetFloat("dPosRate", simulationSpeed);
            shader.SetFloat("dVelRate", strengthOfForces);
            shader.SetFloat("gravityForce", gravityForce);
            shader.SetFloat("windForce", windForce);



            bool update = false; //* so that we don't update every frame if we only have fixed lines
            for (int i = 0; i <  lines.Count; i++)
            {
                if(i > linePivotsArray.Length) 
                {
                    Debug.LogError(" Trying to access pos of line index out of bound");
                    return;
                }
                if((Vector2)lines[i].transform.position != linePivotsArray[i])
                {
                    update = true;
                    linePivotsArray[i] = lines[i].transform.position;
                }
            }
            if(update) 
            {
                linePivotsBuffer.SetData(linePivotsArray);
                shader.SetBuffer(kiCalcApply, "linePivotsBuffer", linePivotsBuffer);
            }
            update = false;
            for (int i = 0; i <  lines.Count; i++)
            {
                if(i > lineEndsPivotsArray.Length) 
                {
                    Debug.LogError(" Trying to access pos of line index out of bound");
                    return;
                }
                if(lines[i].hasEnd && lines[i].GetEnd() != lineEndsPivotsArray[i])
                {
                    update = true;
                    lineEndsPivotsArray[i] = lines[i].GetEnd();
                }
            }
            if(update) 
            {
                lineEndsPivotsBuffer.SetData(lineEndsPivotsArray);
                shader.SetBuffer(kiCalcApply, "lineEndsPivotsBuffer", lineEndsPivotsBuffer);
            }

            Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
            Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;

            shader.SetMatrix("viewProjMatrix", viewProjMatrix);
        }

        private void doShaderStuff()
        {
            int nLineThreadGroups = (nLines - 1) / 16 + 1;
            int nNodesThreadGroups = (nNodesPerLine - 1) / 8 + 1;
            // Dispatch kernels
            for (int i = 0; i < 40; i++)
            {
                shader.Dispatch(kiVelShare, nLineThreadGroups, nNodesThreadGroups, 1);
                shader.Dispatch(kiCalc, nLineThreadGroups, nNodesThreadGroups, 1);
                shader.Dispatch(kiCalcApply, nLineThreadGroups, nNodesThreadGroups, 1);
                shader.Dispatch(kiOneThreadAction, nLines, 1, 1);
            }
            
            shader.Dispatch(kiVisInternodeLines, nLineThreadGroups, nNodesThreadGroups, 1);
            shader.Dispatch(kiClearTexture, 32, 32, 1);
            shader.Dispatch(kiPixelsToTexture, 32, 32, 1);
            shader.Dispatch(kiClearPixels, 32, 32, 1);
        }

        #endregion

        #region Line Cutting

        public void SimpleCut(int LineID, int localNodeID)
        {
            LineNode[] line = new LineNode[nNodesPerLine];

            // Retrieve the relevant part of the buffer
            lineNodesBuffer.GetData(line, 0, LineID * nNodesPerLine, nNodesPerLine);

            // Modify the specific node in the array
            line[localNodeID].parent = -2;

            // Set the modified part of the buffer back to the GPU
            lineNodesBuffer.SetData(line, 0, LineID * nNodesPerLine, nNodesPerLine);
        }

        public void SliceLine(int LineID, int NodeID)
        {
            LineNode[] OriginalLine = new LineNode[nNodesPerLine];

            lineNodesBuffer.GetData(OriginalLine, 0, LineID * nNodesPerLine, nNodesPerLine);
            if(LineID > lines.Count) 
            {
                Debug.LogError(" Trying to slice line out of bounds");
                return;
            }

            LineNode[] FirstPart;
            LineNode[] SecondPart;
            (FirstPart, SecondPart) = GetSlicedLineNodes(OriginalLine, NodeID - LineID * nNodesPerLine);

            Debug.Log("First part : " + FirstPart.Length);
            Debug.Log("First element part : " + FirstPart[0].x + " " + FirstPart[0].y + " " + FirstPart[0].vx + " " + FirstPart[0].vy + " " + FirstPart[0].targetDist);
            Debug.Log("Second part : " + SecondPart.Length);
            Debug.Log("Second element part : " + SecondPart[0].x + " " + SecondPart[0].y + " " + SecondPart[0].vx + " " + SecondPart[0].vy + " " + SecondPart[0].targetDist);

            lineNodesArray = new LineNode[(nLines+1) * nNodesPerLine];
            lineParamsArray = new LineParams[nLines+1];
            linePivotsArray = new Vector2[nLines+1];
            lineNodesBuffer.GetData(lineNodesArray, 0, 0, nLines * nNodesPerLine);
            lineParamsBuffer.GetData(lineParamsArray, 0, 0, nLines);
            linePivotsBuffer.GetData(linePivotsArray, 0, 0, nLines);

            lineParamsArray[nLines] = lineParamsArray[LineID];
            linePivotsArray[nLines] = new Vector2(0, 0);
            
            // set the first part of the line to the index of the cut line
            for (int i = 0; i < nNodesPerLine; i++)
            {
                lineNodesArray[LineID * nNodesPerLine + i] = FirstPart[i];
            }

            // set the second part of the line to the end of the array
            for (int i = 0; i < nNodesPerLine; i++)
            {
                lineNodesArray[(nLines - 1) * nNodesPerLine + i] = SecondPart[i];
            }

            nLines += 1;

            InitBuffers();
            InitShader();
        }


        public (LineNode[], LineNode[]) GetSlicedLineNodes(LineNode[] originalLine, int CutNode)
        {
            float OldLength = 0.0f;
            float NewLength = 0.0f;

            for (int i = 0 ; i < originalLine.Length -1; i++)
            {
                OldLength += originalLine[i].targetDist;

                if(i <= CutNode)
                {
                    NewLength += originalLine[i].targetDist;
                }
            }

            Debug.Log("Old Length : " + OldLength + " New Length : " + NewLength);
            
            LineNode[] Part1 = new LineNode[nNodesPerLine];
            LineNode[] Part2 = new LineNode[nNodesPerLine];


            for (int i = 0 ; i < nNodesPerLine ; i++)
            {
                Part1[i] = GetNodeAtLength((NewLength / nNodesPerLine) * i, originalLine);
                Part2[i] = GetNodeAtLength(NewLength + ((OldLength - NewLength) / nNodesPerLine) * i, originalLine);
            }

            for (int i = 0 ; i < nNodesPerLine -1; i++)
            {
                Part1[i].targetDist = Vector2.Distance(Pos(Part1[i + 1]), Pos(Part1[i]));
                Debug.Log("Part1 at " + i + " is " + Pos(Part1[i] ) + " with target dist " + Part1[i].targetDist);
            }
            for (int i = 0 ; i < nNodesPerLine -1; i++)
            {
                Part2[i].targetDist = Vector2.Distance(Pos(Part2[i + 1]), Pos(Part2[i])); 
                Debug.Log("Part2 at " + i + " is " + Pos(Part2[i]) + " with target dist " + Part2[i].targetDist);
            }

            return (Part1, Part2);
        }


        public static LineNode GetNodeAtLength(float Length, LineNode[] Points)
        {
            float remaining = Length;
            LineNode node = new LineNode();

            for (int i = 0 ; i < Points.Length - 1; i++)
            {
                float segment = Vector2.Distance(Pos(Points[i + 1]), Pos(Points[i]));
                if(remaining <= segment)
                {
                    return GetNodeInBetween(Points[i], Points[i + 1], remaining / segment);
                }
                remaining -= segment;
            }

            Debug.Log("trying to seek point beyond line end");
            return node;
        }

        public static Vector2 GetLocalPosAtLength(float Length, Vector2[] Points)
        {
            int index = GetNodeIndexAtLength(Length, Points);

            if(index >= Points.Length - 1) return Points[Points.Length - 1];

            else if(index < 0) return Points[0];
            else return GetLocalPosInBetween(Points[index], Points[index + 1], (Length - LengthAtIndex(index, Points)) / Vector2.Distance(Points[index], Points[index + 1]));
        }

        public static float LengthAtIndex(int index, Vector2[] Points)
        {
            int to = Mathf.Clamp(index, 0, Points.Length - 1);
            float length = 0.0f;
            for (int i = 0 ; i < to; i++) length += Vector2.Distance(Points[i + 1], Points[i]);
            return length;
        }

        public static float GetLength(Vector2[] Points)
        {
            return LengthAtIndex(Points.Length - 1, Points);
        }

        public static int GetNodeIndexAtLength(float Length, Vector2[] points)
        {
            float remaining = Length;

            for (int i = 0 ; i < points.Length - 1; i++)
            {
                float segment = Vector2.Distance(points[i + 1], points[i]);
                if(remaining <= segment)
                {
                    return i;
                }
                remaining -= segment;
            }

            return points.Length - 1;
        }

        public static Vector2 GetDirectionAtLength(float Length, Vector2[] Points)
        {
            float remaining = Length;
            for (int i = 0 ; i < Points.Length - 1; i++)
            {
                float segment = Vector2.Distance(Points[i + 1], Points[i]);
                if(remaining <= segment)
                {
                    return Points[i + 1] - Points[i];
                }
                remaining -= segment;
            }

            return Points[Points.Length - 1] - Points[Points.Length - 2];
        }

        public static Vector2 GetLocalPosInBetween(Vector2 first, Vector2 second, float lerpAmount) => first * (1 - lerpAmount) + lerpAmount * second;

        private static LineNode GetNodeInBetween(LineNode first, LineNode second, float lerpAmount)
        {
            LineNode node = new LineNode();

            if(lerpAmount < 0 || lerpAmount > 1)
            {
                Debug.LogError("Lerp amount out of bounds");
                lerpAmount = Mathf.Clamp(lerpAmount, 0, 1);
            }

            Vector2 Position = Pos(first) * (1 - lerpAmount) + lerpAmount * Pos(second);
            node.x = Position.x;
            node.y = Position.y;

            Vector2 _Speed = Speed(first) * (1 - lerpAmount) + lerpAmount * Speed(second);
            node.vx = _Speed.x;
            node.vy = _Speed.y;
            Debug.Log("speed 1 : " + Speed(first) + " speed 2 : " + Speed(second) + " speed node : " + _Speed);

            return node;
        }
        #endregion

        #region Utility
        private static Vector2 Pos(LineNode Node)
        {
            return new Vector2(Node.x, Node.y);
        }

        private static Vector2 Speed(LineNode Node)
        {
            return new Vector2(Node.vx, Node.vy);
        }

        private Vector2[] PointsOf(LineNode[] Line)
        {
            Vector2[] Points = new Vector2[nNodesPerLine];

            if(Line.Length > nNodesPerLine)
            {
                Debug.LogError(" Getting points of a line without correct size");
            }
            else
            {
                for (int i = 0 ; i < nNodesPerLine ; i++)
                {
                    Points[i] = Pos(Line[i]);
                }
            }
            return Points;
        }

        private void OnDestroy()
        {
            lineNodesBuffer?.Release();
            lineParamsBuffer?.Release();
            linePivotsBuffer?.Release();
            visBuffer?.Release();
            debugBuffer?.Release();
        }

        #endregion
        
        #if UNITY_EDITOR

        [MenuItem("Tools/Validate Lines")]
        public static void ValidateLines()
        {
            DynamicLine[] lines = UnityEngine.Object.FindObjectsByType<DynamicLine>(FindObjectsSortMode.None);
            foreach (var line in lines)
            {
                if (line != null) line.ComputePoints();
                if(line != null) line.UpdateVisuals();
            }

            RegularLineObject[] objects = UnityEngine.Object.FindObjectsByType<RegularLineObject>(FindObjectsSortMode.None);

            foreach (var obj in objects)
            {
                if (obj != null) obj.ReloadObjects();
            }

            LineObject[] lineObj = UnityEngine.Object.FindObjectsByType<LineObject>(FindObjectsSortMode.None);
            foreach (var obj in lineObj)
            {
                if (obj != null) obj.PositionObject();
            }
        }
        #endif
    }

}