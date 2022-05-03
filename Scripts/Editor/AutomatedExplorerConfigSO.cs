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
        [SerializeField, Tooltip("The prefab that defines the camera follow target. This will have the model if the target is visisble.")]
        public MoveToWaypoint followTarget;
        public CinemachineVirtualCameraBase followVCam;
        public BoxAreaSpawner waypointSpawner;
    }
}