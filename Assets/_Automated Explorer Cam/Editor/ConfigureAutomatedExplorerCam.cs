using Cinemachine;
using UnityEngine;
using UnityEditor;
using WizardsCode.Spawning;
using System;

namespace WizardsCode.AI.Editor
{
    public static class ConfigureAutomatedExplorerCam
    {
        const float spawnAreaCoverage = 0.8f;

        const string followTargetPrefabName = "Camera Follow Target";
        const string followVCamPrefabName = "Follow CM vcam1";
        const string waypointSpawnerPrefabName = "Waypoint Spawner";

        [MenuItem("Tools/Wizards Code/AI/Add Automated Explorer Camera")]
        static void AddAutomatedExplorerCam() {
            Vector3 centerPos = FindCenterPosition();

            // Add Cinemachine Brain to Main Camera
            CinemachineBrain brain = Camera.main.gameObject.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                Camera.main.gameObject.AddComponent<CinemachineBrain>();
            }

            // Add Camera Follow Target prefab
            string[] assets = AssetDatabase.FindAssets("t:prefab " + followTargetPrefabName);
            string path = AssetDatabase.GUIDToAssetPath(assets[0]);
            GameObject go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)) as GameObject;
            
            MoveToWaypoint target = go.GetComponent<MoveToWaypoint>();
            target.gameObject.name = followTargetPrefabName;
            target.transform.position = centerPos;

            // Add Follow Cm VCam1 prefab
            assets = AssetDatabase.FindAssets("t:prefab " + followVCamPrefabName);
            path = AssetDatabase.GUIDToAssetPath(assets[0]);
            go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)) as GameObject;

            CinemachineVirtualCamera vcam = go.GetComponent<CinemachineVirtualCamera>();
            vcam.gameObject.name = followVCamPrefabName;
            // Set look at and follow targets of VCam
            vcam.LookAt = target.transform;
            vcam.Follow = target.transform;

            // Add Waypoint Spawner
            assets = AssetDatabase.FindAssets("t:prefab " + waypointSpawnerPrefabName);
            path = AssetDatabase.GUIDToAssetPath(assets[0]);
            go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)) as GameObject;

            BoxAreaSpawner spawner = go.GetComponent<BoxAreaSpawner>();
            spawner.gameObject.name = waypointSpawnerPrefabName;
            spawner.transform.position = centerPos;
            
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                spawner.SizeX = terrain.terrainData.size.x * spawnAreaCoverage;
                spawner.SizeZ = terrain.terrainData.size.z * spawnAreaCoverage;
            }
        }

        private static Vector3 FindCenterPosition()
        {
            Vector3 centerPos = Vector3.zero;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                centerPos = terrain.terrainData.size;
                centerPos.x = centerPos.x / 2;
                centerPos.z = centerPos.z / 2;
                centerPos.y = Terrain.activeTerrain.SampleHeight(centerPos);
            }

            return centerPos;
        }
    }
}
