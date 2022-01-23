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

        [SerializeField, Tooltip("The animation controller for updating speed accordingly.")]
        private Animator m_Animator;
        [SerializeField, Tooltip("If you model is using the legacy animation system all is not lost, set it here and we will do the rest.")]
        private Animation m_LegacyAnimation;

        [SerializeField, Tooltip("Should forces be applied randomly in the x direction.")]
        private bool m_RandomizeX = true;
        [SerializeField, Tooltip("Should forces be applied randomly in the y direction.")]
        private bool m_RandomizeY = true;
        [SerializeField, Tooltip("Should forces be applied randomly in the z direction. Since Z is (usually) the forward direction the speed of the animator will also be modified according to this value.")]
        private bool m_RandomizeZ = true;
        [SerializeField, Tooltip("Frequency of randomization force change in seconds. Each time the randomization force is changed it will be in the opposite direction to the last change, thus the change will not often push the object too far off course.")]
        public float m_RandomizationFrequency = 3;
        [SerializeField, Tooltip("The amount of randomization in each direction. The force in each direction will be adjusted up or down by this % amount.")]
        [Range(0, 1f)]
        float m_RandomizationFactor = 0.1f;

        private float timeOFRandomization;
        private float speedVariation;
        private float originalMoveForce;
        private float targetMoveForce;
        private float originalSpeed;
        private Vector3 targetRandomizationForce;

        private void Start()
        {
            originalMoveForce = MoveForce;

            if (m_Animator)
            {
                originalSpeed = m_Animator.speed;
            }
            else if (m_LegacyAnimation)
            {
                Debug.LogWarning("You are randomizing speed with a legacy animator. This will work fine if all clips have the same speed. Otherwise they will be adjusted to an identical speed making some animations look off. This is a limitation of the curent code and is unlikely to be fixed given this is for legacy animations. Patches welcome if you actually have a need.");
                float speed = 0;
                foreach (AnimationState state in m_LegacyAnimation)
                {
                    speed += state.speed;
                }
                originalSpeed = speed / m_LegacyAnimation.GetClipCount();
            }
        }

        /// <summary>
        /// FIXME: This requires that the SteeringUpdate base class is marketed `protected virtual` which it is not out of the box. 
        /// I've made a request to do this at https://forum.unity.com/threads/released-sensor-toolkit.468255/
        /// but at the time of writing you will need to make this edit yourself.
        /// </summary>
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (m_MaintainHeight)
            {
                MaintainHeight();
            }

            ApplyRandomizationForce();

            SetAnimationParameters();
        }

        void SetAnimationParameters()
        {
            if (m_Animator)
            {
                Vector3 velocity = RB.transform.InverseTransformDirection(RB.velocity);

                //m_Animator.SetFloat("x", velocity.x);
                m_Animator.SetFloat("angularVelocity", RB.angularVelocity.y);
                m_Animator.SetFloat("verticalVelocity", velocity.y);
                m_Animator.SetFloat("forwardVelocity", velocity.z);
            }
        }

        /// <summary>
        /// Calculate a random force that will be added to the object to add variation to the flight.
        /// </summary>
        private void ApplyRandomizationForce()
        {
            if (!(m_RandomizeX || m_RandomizeX || m_RandomizeX)) return;
            if (timeOFRandomization > Time.timeSinceLevelLoad)
            {
                RB.AddForce(Vector3.Slerp(Vector3.zero, targetRandomizationForce, (Time.timeSinceLevelLoad - timeOFRandomization) / m_RandomizationFrequency));
                return;
            }
            timeOFRandomization = Time.timeSinceLevelLoad + m_RandomizationFrequency;

            float targetX = 0;
            float targetY = 0;
            float targetZ = 0;
            if (m_RandomizeX)
            {
                if (Random.value < 0.7)
                {
                    targetX = MoveForce * (1 + Random.Range(-m_RandomizationFactor, m_RandomizationFactor));
                }
            }
            if (m_RandomizeY)
            {
                if (Random.value < 0.7)
                {
                    targetY = MoveForce * (1 + Random.Range(-m_RandomizationFactor, m_RandomizationFactor));
                }
            }
            if (m_RandomizeZ)
            {
                if (Random.value < 0.7)
                {
                    targetZ = 1 + Random.Range(-m_RandomizationFactor, m_RandomizationFactor);

                    if (m_Animator)
                    {
                        m_Animator.speed = originalSpeed * targetZ;
                    }
                    else if (m_LegacyAnimation)
                    {
                        foreach (AnimationState state in m_LegacyAnimation)
                        {
                            state.speed = targetZ;
                        }
                    }

                    targetZ *= MoveForce;
                }
            }

            targetRandomizationForce = new Vector3(targetX, targetY, targetZ);
        }

        private void MaintainHeight()
        {
            RaycastHit hit;
            float terrainHeight = 0;
            if (Physics.Raycast(RB.transform.position, Vector3.down, out hit, Mathf.Infinity))
            {
                terrainHeight = hit.distance;
            }
            else
            {
                Debug.LogWarning($"{name} has `Maintain Height` enabled but therre is no raycast hit below it. Relying on Steering Behaviours to keep things in order.");
                return;
            }

            float objectHeight = transform.position.y - terrainHeight;
            float waypointHeight = Destination.y - terrainHeight;

            if (waypointHeight > optimalHeight || waypointHeight < optimalHeight) return;

            if (objectHeight > optimalHeight)
            {
                if (objectHeight > maxHeight)
                {
                    RB.AddForce(RB.transform.up * -(MoveForce));
                }
                else
                {
                    RB.AddForce(RB.transform.up * -(MoveForce / 2));
                }
            }
            else if (objectHeight < optimalHeight)
            {
                if (objectHeight < minHeight)
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
