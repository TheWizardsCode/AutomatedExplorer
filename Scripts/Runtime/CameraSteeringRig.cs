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
        bool m_RandomizeSpeed = false;
        [SerializeField, Tooltip("The animation controller for updating speed accordingly.")]
        private Animator m_Animator;
        [SerializeField, Tooltip("If you model is using the legacy animation system all is not lost, set it here and we will do the rest.")]
        private Animation m_LegacyAnimation;

        [SerializeField, Tooltip("Should forces be applied randomly in the x direction.")]
        private bool m_RandomizeX = true;
        [SerializeField, Tooltip("Should forces be applied randomly in the y direction.")]
        private bool m_RandomizeY = true;
        [SerializeField, Tooltip("Should forces be applied randomly in the z direction.")]
        private bool m_RandomizeZ = true;
        [SerializeField, Tooltip("Frequency of randomization force change in seconds. Each time the randomization force is changed it will be in the opposite direction to the last change, thus the change will not often push the object too far off course.")]
        public float m_RandomizationFrequency = 3;
        [SerializeField, Tooltip("The amount of randomization in each direction. The force in each direction will be between `Move Force` and +/-`MoveForce/10 * RandomicationFactor`.")]
        float m_RandomizationFactor = 1.1f;

        private Vector3 randomizationForce;
        private float timeOFRandomization;
        private float speedVariation;
        private float originalMoveForce;
        private float originalSpeed;

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
        /// Randomize the speed of the animator and the movement.
        /// </summary>
        private void RandomizeSpeed()
        {
            speedVariation = MoveForce < originalMoveForce ? Random.Range(1f, 1.1f) : Random.Range(0.9f, 1f);
            MoveForce = originalMoveForce * speedVariation;

            if (m_Animator)
            {
                m_Animator.speed = originalSpeed * speedVariation;
            } else if (m_LegacyAnimation)
            {
                foreach (AnimationState state in m_LegacyAnimation)
                {
                    state.speed *= speedVariation;
                }
            }
        }

        void LateUpdate()
        {
            if (RB == null || RB.isKinematic || !IsSeeking) return;

            if (m_MaintainHeight)
            {
                MaintainHeight();
            }

            if (timeOFRandomization <= Time.timeSinceLevelLoad)
            {
                CalculateRandomizationForce();
                if (m_RandomizeSpeed)
                {
                    RandomizeSpeed();
                }
                timeOFRandomization = Time.timeSinceLevelLoad + m_RandomizationFrequency;
            }

            if (randomizationForce != Vector3.zero)
            {
                RB.AddForce(randomizationForce);
            }
        }

        /// <summary>
        /// Calculate a random force that will be added to the object to add variation to the flight.
        /// </summary>
        private void CalculateRandomizationForce()
        {
            if (!(m_RandomizeX || m_RandomizeX || m_RandomizeX)) return;

            float force = MoveForce * 0.1f;
            float x = 0;
            float y = 0;
            float z = 0;
            if (m_RandomizeX)
            {
                if (Random.value < 0.7)
                {
                    x = randomizationForce.x < 0 ? Random.Range(force, force * m_RandomizationFactor) : Random.Range(-force * m_RandomizationFactor, -force);
                }
            }
            if (m_RandomizeY)
            {
                if (Random.value < 0.7)
                {
                    y = randomizationForce.x < 0 ? Random.Range(force, force * m_RandomizationFactor) : Random.Range(-force * m_RandomizationFactor, -force);
                }
            }
            if (m_RandomizeZ)
            {
                if (Random.value < 0.7)
                {
                    z = randomizationForce.x < 0 ? Random.Range(force, force * m_RandomizationFactor) : Random.Range(-force * m_RandomizationFactor, -force);
                }
            }

            randomizationForce = new Vector3(x, y, z);
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

            if (terrainHeight > optimalHeight)
            {
                if (terrainHeight > maxHeight)
                {
                    RB.AddForce(RB.transform.up * -(MoveForce));
                }
                else
                {
                    RB.AddForce(RB.transform.up * -(MoveForce / 2));
                }
            }
            else if (terrainHeight < optimalHeight)
            {
                if (terrainHeight < minHeight)
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
