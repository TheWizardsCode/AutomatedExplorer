using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizardsCode.Spawning;

namespace WizardsCode.AI.Editor
{
    [CreateAssetMenu(fileName = "New Automated Camera Config", menuName = "Wizards Code/AI/Automated Explorer Config")]
    public class AutomatedExplorerConfigSO : ScriptableObject
    {
        [SerializeField, Tooltip("A prefab that defines the camera follow target. This will have the model if the target is visisble.")]
        public MoveToWaypoint followTarget;
        [SerializeField, Tooltip("A prefab that defines the Cinemachine camera that will be configured to follow the followTarget.")]
        public CinemachineVirtualCameraBase followVCam;
        [SerializeField, Tooltip("A prefab for creating a waypoint spawner for the followTarget to follow.")]
        public BoxAreaSpawner waypointSpawner;
    }
}