using UnityEngine;
using System.Collections;
using System;

namespace WizardsCode.AI
{
    /// <summary>
    /// Defines a waypoint that represents a point in space.
    /// 
    /// Waypoints are used to mark items of interest and points along a route
    /// or points of interest.
    /// 
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WayPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("The radiues that will be tested for obstructions. If an obstruction is found within this radius then a new spawn point will be generated.")]
        public float ClearRadius = 1.8f;
        [SerializeField, Tooltip("The relative weight of this waypoint in terms of it's interest. A higher value will increase the chances of this waypoint being selected all other choice factors being equal. A weight of 0 means it will never be selected, this is useful when you want a waypoint that can only be assigned in code."), Range(0f, 1f)]
        internal float weight = 0.5f;
        [SerializeField, Tooltip("Time, in seconds, to wait before re-enabling the waypoint after it is visited. If 0 the waypoint will destroyed once visited.")]
        internal float reEnableWaitTime = 0;
        [SerializeField, Tooltip("If set the next waypoint selected will be the one set here, assuming that it is still present in the scene.")]
        public WayPoint NextWaypoint;

        float timeToReEnable = 0;

        bool m_IsEnabled = true;
        /// <summary>
        /// Enables or disables this waypoint. This is different from activating or deactivating the GameObject.
        /// When disabled the colliders will be turned off to prevent it being detected, but the scripts attached
        /// will continue to operate.
        /// </summary>
        public bool IsEnabled
        {
            get { return m_IsEnabled; } 
            internal set
            {
                m_IsEnabled = value;

                Collider[] colliders = GetComponents<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = m_IsEnabled;
                }
            }
        }

        private void Start()
        {
            IsEnabled = true;
            timeToReEnable = float.PositiveInfinity;
        }


        private void Update()
        {
            if (!IsEnabled && timeToReEnable <= Time.timeSinceLevelLoad)
            {
                IsEnabled = true;
            }
        }

        /// <summary>
        /// Disable the waypoint so that it will no longer be detected. This is achieved by turning
        /// off all colliders attached to the same object (not colliders in the parent or children).
        /// 
        /// If timeToReEnable > 0 then the waypoint will be reenabled after that many seconds.
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
            if (reEnableWaitTime > 0)
            {
                timeToReEnable = Time.timeSinceLevelLoad + reEnableWaitTime;
            } else
            {
                Destroy(this.gameObject);
            }
        }

        /// <summary>
        /// Enable the waypoint so that it will be detected. This is achieved by turning
        /// off all colliders attached to the same object (not colliders in the parent or children).
        /// </summary>
        public void Enable()
        {
            IsEnabled =true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, ClearRadius);
        }
    }
}