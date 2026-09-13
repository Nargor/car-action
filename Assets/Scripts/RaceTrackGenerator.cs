using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RaceTrack
{
    [ExecuteInEditMode]
    public class RaceTrackGenerator : MonoBehaviour
    {
        [Header("Blueprint & Track Scale")]
        [Tooltip("Path to blueprint outline image. Default: Assets/RaceTrack/track_blueprint.png")]
        public string blueprintPath = "Assets/RaceTrack/track_blueprint.png";

        [Tooltip("Target circumference in meters (12,000m = 12km, exactly 6 min lap at 120 km/h)")]
        public float targetCircumference = 12000f;

        [Tooltip("Width of drivable road in meters (4-6 lanes wide)")]
        public float trackWidth = 35f;

        [Tooltip("Number of segments around the track circuit")]
        [Range(360, 1440)]
        public int segments = 800;

        [Tooltip("Maximum banking angle on sharp corners in degrees")]
        [Range(0f, 10f)]
        public float maxBankAngle = 5.0f;

        [Header("Track Features")]
        public float curbWidth = 1.8f;
        public float curbHeight = 0.15f;
        public float barrierHeight = 1.8f;
        public float barrierThickness = 0.4f;

        [Header("Materials")]
        public Material roadMaterial;
        public Material curbMaterial;
        public Material barrierMaterial;
        public Material groundMaterial;
        public Material archMaterial;
        public Material whiteMarkingMaterial;

        [HideInInspector]
        public Vector3[] pathPoints;
        [HideInInspector]
        public Vector3[] pathTangents;
        [HideInInspector]
        public Vector3[] pathNormals;
        [HideInInspector]
        public float[] pathBankAngles;

        [ContextMenu("Build Race Track")]
        public void BuildTrack()
        {
            EnsureMaterials();
            ClearExistingTrack();

            if (!GenerateCenterlinePath())
            {
                Debug.LogError("[RaceTrackGenerator] Failed to generate centerline path from blueprint.");
                return;
            }

            GameObject trackRoot = this.gameObject;

            // 1. Road Surface (with 100% continuous solid edge lines, double yellow center lines, and 4-lane divider dashes)
            BuildRoad(trackRoot.transform);

            // 2. Curbs (Inner & Outer)
            BuildCurbs(trackRoot.transform);

            // 3. Safety Guardrails (Inner & Outer)
            BuildBarriers(trackRoot.transform);

            // 4. Start / Finish Arch & Full-Width 35m Checkered Strip
            BuildStartFinish(trackRoot.transform);

            // 5. Starting Grid Boxes (16 Staggered Race Starting Slots)
            BuildStartingGrid(trackRoot.transform);

            // 6. Corner Braking Distance Lines (150m, 100m, 50m)
            BuildBrakingLines(trackRoot.transform);

            // 7. Milestone Markers (every 1000m)
            BuildMilestones(trackRoot.transform);

            // 8. Ground Environment
            BuildGround(trackRoot.transform);

            Debug.Log($"<color=#00FF00><b>[RaceTrackGenerator]</b> Complete track & road markings built successfully! Circumference: {targetCircumference:F0}m (~{targetCircumference / 1000f:F1} km) | Width: {trackWidth}m</color>");
        }

        [ContextMenu("Clear Track")]
        public void ClearExistingTrack()
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        public bool GenerateCenterlinePath()
        {
            string fullPath = Application.dataPath + "/" + blueprintPath.Replace("Assets/", "");
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"Blueprint image not found at: {fullPath}");
                return false;
            }

            byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes)) return false;

            int w = tex.width;
            int h = tex.height;

            bool[,] isBlack = new bool[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    isBlack[x, y] = (tex.GetPixel(x, y).r < 0.5f);

            bool[,] isExterior = new bool[w, h];
            Queue<Vector2Int> q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(0, 0));
            isExterior[0, 0] = true;

            int[] dx4 = { 1, -1, 0, 0 };
            int[] dy4 = { 0, 0, 1, -1 };

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nx = p.x + dx4[i];
                    int ny = p.y + dy4[i];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h && !isExterior[nx, ny] && !isBlack[nx, ny])
                    {
                        isExterior[nx, ny] = true;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }

            bool[,] isBorder = new bool[w, h];
            List<Vector2Int> borderList = new List<Vector2Int>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (isBlack[x, y])
                    {
                        bool adj = false;
                        for (int i = 0; i < 4 && !adj; i++)
                        {
                            int nx = x + dx4[i];
                            int ny = y + dy4[i];
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h && isExterior[nx, ny]) adj = true;
                        }
                        if (adj)
                        {
                            isBorder[x, y] = true;
                            borderList.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            if (borderList.Count < 50) return false;

            Vector2Int start = borderList[0];
            foreach (var p in borderList) if (p.y > start.y) start = p;

            List<Vector2> raw = new List<Vector2>();
            bool[,] visited = new bool[w, h];
            Vector2Int current = start;
            raw.Add(new Vector2(current.x, current.y));
            visited[current.x, current.y] = true;

            for (int step = 0; step < 10000; step++)
            {
                bool found = false;
                Vector2Int bestNext = current;
                float bestDist = float.MaxValue;

                for (int r = 1; r <= 8 && !found; r++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        for (int dx = -r; dx <= r; dx++)
                        {
                            if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                            int nx = current.x + dx;
                            int ny = current.y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h && isBorder[nx, ny] && !visited[nx, ny])
                            {
                                float d = dx * dx + dy * dy;
                                if (d < bestDist)
                                {
                                    bestDist = d;
                                    bestNext = new Vector2Int(nx, ny);
                                    found = true;
                                }
                            }
                        }
                    }
                }

                if (!found) break;
                current = bestNext;
                visited[current.x, current.y] = true;
                raw.Add(new Vector2(current.x, current.y));
            }

            int N = raw.Count;
            if (N < 50) return false;

            float[] cumDist = new float[N + 1];
            cumDist[0] = 0f;
            for (int i = 0; i < N; i++)
            {
                cumDist[i + 1] = cumDist[i] + Vector2.Distance(raw[i], raw[(i + 1) % N]);
            }
            float totalLen = cumDist[N];

            Vector2[] resampled = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float targetDist = (float)i / segments * totalLen;
                int low = 0, high = N;
                while (low < high - 1)
                {
                    int mid = (low + high) / 2;
                    if (cumDist[mid] <= targetDist) low = mid;
                    else high = mid;
                }
                float segT = (targetDist - cumDist[low]) / Mathf.Max(0.0001f, cumDist[low + 1] - cumDist[low]);
                resampled[i] = Vector2.Lerp(raw[low], raw[(low + 1) % N], segT);
            }

            Vector2[] smoothed = new Vector2[segments];
            int kernel = 13;
            float[] weights = new float[kernel * 2 + 1];
            float weightSum = 0f;
            for (int k = -kernel; k <= kernel; k++)
            {
                weights[k + kernel] = Mathf.Exp(-0.5f * (k * k) / 22f);
                weightSum += weights[k + kernel];
            }
            for (int i = 0; i < segments; i++)
            {
                Vector2 acc = Vector2.zero;
                for (int k = -kernel; k <= kernel; k++)
                {
                    int idx = (i + k + segments) % segments;
                    acc += resampled[idx] * weights[k + kernel];
                }
                smoothed[i] = acc / weightSum;
            }

            Vector2 center2D = Vector2.zero;
            for (int i = 0; i < segments; i++) center2D += smoothed[i];
            center2D /= segments;

            float smoothedPerimeter = 0f;
            for (int i = 0; i < segments; i++)
            {
                smoothedPerimeter += Vector2.Distance(smoothed[i], smoothed[(i + 1) % segments]);
            }
            float scale = targetCircumference / smoothedPerimeter;

            float lowestY = float.MaxValue;
            int startIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                if (smoothed[i].y < lowestY)
                {
                    lowestY = smoothed[i].y;
                    startIdx = i;
                }
            }

            pathPoints = new Vector3[segments + 1];
            pathTangents = new Vector3[segments + 1];
            pathNormals = new Vector3[segments + 1];
            pathBankAngles = new float[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                int srcIdx = (startIdx + i) % segments;
                Vector2 pt = (smoothed[srcIdx] - center2D) * scale;
                pathPoints[i] = new Vector3(pt.x, 0f, pt.y);
            }

            for (int i = 0; i < segments; i++)
            {
                int prev = (i - 1 + segments) % segments;
                int next = (i + 1) % segments;

                Vector3 fwd = (pathPoints[next] - pathPoints[prev]).normalized;
                pathTangents[i] = fwd;

                Vector3 right = new Vector3(fwd.z, 0f, -fwd.x).normalized;
                pathNormals[i] = right;

                Vector3 prevFwd = (pathPoints[i] - pathPoints[prev]).normalized;
                Vector3 nextFwd = (pathPoints[next] - pathPoints[i]).normalized;
                float turnAngle = Vector2.SignedAngle(new Vector2(prevFwd.x, prevFwd.z), new Vector2(nextFwd.x, nextFwd.z));

                float bank = Mathf.Clamp(turnAngle * 3.0f, -maxBankAngle, maxBankAngle);
                pathBankAngles[i] = bank;
            }

            float[] smoothedBank = new float[segments];
            for (int i = 0; i < segments; i++)
            {
                float sum = 0f;
                int count = 0;
                for (int k = -8; k <= 8; k++)
                {
                    sum += pathBankAngles[(i + k + segments) % segments];
                    count++;
                }
                smoothedBank[i] = sum / count;
            }
            for (int i = 0; i < segments; i++) pathBankAngles[i] = smoothedBank[i];

            pathPoints[segments] = pathPoints[0];
            pathTangents[segments] = pathTangents[0];
            pathNormals[segments] = pathNormals[0];
            pathBankAngles[segments] = pathBankAngles[0];

            return true;
        }

        private void EnsureMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (roadMaterial == null)
            {
                roadMaterial = new Material(litShader);
                roadMaterial.name = "Mat_Road_Asphalt";
            }
            roadMaterial.color = Color.white;
            roadMaterial.SetColor("_BaseColor", Color.white);
            roadMaterial.SetFloat("_Smoothness", 0.35f);
            roadMaterial.mainTexture = GenerateRoadTexture();
            roadMaterial.mainTextureScale = Vector2.one;

            if (curbMaterial == null)
            {
                curbMaterial = new Material(litShader);
                curbMaterial.name = "Mat_Curbs";
            }
            curbMaterial.color = Color.white;
            curbMaterial.SetColor("_BaseColor", Color.white);
            curbMaterial.mainTexture = GenerateCurbTexture();
            curbMaterial.mainTextureScale = Vector2.one;
            curbMaterial.SetFloat("_Smoothness", 0.4f);

            if (barrierMaterial == null)
            {
                barrierMaterial = new Material(litShader);
                barrierMaterial.name = "Mat_Barriers";
            }
            barrierMaterial.color = new Color(0.88f, 0.90f, 0.92f);
            barrierMaterial.SetFloat("_Metallic", 0.65f);
            barrierMaterial.SetFloat("_Smoothness", 0.6f);

            if (groundMaterial == null)
            {
                groundMaterial = new Material(litShader);
                groundMaterial.name = "Mat_Track_Ground";
            }
            groundMaterial.color = new Color(0.20f, 0.36f, 0.16f);
            groundMaterial.SetFloat("_Smoothness", 0.1f);

            if (archMaterial == null)
            {
                archMaterial = new Material(litShader);
                archMaterial.name = "Mat_StartFinishArch";
            }
            archMaterial.color = new Color(0.98f, 0.76f, 0.05f);
            archMaterial.SetFloat("_Smoothness", 0.5f);

            if (whiteMarkingMaterial == null)
            {
                whiteMarkingMaterial = new Material(litShader);
                whiteMarkingMaterial.name = "Mat_Road_Markings";
                whiteMarkingMaterial.color = new Color(0.98f, 0.98f, 0.98f);
                whiteMarkingMaterial.SetColor("_BaseColor", new Color(0.98f, 0.98f, 0.98f));
                whiteMarkingMaterial.SetFloat("_Smoothness", 0.3f);
            }
        }

        private Texture2D GenerateRoadTexture()
        {
            int width = 1024;
            int height = 1024;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color asphaltBase = new Color(0.18f, 0.18f, 0.19f);
            Color asphaltDark = new Color(0.13f, 0.13f, 0.14f);
            Color whiteLine = new Color(0.98f, 0.98f, 0.98f);
            Color yellowLine = new Color(0.98f, 0.82f, 0.05f);
            Color rubberMark = new Color(0.10f, 0.10f, 0.11f);

            Color[] colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                // Dash cycle: 60% dash length, 40% gap
                bool isDash = v < 0.60f;

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    float noise = Mathf.PerlinNoise(u * 35f, v * 35f) * 0.035f;
                    Color c = Color.Lerp(asphaltBase, asphaltDark, noise);

                    // Tire rubber racing grooves in the tire tracks of each lane
                    float lane1Tire = Mathf.Abs(u - 0.15f);
                    float lane2Tire = Mathf.Abs(u - 0.38f);
                    float lane3Tire = Mathf.Abs(u - 0.62f);
                    float lane4Tire = Mathf.Abs(u - 0.85f);
                    if (lane1Tire < 0.04f || lane2Tire < 0.04f || lane3Tire < 0.04f || lane4Tire < 0.04f)
                    {
                        c = Color.Lerp(c, rubberMark, 0.25f);
                    }

                    // 1. INNER SOLID WHITE SHOULDER LINE (100% continuous, never broken)
                    if (u >= 0.022f && u <= 0.042f)
                    {
                        c = whiteLine;
                    }

                    // 2. LANE 1-2 DASHED DIVIDER LINE (White)
                    if (isDash && u >= 0.260f && u <= 0.276f)
                    {
                        c = whiteLine;
                    }

                    // 3. CENTER DOUBLE SOLID YELLOW LINES (100% continuous, never broken)
                    if ((u >= 0.485f && u <= 0.495f) || (u >= 0.505f && u <= 0.515f))
                    {
                        c = yellowLine;
                    }

                    // 4. LANE 3-4 DASHED DIVIDER LINE (White)
                    if (isDash && u >= 0.724f && u <= 0.740f)
                    {
                        c = whiteLine;
                    }

                    // 5. OUTER SOLID WHITE SHOULDER LINE (100% continuous, never broken)
                    if (u >= 0.958f && u <= 0.978f)
                    {
                        c = whiteLine;
                    }

                    colors[y * width + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 16;
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateCurbTexture()
        {
            int width = 64;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color red = new Color(0.88f, 0.12f, 0.12f);
            Color white = new Color(0.96f, 0.96f, 0.96f);

            Color[] colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                Color c = (y < height / 2) ? red : white;
                for (int x = 0; x < width; x++)
                {
                    colors[y * width + x] = c;
                }
            }
            tex.SetPixels(colors);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 16;
            tex.Apply();
            return tex;
        }

        private void BuildRoad(Transform parent)
        {
            GameObject roadObj = new GameObject("RoadSurface");
            roadObj.transform.SetParent(parent, false);

            MeshFilter mf = roadObj.AddComponent<MeshFilter>();
            MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
            MeshCollider mc = roadObj.AddComponent<MeshCollider>();

            mr.sharedMaterial = roadMaterial;

            Mesh mesh = new Mesh();
            mesh.name = "CustomTrack_Road_Mesh";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            float vTileScale = targetCircumference / 24f; // 24m per dash cycle (14m dash + 10m gap)
            float halfW = trackWidth * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 center = pathPoints[i];
                Vector3 normal = pathNormals[i];
                float bankRad = pathBankAngles[i] * Mathf.Deg2Rad;

                Vector3 surfaceRight = normal * Mathf.Cos(bankRad) + Vector3.up * Mathf.Sin(bankRad);
                Vector3 surfaceUp = Vector3.up * Mathf.Cos(bankRad) - normal * Mathf.Sin(bankRad);

                Vector3 innerPos = center - surfaceRight * halfW;
                Vector3 outerPos = center + surfaceRight * halfW;

                int vi = i * 2;
                vertices[vi + 0] = innerPos;
                vertices[vi + 1] = outerPos;

                normals[vi + 0] = surfaceUp;
                normals[vi + 1] = surfaceUp;

                uvs[vi + 0] = new Vector2(0f, t * vTileScale);
                uvs[vi + 1] = new Vector2(1f, t * vTileScale);

                if (i < segments)
                {
                    int ti = i * 6;
                    triangles[ti + 0] = vi + 0;
                    triangles[ti + 1] = vi + 2;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + 2;
                    triangles[ti + 5] = vi + 3;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
        }

        private void BuildCurbs(Transform parent)
        {
            BuildSingleCurb(parent, "InnerCurb", isInner: true);
            BuildSingleCurb(parent, "OuterCurb", isInner: false);
        }

        private void BuildSingleCurb(Transform parent, string name, bool isInner)
        {
            GameObject curbObj = new GameObject(name);
            curbObj.transform.SetParent(parent, false);

            MeshFilter mf = curbObj.AddComponent<MeshFilter>();
            MeshRenderer mr = curbObj.AddComponent<MeshRenderer>();
            MeshCollider mc = curbObj.AddComponent<MeshCollider>();

            mr.sharedMaterial = curbMaterial;

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            float vTileScale = targetCircumference / 3.5f;
            float halfW = trackWidth * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 center = pathPoints[i];
                Vector3 normal = pathNormals[i];
                float bankRad = pathBankAngles[i] * Mathf.Deg2Rad;

                Vector3 surfaceRight = normal * Mathf.Cos(bankRad) + Vector3.up * Mathf.Sin(bankRad);
                Vector3 surfaceUp = Vector3.up * Mathf.Cos(bankRad) - normal * Mathf.Sin(bankRad);

                Vector3 roadEdge, curbEdge;
                if (isInner)
                {
                    roadEdge = center - surfaceRight * halfW;
                    curbEdge = center - surfaceRight * (halfW + curbWidth) + surfaceUp * curbHeight;
                }
                else
                {
                    roadEdge = center + surfaceRight * halfW;
                    curbEdge = center + surfaceRight * (halfW + curbWidth) + surfaceUp * curbHeight;
                }

                int vi = i * 2;
                if (isInner)
                {
                    vertices[vi + 0] = curbEdge;
                    vertices[vi + 1] = roadEdge;
                }
                else
                {
                    vertices[vi + 0] = roadEdge;
                    vertices[vi + 1] = curbEdge;
                }

                normals[vi + 0] = surfaceUp;
                normals[vi + 1] = surfaceUp;

                uvs[vi + 0] = new Vector2(0f, t * vTileScale);
                uvs[vi + 1] = new Vector2(1f, t * vTileScale);

                if (i < segments)
                {
                    int ti = i * 6;
                    triangles[ti + 0] = vi + 0;
                    triangles[ti + 1] = vi + 2;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + 2;
                    triangles[ti + 5] = vi + 3;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
        }

        private void BuildBarriers(Transform parent)
        {
            BuildSingleBarrier(parent, "InnerBarrier", isInner: true);
            BuildSingleBarrier(parent, "OuterBarrier", isInner: false);
        }

        private void BuildSingleBarrier(Transform parent, string name, bool isInner)
        {
            GameObject barrierObj = new GameObject(name);
            barrierObj.transform.SetParent(parent, false);

            MeshFilter mf = barrierObj.AddComponent<MeshFilter>();
            MeshRenderer mr = barrierObj.AddComponent<MeshRenderer>();
            MeshCollider mc = barrierObj.AddComponent<MeshCollider>();

            mr.sharedMaterial = barrierMaterial;

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            int vertCount = (segments + 1) * 4;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 18];

            float halfW = trackWidth * 0.5f + curbWidth;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 center = pathPoints[i];
                Vector3 normal = pathNormals[i];
                float bankRad = pathBankAngles[i] * Mathf.Deg2Rad;

                Vector3 surfaceRight = normal * Mathf.Cos(bankRad) + Vector3.up * Mathf.Sin(bankRad);
                Vector3 surfaceUp = Vector3.up * Mathf.Cos(bankRad) - normal * Mathf.Sin(bankRad);

                float rOffset = isInner ? -halfW : halfW;
                Vector3 basePos = center + surfaceRight * rOffset;

                Vector3 wallNormal = isInner ? surfaceRight : -surfaceRight;
                Vector3 wallOut = -wallNormal;

                Vector3 p0 = basePos;
                Vector3 p1 = basePos + wallOut * barrierThickness;
                Vector3 p2 = p0 + surfaceUp * barrierHeight;
                Vector3 p3 = p1 + surfaceUp * barrierHeight;

                int vi = i * 4;
                vertices[vi + 0] = p0;
                vertices[vi + 1] = p2;
                vertices[vi + 2] = p3;
                vertices[vi + 3] = p1;

                normals[vi + 0] = wallNormal;
                normals[vi + 1] = wallNormal;
                normals[vi + 2] = surfaceUp;
                normals[vi + 3] = wallOut;

                float uCoord = t * (targetCircumference / 6f);
                uvs[vi + 0] = new Vector2(uCoord, 0f);
                uvs[vi + 1] = new Vector2(uCoord, 1f);
                uvs[vi + 2] = new Vector2(uCoord, 1f);
                uvs[vi + 3] = new Vector2(uCoord, 0f);

                if (i < segments)
                {
                    int ti = i * 18;
                    int next = vi + 4;

                    triangles[ti + 0] = vi + 0;
                    triangles[ti + 1] = next + 0;
                    triangles[ti + 2] = vi + 1;

                    triangles[ti + 3] = next + 0;
                    triangles[ti + 4] = next + 1;
                    triangles[ti + 5] = vi + 1;

                    triangles[ti + 6] = vi + 1;
                    triangles[ti + 7] = next + 1;
                    triangles[ti + 8] = vi + 2;

                    triangles[ti + 9] = next + 1;
                    triangles[ti + 10] = next + 2;
                    triangles[ti + 11] = vi + 2;

                    triangles[ti + 12] = vi + 2;
                    triangles[ti + 13] = next + 2;
                    triangles[ti + 14] = vi + 3;

                    triangles[ti + 15] = next + 2;
                    triangles[ti + 16] = next + 3;
                    triangles[ti + 17] = vi + 3;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
        }

        private void BuildStartFinish(Transform parent)
        {
            Vector3 centerPos = pathPoints[0];
            Vector3 tangent = pathTangents[0];
            Vector3 normal = pathNormals[0];
            float bankRad = pathBankAngles[0] * Mathf.Deg2Rad;

            Vector3 surfaceRight = normal * Mathf.Cos(bankRad) + Vector3.up * Mathf.Sin(bankRad);
            Vector3 surfaceUp = Vector3.up * Mathf.Cos(bankRad) - normal * Mathf.Sin(bankRad);

            GameObject archRoot = new GameObject("StartFinishArch");
            archRoot.transform.SetParent(parent, false);
            archRoot.transform.position = centerPos;
            archRoot.transform.rotation = Quaternion.LookRotation(tangent, surfaceUp);

            float halfSpan = (trackWidth * 0.5f) + curbWidth + 0.6f;
            float gantryHeight = 11.0f;
            float colWidth = 1.4f;

            // Inner column
            GameObject innerCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            innerCol.name = "Column_Inner";
            innerCol.transform.SetParent(archRoot.transform, false);
            innerCol.transform.localPosition = new Vector3(-halfSpan, gantryHeight * 0.5f, 0f);
            innerCol.transform.localScale = new Vector3(colWidth, gantryHeight, colWidth);
            innerCol.GetComponent<MeshRenderer>().sharedMaterial = archMaterial;

            // Outer column
            GameObject outerCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerCol.name = "Column_Outer";
            outerCol.transform.SetParent(archRoot.transform, false);
            outerCol.transform.localPosition = new Vector3(halfSpan, gantryHeight * 0.5f, 0f);
            outerCol.transform.localScale = new Vector3(colWidth, gantryHeight, colWidth);
            outerCol.GetComponent<MeshRenderer>().sharedMaterial = archMaterial;

            // Top Crossbar Beam
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Crossbar";
            beam.transform.SetParent(archRoot.transform, false);
            beam.transform.localPosition = new Vector3(0f, gantryHeight, 0f);
            beam.transform.localScale = new Vector3(halfSpan * 2f + colWidth, 1.4f, colWidth);
            beam.GetComponent<MeshRenderer>().sharedMaterial = archMaterial;

            // Checkered Banner
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "CheckeredBanner";
            banner.transform.SetParent(archRoot.transform, false);
            banner.transform.localPosition = new Vector3(0f, gantryHeight - 1.8f, 0f);
            banner.transform.localScale = new Vector3(trackWidth * 0.92f, 2.6f, 0.25f);

            Material bannerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bannerMat.name = "Mat_CheckeredBanner";
            bannerMat.mainTexture = GenerateCheckerTexture();
            bannerMat.mainTextureScale = new Vector2(28f, 4f);
            banner.GetComponent<MeshRenderer>().sharedMaterial = bannerMat;

            // FULL-WIDTH 35-METER START/FINISH CHECKERED STRIP
            // Generated as a dedicated mesh perfectly aligned with the road surface
            GameObject stripObj = new GameObject("StartFinish_FullWidthStrip");
            stripObj.transform.SetParent(parent, false);

            MeshFilter mfStrip = stripObj.AddComponent<MeshFilter>();
            MeshRenderer mrStrip = stripObj.AddComponent<MeshRenderer>();
            mrStrip.sharedMaterial = bannerMat;

            Mesh stripMesh = new Mesh();
            stripMesh.name = "StartFinish_StripMesh";

            float stripThickness = 3.5f; // 3.5 meters long along track
            float halfW = trackWidth * 0.5f;

            Vector3 pInner = centerPos - surfaceRight * halfW + surfaceUp * 0.03f;
            Vector3 pOuter = centerPos + surfaceRight * halfW + surfaceUp * 0.03f;

            Vector3[] sVerts = new Vector3[4];
            sVerts[0] = pInner - tangent * (stripThickness * 0.5f);
            sVerts[1] = pOuter - tangent * (stripThickness * 0.5f);
            sVerts[2] = pInner + tangent * (stripThickness * 0.5f);
            sVerts[3] = pOuter + tangent * (stripThickness * 0.5f);

            Vector2[] sUvs = new Vector2[4];
            sUvs[0] = new Vector2(0f, 0f);
            sUvs[1] = new Vector2(28f, 0f);
            sUvs[2] = new Vector2(0f, 4f);
            sUvs[3] = new Vector2(28f, 4f);

            int[] sTris = new int[] { 0, 2, 1, 1, 2, 3 };

            stripMesh.vertices = sVerts;
            stripMesh.normals = new Vector3[] { surfaceUp, surfaceUp, surfaceUp, surfaceUp };
            stripMesh.uv = sUvs;
            stripMesh.triangles = sTris;
            stripMesh.RecalculateBounds();

            mfStrip.sharedMesh = stripMesh;

            // Lap Trigger Box
            GameObject triggerObj = new GameObject("LapTrigger");
            triggerObj.transform.SetParent(archRoot.transform, false);
            triggerObj.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            BoxCollider triggerBox = triggerObj.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true;
            triggerBox.size = new Vector3(trackWidth + 10f, 8f, 4f);
        }

        private void BuildStartingGrid(Transform parent)
        {
            GameObject gridRoot = new GameObject("StartingGrid_Boxes");
            gridRoot.transform.SetParent(parent, false);

            int totalSlots = 16;
            float slotWidth = 3.0f;
            float slotLength = 5.5f;
            float lineWidth = 0.35f;

            // Staggered grid behind start line (indices before 0: segments - k)
            float stepDistance = 10.0f; // 10m between consecutive slots

            for (int g = 1; g <= totalSlots; g++)
            {
                float distBehindStart = 15f + g * stepDistance;
                float tBehind = 1f - (distBehindStart / targetCircumference);
                int segIdx = Mathf.Clamp(Mathf.RoundToInt(tBehind * segments), 0, segments - 1);

                Vector3 center = pathPoints[segIdx];
                Vector3 tangent = pathTangents[segIdx];
                Vector3 normal = pathNormals[segIdx];
                float bankRad = pathBankAngles[segIdx] * Mathf.Deg2Rad;

                Vector3 surfaceRight = normal * Mathf.Cos(bankRad) + Vector3.up * Mathf.Sin(bankRad);
                Vector3 surfaceUp = Vector3.up * Mathf.Cos(bankRad) - normal * Mathf.Sin(bankRad);

                // Alternating lane: Odd on left (inside), Even on right (outside)
                float lateralOffset = (g % 2 == 1) ? -(trackWidth * 0.22f) : (trackWidth * 0.22f);
                Vector3 slotCenter = center + surfaceRight * lateralOffset + surfaceUp * 0.035f;

                GameObject slotObj = new GameObject($"GridSlot_P{g:D2}");
                slotObj.transform.SetParent(gridRoot.transform, false);
                slotObj.transform.position = slotCenter;
                slotObj.transform.rotation = Quaternion.LookRotation(tangent, surfaceUp);

                // 1. Front Stop Line (Bold white bar across the car's slot)
                GameObject frontLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontLine.name = "FrontLine";
                frontLine.transform.SetParent(slotObj.transform, false);
                frontLine.transform.localPosition = new Vector3(0f, 0.01f, slotLength * 0.5f);
                frontLine.transform.localScale = new Vector3(slotWidth, 0.02f, lineWidth);
                frontLine.GetComponent<MeshRenderer>().sharedMaterial = whiteMarkingMaterial;

                // 2. Left Bracket
                GameObject leftBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftBracket.name = "LeftBracket";
                leftBracket.transform.SetParent(slotObj.transform, false);
                leftBracket.transform.localPosition = new Vector3(-slotWidth * 0.5f, 0.01f, 0f);
                leftBracket.transform.localScale = new Vector3(lineWidth, 0.02f, slotLength);
                leftBracket.GetComponent<MeshRenderer>().sharedMaterial = whiteMarkingMaterial;

                // 3. Right Bracket
                GameObject rightBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightBracket.name = "RightBracket";
                rightBracket.transform.SetParent(slotObj.transform, false);
                rightBracket.transform.localPosition = new Vector3(slotWidth * 0.5f, 0.01f, 0f);
                rightBracket.transform.localScale = new Vector3(lineWidth, 0.02f, slotLength);
                rightBracket.GetComponent<MeshRenderer>().sharedMaterial = whiteMarkingMaterial;
            }
        }

        private void BuildBrakingLines(Transform parent)
        {
            GameObject brakeRoot = new GameObject("BrakingMarkers_Road");
            brakeRoot.transform.SetParent(parent, false);

            // Find major curve apexes to place 150m, 100m, 50m countdown stripes across the road
            List<int> sharpCorners = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                if (Mathf.Abs(pathBankAngles[i]) > 3.0f)
                {
                    // Look for local peak in curvature
                    int prev = (i - 1 + segments) % segments;
                    int next = (i + 1) % segments;
                    if (Mathf.Abs(pathBankAngles[i]) >= Mathf.Abs(pathBankAngles[prev]) &&
                        Mathf.Abs(pathBankAngles[i]) >= Mathf.Abs(pathBankAngles[next]))
                    {
                        if (sharpCorners.Count == 0 || Mathf.Abs(i - sharpCorners[sharpCorners.Count - 1]) > 50)
                        {
                            sharpCorners.Add(i);
                        }
                    }
                }
            }

            int[] countdownDistances = new int[] { 150, 100, 50 };
            float segLength = targetCircumference / segments;

            foreach (int cornerIdx in sharpCorners)
            {
                for (int m = 0; m < countdownDistances.Length; m++)
                {
                    int dist = countdownDistances[m];
                    int offsetSegs = Mathf.RoundToInt(dist / segLength);
                    int markerIdx = (cornerIdx - offsetSegs + segments) % segments;

                    Vector3 center = pathPoints[markerIdx];
                    Vector3 tangent = pathTangents[markerIdx];
                    Vector3 normal = pathNormals[markerIdx];
                    float bankRad = pathBankAngles[markerIdx] * Mathf.Deg2Rad;

                    Vector3 surfaceRight = normal * Mathf.Cos(bankRad) + Vector3.up * Mathf.Sin(bankRad);
                    Vector3 surfaceUp = Vector3.up * Mathf.Cos(bankRad) - normal * Mathf.Sin(bankRad);

                    // Road countdown bar (e.g. 3 bars for 150m, 2 bars for 100m, 1 bar for 50m)
                    int numBars = 3 - m; // 3 at 150m, 2 at 100m, 1 at 50m
                    for (int b = 0; b < numBars; b++)
                    {
                        float bOffset = (b - (numBars - 1) * 0.5f) * 1.6f;
                        Vector3 barPos = center + tangent * bOffset + surfaceUp * 0.035f;

                        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        bar.name = $"Brake_{dist}m_Bar{b+1}";
                        bar.transform.SetParent(brakeRoot.transform, false);
                        bar.transform.position = barPos;
                        bar.transform.rotation = Quaternion.LookRotation(tangent, surfaceUp);
                        // Span across the outer half of the track
                        bar.transform.localScale = new Vector3(trackWidth * 0.45f, 0.02f, 0.8f);
                        bar.GetComponent<MeshRenderer>().sharedMaterial = whiteMarkingMaterial;
                    }
                }
            }
        }

        private Texture2D GenerateCheckerTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] cols = new Color[size * size];
            int checkSize = 16;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isWhite = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                    cols[y * size + x] = isWhite ? Color.white : new Color(0.12f, 0.12f, 0.12f);
                }
            }
            tex.SetPixels(cols);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        private void BuildMilestones(Transform parent)
        {
            GameObject milestonesRoot = new GameObject("MilestoneMarkers");
            milestonesRoot.transform.SetParent(parent, false);

            int numMarkers = Mathf.FloorToInt(targetCircumference / 1000f);

            Material signMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            signMat.name = "Mat_MilestoneSign";
            signMat.color = new Color(0.12f, 0.48f, 0.90f);
            signMat.SetFloat("_Smoothness", 0.5f);

            for (int km = 1; km <= numMarkers; km++)
            {
                float targetDist = km * 1000f;
                float t = targetDist / targetCircumference;
                int idx = Mathf.Clamp(Mathf.RoundToInt(t * segments), 0, segments - 1);

                Vector3 pos = pathPoints[idx];
                Vector3 fwd = pathTangents[idx];
                Vector3 right = pathNormals[idx];

                float rDist = (trackWidth * 0.5f) + curbWidth + 3.0f;
                Vector3 signPos = pos + right * rDist;

                GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = $"Marker_{km}KM";
                sign.transform.SetParent(milestonesRoot.transform, false);
                sign.transform.position = signPos + Vector3.up * 2f;
                sign.transform.localScale = new Vector3(2.0f, 3.5f, 0.3f);
                sign.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
                sign.GetComponent<MeshRenderer>().sharedMaterial = signMat;
            }
        }

        private void BuildGround(Transform parent)
        {
            GameObject ground = new GameObject("Ground_Environment");
            ground.transform.SetParent(parent, false);

            MeshFilter mf = ground.AddComponent<MeshFilter>();
            MeshRenderer mr = ground.AddComponent<MeshRenderer>();
            ground.AddComponent<MeshCollider>();

            mr.sharedMaterial = groundMaterial;

            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < segments; i++)
            {
                Vector3 p = pathPoints[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }

            float pad = 600f;
            minX -= pad; maxX += pad;
            minZ -= pad; maxZ += pad;

            Mesh mesh = new Mesh();
            mesh.name = "Ground_Mesh";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            int gridRes = 40;
            int vertCount = (gridRes + 1) * (gridRes + 1);
            Vector3[] verts = new Vector3[vertCount];
            Vector3[] norms = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] tris = new int[gridRes * gridRes * 6];

            for (int gz = 0; gz <= gridRes; gz++)
            {
                float fz = (float)gz / gridRes;
                float z = Mathf.Lerp(minZ, maxZ, fz);
                for (int gx = 0; gx <= gridRes; gx++)
                {
                    float fx = (float)gx / gridRes;
                    float x = Mathf.Lerp(minX, maxX, fx);

                    int vi = gz * (gridRes + 1) + gx;
                    verts[vi] = new Vector3(x, -0.6f, z);
                    norms[vi] = Vector3.up;
                    uvs[vi] = new Vector2(fx * 150f, fz * 150f);

                    if (gz < gridRes && gx < gridRes)
                    {
                        int ti = (gz * gridRes + gx) * 6;
                        int bl = vi;
                        int br = vi + 1;
                        int tl = vi + (gridRes + 1);
                        int tr = vi + (gridRes + 1) + 1;

                        tris[ti + 0] = bl;
                        tris[ti + 1] = tl;
                        tris[ti + 2] = br;

                        tris[ti + 3] = br;
                        tris[ti + 4] = tl;
                        tris[ti + 5] = tr;
                    }
                }
            }

            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            ground.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
