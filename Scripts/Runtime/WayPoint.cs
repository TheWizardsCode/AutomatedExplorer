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
    public class WayPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("The relative weight of this waypoint in terms of it's interest. A higher value will increase the chances of this waypoint being selected all other choice factors being equal."), Range(0.001f, 1f)]
        internal float weight = 0.5f;
        [SerializeField, Tooltip("Time, in seconds, to wait before re-enabling the waypoint after it is visited. If 0 the waypoint will not be re-enabled.")]
        internal float reEnableWaitTime = 0;

        bool isEnabled = false;
        float timeToReEnable = 0;

        private void Start()
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled)
                {
                    isEnabled = true;
                    break;
                }
            }

            timeToReEnable = float.PositiveInfinity;
        }


        private void Update()
        {
            if (!isEnabled && timeToReEnable <= Time.timeSinceLevelLoad)
            {
                SetEnabled(true);
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
            SetEnabled(false);
            if (reEnableWaitTime > 0)
            {
                timeToReEnable = Time.timeSinceLevelLoad + reEnableWaitTime;
            }
        }

        /// <summary>
        /// Enable the waypoint so that it will be detected. This is achieved by turning
        /// off all colliders attached to the same object (not colliders in the parent or children).
        /// </summary>
        public void Enable()
        {
            SetEnabled(true);
        }

        private void SetEnabled(bool enabled)
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = enabled;
            }

            isEnabled = enabled;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}