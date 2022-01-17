using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SensorToolkit;

namespace WizardsCode.AI
{
    /// <summary>
    /// A steering rig designed to drive a motion for a camera or a camera follow target.
    /// </summary>
    public class CameraSteeringRig : SteeringRig
    {
        [SerializeField, Tooltip("Try to keep the camera within a certain height range (true) or allow any height (false)." +
            " If this is true the following settings will confine the height.")]
        bool m_MaintainHeight = true;
        [SerializeField, Tooltip("The minimum height above the ground or nearest obstacle that this rig" +
            " should be. This is used as a validation check to ensure the object is not going below" +
            " the terrain or through a mesh obstacles in the scene.")]
        float minHeight = 1.5f;
        [SerializeField, Tooltip("The maximum height above the ground or nearest obstacle that this rig" +
            " should be. This is used as a validation check to ensure the object is not going too" +
            " far above the terrain or mesh obstacles in the scene. The camera may go above this height" +
            " but if it does it will have forces placed upon it to move down.")]
        float maxHeight = 10f;
        [SerializeField, Tooltip("The optimal height above the ground or nearest obstacle that this rig" +
            " should be. The camera is allowed to move from this height but it will recieve gentle forces" +
            " encourage it to return to this height.")]
        float optimalHeight = 2f;

        [SerializeField, Tooltip("Randomize the speed of this AI by up to +/- 10% on startup.")]
        bool m_RandomizeSpeedOnStart = false;
        [SerializeField, Tooltip("The animation controller for updating speed accordingly.")]
        private Animator m_Animator;
        [SerializeField, Tooltip("If you model is using the legacy animation system all is not lost, set it here and we will do the rest.")]
        private Animation m_LegacyAnimation;

        private void Start()
        {
            if (m_RandomizeSpeedOnStart) {
                RandomizeSpeed();
            }
        }

        /// <summary>
        /// Randomize the speed of the animator and the movement.
        /// </summary>
        private void RandomizeSpeed()
        {
            float variation = Random.Range(0.9f, 1.1f);
            MoveForce *= variation;

            if (m_Animator)
            {
                m_Animator.speed = m_Animator.speed * variation;
            } else if (m_LegacyAnimation)
            {
                foreach (AnimationState state in m_LegacyAnimation)
                {
                    state.speed *= variation;
                }
            }
        }

        void LateUpdate()
        {
            if (!m_MaintainHeight) return;
                
            if (RB == null || RB.isKinematic || !IsSeeking) return;


            RaycastHit hit;
            float terrainHeight = 0;
            if (Physics.Raycast(RB.transform.position, Vector3.down, out hit, Mathf.Infinity))
            {
                terrainHeight = hit.distance;
            } else
            {
                Debug.LogWarning($"{name} has `Maintain Height` enabled but therre is no raycast hit below it. Relying on Steering Behaviours to keep things in order.");
                return;
            }

            float height = RB.transform.position.y - terrainHeight;
            if (height > optimalHeight)
            {
                if (height > maxHeight)
                {
                    RB.AddForce(RB.transform.up * -(MoveForce));
                }
                else
                {
                    RB.AddForce(RB.transform.up * -(MoveForce / 2));
                }
            }
            else if (height < optimalHeight)
            {
                if (height < minHeight)
                {
                    RB.AddForce(RB.transform.up * (MoveForce));
                }
                else
                {
                    RB.AddForce(RB.transform.up * (MoveForce / 2));
                }
            }
        }
    }
}
