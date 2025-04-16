using Unity.Cinemachine;
using UnityEngine;
using UnityEditor;
using WizardsCode.Spawning;
using System;

namespace WizardsCode.AI.Editor
{
    public class ConfigureAutomatedExplorerCam : EditorWindow
    {
        public AutomatedExplorerConfigSO m_config;

        const float spawnAreaCoverage = 0.8f;

        //const string followTargetPrefabName = "Camera Follow Target";
        //const string followVCamPrefabName = "Follow CM vcam1";
        //const string waypointSpawnerPrefabName = "Waypoint Spawner";

        [MenuItem("Tools/Wizards Code/AI/Automated Explorer Window")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ConfigureAutomatedExplorerCam), false, "Automated Explorer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            m_config = (AutomatedExplorerConfigSO)EditorGUILayout.ObjectField(m_config, typeof(AutomatedExplorerConfigSO), true);

            if (m_config == null)
            {
                EditorGUILayout.HelpBox("Set an Explorer Config in order to continue.", MessageType.Warning);
                return;
            }

            if (m_config != null)
            {
                UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(m_config);
                editor.OnInspectorGUI();
            }

            if (GUILayout.Button("Configure the scene")) {
                SetupAutomatedExplorer();
            }
        }

        [MenuItem("Tools/Wizards Code/AI/(Deprecated) Add Automated Explorer Camera")]
        [Obsolete("Use ShowWindow instead")]
        static void AddAutomatedExplorerCam() {
            EditorUtility.DisplayDialog("Deprecated Option", "The Automated Explorer is now configured from the Editor Window opened via Tools/Wizards Code/AI/Automated Explorer Window. This menu item will go away in a future version.", "Open Editor Window");
            ShowWindow();
            //SetupAutomatedExplorer();
        }

        void SetupAutomatedExplorer() {
            Vector3 centerPos = FindCenterPosition();
            Camera camera = Camera.main;

            if (camera == null)
            {
                EditorUtility.DisplayDialog("Can't find camera", "You do not have a camera marke as main. Please mark the camera you want to use as the automated camera and try again.", "OK");
                return;
            }

            // Add Cinemachine Brain to Main Camera
            CinemachineBrain brain = camera.gameObject.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                camera.gameObject.AddComponent<CinemachineBrain>();
            }

            // Add and configure Camera Follow Target prefab
            MoveToWaypoint target = PrefabUtility.InstantiatePrefab(m_config.followTarget) as MoveToWaypoint;

            GameObject go = target.gameObject;
            target.gameObject.name = m_config.followTarget.name;
            target.transform.position = centerPos;

            // Add Follow Cinemachine camera prefab
            CinemachineCamera vcam = PrefabUtility.InstantiatePrefab(m_config.followVCam) as CinemachineCamera;
            vcam.gameObject.name = m_config.followVCam.name;
            vcam.LookAt = target.transform;
            vcam.Follow = target.transform;

            go = vcam.gameObject;
            go.transform.position = camera.transform.position;
            go.transform.rotation = camera.transform.rotation;

            // Add Waypoint Spawner
            BoxAreaSpawner spawner = PrefabUtility.InstantiatePrefab(m_config.waypointSpawner) as BoxAreaSpawner;
            spawner.gameObject.name = m_config.waypointSpawner.name;
            spawner.transform.position = centerPos;

            go = spawner.gameObject;
            go.transform.position = centerPos;
            go.transform.rotation = Quaternion.identity;

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
                Vector3 size = terrain.terrainData.size;
                centerPos = terrain.transform.position;
                centerPos.x = centerPos.x + (size.x / 2);
                centerPos.z = centerPos.z + (size.x / 2);
                centerPos.y = Terrain.activeTerrain.SampleHeight(centerPos);
            }

            return centerPos;
        }
    }
}
