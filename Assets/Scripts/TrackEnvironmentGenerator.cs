using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaceTrack
{
    [ExecuteInEditMode]
    public class TrackEnvironmentGenerator : MonoBehaviour
    {
        [Header("References")]
        public RaceTrackGenerator trackGenerator;

        [Header("Environment Density Settings")]
        [Tooltip("Step interval in path points for placing trees (lower = denser)")]
        public int treeStep = 2; // Every 2 segments (~30m)
        [Tooltip("Step interval for buildings")]
        public int buildingStep = 4; // Every 4 segments (~60m)
        [Tooltip("Step interval for grandstands / billboards")]
        public int grandstandStep = 30;

        [Header("Offsets from Track Center")]
        public float treeMinDist = 20f;
        public float treeMaxDist = 38f;
        public float buildingMinDist = 32f;
        public float buildingMaxDist = 65f;

        [Header("Materials")]
        public Material treeTrunkMaterial;
        public Material treeFoliageMaterial;
        public Material buildingMaterial;
        public Material roofMaterial;
        public Material grandstandMaterial;
        public Material billboardMaterial;

        [ContextMenu("Generate Environment")]
        public void GenerateEnvironment()
        {
            if (trackGenerator == null)
                trackGenerator = FindObjectOfType<RaceTrackGenerator>();

            if (trackGenerator == null || trackGenerator.pathPoints == null || trackGenerator.pathPoints.Length < 10)
            {
                Debug.LogError("[TrackEnvironmentGenerator] TrackGenerator pathPoints not ready! Build track first.");
                return;
            }

            EnsureMaterials();

            Transform envRoot = transform.Find("TrackEnvironment");
            if (envRoot == null)
            {
                if (gameObject.name == "TrackEnvironment")
                {
                    envRoot = transform;
                }
                else
                {
                    GameObject envGo = new GameObject("TrackEnvironment");
                    envGo.transform.SetParent(transform, false);
                    envRoot = envGo.transform;
                }
            }

            ClearEnvironmentRoot(envRoot);

            GameObject treesParent = new GameObject("Trees_Group");
            treesParent.transform.SetParent(envRoot, false);

            GameObject buildingsParent = new GameObject("Buildings_Group");
            buildingsParent.transform.SetParent(envRoot, false);

            GameObject grandstandsParent = new GameObject("Grandstands_Group");
            grandstandsParent.transform.SetParent(envRoot, false);

            Vector3[] pts = trackGenerator.pathPoints;
            Vector3[] tangents = trackGenerator.pathTangents;
            Vector3[] normals = trackGenerator.pathNormals;
            int total = pts.Length;

            Random.InitState(42); // Deterministic placement

            // Pre-create shared building materials to allow SRP Batching
            Color[] buildingColors = new Color[]
            {
                new Color(0.18f, 0.22f, 0.28f), // Charcoal Modern
                new Color(0.32f, 0.38f, 0.46f), // Slate Blue Glass
                new Color(0.72f, 0.68f, 0.62f), // Concrete Sand
                new Color(0.55f, 0.25f, 0.22f), // Terracotta Brick
                new Color(0.85f, 0.88f, 0.92f), // White Tower
                new Color(0.12f, 0.14f, 0.18f)  // Night Obelisk
            };
            Material[] sharedBldgMats = new Material[buildingColors.Length];
            for (int m = 0; m < buildingColors.Length; m++)
            {
                sharedBldgMats[m] = new Material(buildingMaterial);
                sharedBldgMats[m].name = $"Mat_Bldg_{m}";
                sharedBldgMats[m].color = buildingColors[m];
            }

            // 1. Generate Trees along grass verges
            int treeCount = 0;
            for (int i = 0; i < total; i += treeStep)
            {
                Vector3 p = pts[i];
                Vector3 t = (tangents != null && i < tangents.Length) ? tangents[i] : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, t).normalized;

                // Outer side tree
                float outerDist = Random.Range(treeMinDist, treeMaxDist);
                Vector3 posOuter = p + right * outerDist;
                posOuter.y = GetGroundHeight(posOuter.x, posOuter.z);
                CreateTree(treesParent.transform, posOuter, Random.Range(0.85f, 1.35f), i % 3 == 0);
                treeCount++;

                // Inner side tree (chance based to leave open vistas)
                if (Random.value > 0.35f)
                {
                    float innerDist = Random.Range(treeMinDist, treeMaxDist);
                    Vector3 posInner = p - right * innerDist;
                    posInner.y = GetGroundHeight(posInner.x, posInner.z);
                    CreateTree(treesParent.transform, posInner, Random.Range(0.8f, 1.25f), (i + 1) % 3 == 0);
                    treeCount++;
                }
            }

            // 2. Generate Buildings / Houses / City Skyline
            int buildingCount = 0;

            for (int i = 0; i < total; i += buildingStep)
            {
                Vector3 p = pts[i];
                Vector3 t = (tangents != null && i < tangents.Length) ? tangents[i] : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, t).normalized;

                // Alternate outer and inner placement
                bool placeOuter = (i % (buildingStep * 2) == 0) || Random.value > 0.4f;
                bool placeInner = !placeOuter || Random.value > 0.6f;

                if (placeOuter)
                {
                    float dist = Random.Range(buildingMinDist, buildingMaxDist);
                    Vector3 pos = p + right * dist;
                    pos.y = GetGroundHeight(pos.x, pos.z);
                    Quaternion rot = Quaternion.LookRotation(t, Vector3.up);
                    CreateBuilding(buildingsParent.transform, pos, rot, sharedBldgMats[i % sharedBldgMats.Length]);
                    buildingCount++;
                }

                if (placeInner)
                {
                    float dist = Random.Range(buildingMinDist + 5f, buildingMaxDist + 15f);
                    Vector3 pos = p - right * dist;
                    pos.y = GetGroundHeight(pos.x, pos.z);
                    Quaternion rot = Quaternion.LookRotation(t, Vector3.up);
                    CreateBuilding(buildingsParent.transform, pos, rot, sharedBldgMats[(i + 2) % sharedBldgMats.Length]);
                    buildingCount++;
                }
            }

            // 3. Generate Grandstands & Sponsor Billboards near straightaways
            int standCount = 0;
            for (int i = 0; i < total; i += grandstandStep)
            {
                Vector3 p = pts[i];
                Vector3 t = (tangents != null && i < tangents.Length) ? tangents[i] : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, t).normalized;

                // Grandstand on outer side
                Vector3 standPos = p + right * 24f;
                standPos.y = GetGroundHeight(standPos.x, standPos.z);
                Quaternion standRot = Quaternion.LookRotation(-right, Vector3.up); // Facing track!
                CreateGrandstand(grandstandsParent.transform, standPos, standRot);
                standCount++;
            }

            Debug.Log($"<color=#00FF88><b>[TrackEnvironmentGenerator]</b> Environment built! Trees: {treeCount}, Buildings: {buildingCount}, Grandstands: {standCount}</color>");
        }

        [ContextMenu("Clear Environment")]
        public void ClearEnvironment()
        {
            Transform envRoot = transform.Find("TrackEnvironment");
            if (envRoot != null)
            {
                ClearEnvironmentRoot(envRoot);
                if (gameObject.name != "TrackEnvironment")
                {
                    if (Application.isPlaying) Destroy(envRoot.gameObject);
                    else DestroyImmediate(envRoot.gameObject);
                }
            }
            else if (gameObject.name == "TrackEnvironment")
            {
                ClearEnvironmentRoot(transform);
            }
        }

        private void ClearEnvironmentRoot(Transform root)
        {
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in root)
            {
                toDestroy.Add(child.gameObject);
            }
            foreach (var go in toDestroy)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }

        private float GetGroundHeight(float x, float z)
        {
            return 0.05f; // Flat terrain level
        }

        // ==========================================
        // 1. PROCEDURAL TREE BUILDER
        // ==========================================
        private void CreateTree(Transform parent, Vector3 position, float scale, bool isPine)
        {
            GameObject treeObj = new GameObject(isPine ? "PineTree" : "OakTree");
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.position = position;
            treeObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            treeObj.transform.localScale = Vector3.one * scale;

            // Trunk (Cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeObj.transform, false);
            float trunkH = isPine ? 3.5f : 2.5f;
            trunk.transform.localScale = new Vector3(0.5f, trunkH * 0.5f, 0.5f);
            trunk.transform.localPosition = new Vector3(0, trunkH * 0.5f, 0);

            var trunkMr = trunk.GetComponent<MeshRenderer>();
            trunkMr.sharedMaterial = treeTrunkMaterial;
            trunkMr.shadowCastingMode = ShadowCastingMode.On;
            trunkMr.receiveShadows = true;
            DestroyCollider(trunk);

            if (isPine)
            {
                // Pine: 3 stacked cones (Cylinder tapered or Pyramids/Spheres)
                for (int level = 0; level < 3; level++)
                {
                    GameObject tier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    tier.name = $"FoliageTier_{level}";
                    tier.transform.SetParent(treeObj.transform, false);

                    float tierY = trunkH + level * 2.2f;
                    float radius = 3.2f - level * 0.8f;
                    tier.transform.localPosition = new Vector3(0, tierY, 0);
                    tier.transform.localScale = new Vector3(radius, 2.5f, radius);

                    var mr = tier.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = treeFoliageMaterial;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    mr.receiveShadows = true;
                    DestroyCollider(tier);
                }
            }
            else
            {
                // Broadleaf: 1 large canopy sphere + 2 cluster spheres
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "FoliageCrown";
                crown.transform.SetParent(treeObj.transform, false);
                crown.transform.localPosition = new Vector3(0, trunkH + 2.8f, 0);
                crown.transform.localScale = new Vector3(4.8f, 4.2f, 4.8f);

                var mr = crown.GetComponent<MeshRenderer>();
                mr.sharedMaterial = treeFoliageMaterial;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                DestroyCollider(crown);

                // Sub-cluster
                GameObject sub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sub.name = "FoliageSub";
                sub.transform.SetParent(treeObj.transform, false);
                sub.transform.localPosition = new Vector3(1.2f, trunkH + 3.2f, 0.8f);
                sub.transform.localScale = new Vector3(3.2f, 2.8f, 3.2f);

                var subMr = sub.GetComponent<MeshRenderer>();
                subMr.sharedMaterial = treeFoliageMaterial;
                subMr.shadowCastingMode = ShadowCastingMode.On;
                subMr.receiveShadows = true;
                DestroyCollider(sub);
            }
        }

        // ==========================================
        // 2. PROCEDURAL BUILDING BUILDER
        // ==========================================
        private void CreateBuilding(Transform parent, Vector3 position, Quaternion rotation, Material wallMat)
        {
            GameObject bldgObj = new GameObject("Building");
            bldgObj.transform.SetParent(parent, false);
            bldgObj.transform.position = position;
            bldgObj.transform.rotation = rotation;

            float width = Random.Range(14f, 26f);
            float depth = Random.Range(14f, 24f);
            float height = Random.Range(18f, 65f); // 6-20 stories high

            // Main Tower (Cube)
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Tower";
            tower.transform.SetParent(bldgObj.transform, false);
            tower.transform.localPosition = new Vector3(0, height * 0.5f, 0);
            tower.transform.localScale = new Vector3(width, height, depth);

            var towerMr = tower.GetComponent<MeshRenderer>();
            towerMr.sharedMaterial = wallMat;
            towerMr.shadowCastingMode = ShadowCastingMode.On;
            towerMr.receiveShadows = true;
            DestroyCollider(tower);

            // Roof Parapet / Top Structure
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "RoofStructure";
            roof.transform.SetParent(bldgObj.transform, false);
            roof.transform.localPosition = new Vector3(0, height + 1.5f, 0);
            roof.transform.localScale = new Vector3(width * 0.75f, 3.0f, depth * 0.75f);

            var roofMr = roof.GetComponent<MeshRenderer>();
            roofMr.sharedMaterial = roofMaterial;
            roofMr.shadowCastingMode = ShadowCastingMode.On;
            roofMr.receiveShadows = true;
            DestroyCollider(roof);

            // High-rise Spire / Antenna on tallest skyscrapers (> 45m)
            if (height > 45f)
            {
                GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spire.name = "Antenna";
                spire.transform.SetParent(bldgObj.transform, false);
                spire.transform.localPosition = new Vector3(0, height + 8f, 0);
                spire.transform.localScale = new Vector3(0.4f, 5.0f, 0.4f);

                var spireMr = spire.GetComponent<MeshRenderer>();
                spireMr.sharedMaterial = roofMaterial;
                spireMr.shadowCastingMode = ShadowCastingMode.On;
                spireMr.receiveShadows = true;
                DestroyCollider(spire);
            }
        }

        // ==========================================
        // 3. GRANDSTAND / BLEACHERS BUILDER
        // ==========================================
        private void CreateGrandstand(Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject standObj = new GameObject("Grandstand");
            standObj.transform.SetParent(parent, false);
            standObj.transform.position = position;
            standObj.transform.rotation = rotation;

            float standLength = 40f;
            float standDepth = 18f;
            float standHeight = 10f;

            // Tiered base structure
            GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBlock.name = "Tiers";
            baseBlock.transform.SetParent(standObj.transform, false);
            baseBlock.transform.localPosition = new Vector3(0, standHeight * 0.5f, standDepth * 0.5f);
            baseBlock.transform.localScale = new Vector3(standLength, standHeight, standDepth);

            var baseMr = baseBlock.GetComponent<MeshRenderer>();
            baseMr.sharedMaterial = grandstandMaterial;
            baseMr.shadowCastingMode = ShadowCastingMode.On;
            baseMr.receiveShadows = true;
            DestroyCollider(baseBlock);

            // Canopy / Roof
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(standObj.transform, false);
            canopy.transform.localPosition = new Vector3(0, standHeight + 4.5f, standDepth * 0.4f);
            canopy.transform.localScale = new Vector3(standLength + 2f, 0.8f, standDepth + 4f);

            var canopyMr = canopy.GetComponent<MeshRenderer>();
            canopyMr.sharedMaterial = roofMaterial;
            canopyMr.shadowCastingMode = ShadowCastingMode.On;
            canopyMr.receiveShadows = true;
            DestroyCollider(canopy);

            // Billboard Banner along the front of grandstand
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "SponsorBanner";
            banner.transform.SetParent(standObj.transform, false);
            banner.transform.localPosition = new Vector3(0, 1.8f, -0.2f);
            banner.transform.localScale = new Vector3(standLength, 2.5f, 0.3f);

            var bannerMr = banner.GetComponent<MeshRenderer>();
            bannerMr.sharedMaterial = billboardMaterial;
            bannerMr.shadowCastingMode = ShadowCastingMode.On;
            bannerMr.receiveShadows = true;
            DestroyCollider(banner);
        }

        private void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private void EnsureMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            if (treeTrunkMaterial == null)
            {
                treeTrunkMaterial = new Material(litShader);
                treeTrunkMaterial.name = "Mat_TreeTrunk";
                treeTrunkMaterial.color = new Color(0.35f, 0.22f, 0.12f);
            }

            if (treeFoliageMaterial == null)
            {
                treeFoliageMaterial = new Material(litShader);
                treeFoliageMaterial.name = "Mat_TreeFoliage";
                treeFoliageMaterial.color = new Color(0.12f, 0.48f, 0.18f); // Vibrant Forest Green
            }

            if (buildingMaterial == null)
            {
                buildingMaterial = new Material(litShader);
                buildingMaterial.name = "Mat_BuildingWall";
                buildingMaterial.color = new Color(0.24f, 0.28f, 0.36f);
            }

            if (roofMaterial == null)
            {
                roofMaterial = new Material(litShader);
                roofMaterial.name = "Mat_RoofMetal";
                roofMaterial.color = new Color(0.15f, 0.17f, 0.20f);
            }

            if (grandstandMaterial == null)
            {
                grandstandMaterial = new Material(litShader);
                grandstandMaterial.name = "Mat_Grandstand";
                grandstandMaterial.color = new Color(0.18f, 0.38f, 0.72f); // Racing Blue Grandstand
            }

            if (billboardMaterial == null)
            {
                billboardMaterial = new Material(litShader);
                billboardMaterial.name = "Mat_Billboard";
                billboardMaterial.color = new Color(0.95f, 0.22f, 0.08f); // Vibrant Sponsor Red
            }
        }
    }
}
