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

        [SerializeField, Tooltip("The maximum vertical velocity this object is expected to reach. This is used to normalize the animator parameters.")]
        float m_MaxVerticalVelocity = 6;
        [SerializeField, Tooltip("The maximum vertical velocity this object is expected to reach. This is used to normalize the animator parameters.")]
        float m_MaxForwardVelocity = 20;

        [SerializeField, Tooltip("The animation controller for updating speed accordingly.")]
        private Animator m_Animator;

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
        }

        /// <summary>
        /// FIXME: This requires that the `SteeringRig.FixedUpdate` method is marked `protected virtual` which it is not out of the box. 
        /// I've made a request to do this at https://forum.unity.com/threads/released-sensor-toolkit.468255/
        /// but at the time of writing you will need to make this edit yourself.
        /// </summary>
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            Vector3 additionalForce = MaintainHeight();

            additionalForce += ApplyRandomizationForce();

            RB.AddForce(additionalForce);

            SetAnimationParameters();
        }

        void SetAnimationParameters()
        {
            // TODO:Allow animations to be disabled
            if (m_Animator)
            {
                Vector3 velocity = RB.transform.InverseTransformDirection(RB.velocity);

                //TODO: Use Hash not string
                m_Animator.SetFloat("angularVelocity", RB.angularVelocity.y);

                if (velocity.y > 0)
                {
                    m_Animator.SetFloat("verticalVelocity", Mathf.Clamp01(velocity.y / m_MaxVerticalVelocity));
                }
                else
                {
                    m_Animator.SetFloat("verticalVelocity", -Mathf.Clamp01(-velocity.y / m_MaxVerticalVelocity));
                }
                if (velocity.z > 0)
                {
                    m_Animator.SetFloat("forwardVelocity", Mathf.Clamp01(velocity.z / m_MaxForwardVelocity));
                } 
                else
                {
                    m_Animator.SetFloat("forwardVelocity", -Mathf.Clamp01(-velocity.z / m_MaxForwardVelocity));
                }
            }
        }

        /// <summary>
        /// Calculate a random force that will be added to the object to add variation to the flight.
        /// </summary>
        private Vector3 ApplyRandomizationForce()
        {
            Vector3 randomForce = Vector3.zero;
            if (!(m_RandomizeX || m_RandomizeX || m_RandomizeX)) return randomForce;

            if (timeOFRandomization > Time.timeSinceLevelLoad)
            {
                randomForce = Vector3.Slerp(Vector3.zero, targetRandomizationForce, (Time.timeSinceLevelLoad - timeOFRandomization) / m_RandomizationFrequency);
                return randomForce;
            }

            timeOFRandomization = Time.timeSinceLevelLoad + m_RandomizationFrequency;

            float targetX = 0;
            float targetY = 0;
            float targetZ = 0;
            if (m_RandomizeX)
            {
                if (Random.value < 0.7)
                {
                    targetX = StrafeForce * (1 + Random.Range(-m_RandomizationFactor, m_RandomizationFactor));
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

                    targetZ *= MoveForce;
                }
            }

            targetRandomizationForce = new Vector3(targetX, targetY, targetZ);

            return Vector3.Slerp(Vector3.zero, targetRandomizationForce, (Time.timeSinceLevelLoad - timeOFRandomization) / m_RandomizationFrequency);
        }


        /// <summary>
        /// Get a force that is used to adjuste the height of the body based on the Height Management settings.
        /// </summary>
        /// <returns>A force to be applied to the body.</returns>
        private Vector3 MaintainHeight()
        {
            Vector3 heightAdjustmentForce = Vector3.zero;
            if (!m_MaintainHeight) return heightAdjustmentForce;
            
            RaycastHit hit;
            float height = 0;
            if (Physics.Raycast(RB.transform.position, Vector3.down, out hit, Mathf.Infinity))
            {
                height = hit.distance;
            }
            else
            {
                Debug.LogWarning($"{name} has `Maintain Height` enabled but therre is no raycast hit below it. Relying on Steering Behaviours to keep things in order.");
                return heightAdjustmentForce;
            }

            /* What was this for? doesn't really do anything useful that I can think of. Commented out to see what it does... 1/24/22 
            float waypointHeight = Destination.y - height;

            if (waypointHeight > optimalHeight || waypointHeight < optimalHeight) return;
            */
            if (height > optimalHeight)
            {
                if (height > maxHeight)
                {
                    heightAdjustmentForce = -RB.transform.up * Mathf.Lerp(0, MoveForce, Mathf.Clamp01((height - maxHeight) / maxHeight));
                } 
                else
                {
                    heightAdjustmentForce = -RB.transform.up * Mathf.Lerp(0, MoveForce, Mathf.Clamp01((height - optimalHeight) / optimalHeight));
                }
            }
            else if (height < optimalHeight)
            {
                if (height < minHeight)
                {
                    heightAdjustmentForce = RB.transform.up * Mathf.Lerp(0, MoveForce * 3, Mathf.Clamp01((minHeight - height) / minHeight));
                }
                else
                {
                    heightAdjustmentForce = RB.transform.up * Mathf.Lerp(0, MoveForce * 2, Mathf.Clamp01((optimalHeight - height) / optimalHeight));
                }
            }

            return heightAdjustmentForce;
        }
    }
}
