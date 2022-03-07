using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static WizardsCode.AI.Sensor;
using WizardsCode.Character;
using System;
using Random = UnityEngine.Random;

namespace WizardsCode.AI
{
    /// <summary>
    /// This rig provides a 3D navigation for flying creatures and objects. It detects opstacles on the expected path an flies over
    /// them or around them if it can. 
    /// </summary>
    public class FlyingSteeringRig : AnimatorActorController
    {
        #region Inspector Parameters
        [Header("Flight Controls")]
        [SerializeField, Tooltip("The rigid body that forces will be applied to to make the object fly (or fall in the case of gravity).")]
        internal Rigidbody rb;
        [SerializeField, Tooltip("Once the object is within this distance of the destination it is considered to have reached the destination.")]
        float m_ArrivalDistance = 1;
        [SerializeField, Tooltip("The maximum toque (turning force) that can be applied to this body. Higher values will result in tighter turns.")]
        float m_MaxTorque = 4;
        [SerializeField, Tooltip("The maximum strafe (sideways and backwards) force that can be applied to this body. Larger values will allow the body to slide sideways and backwards. A value of 0 will result in the body only moving forward, thus requiring a turn to move sideways or backwards from the current position.")]
        float m_MaxStrafeForce = 5;
        [SerializeField, Tooltip("The maximum vertical force that can be applied to this body. Larger values will result in faster climbs and dives.")]
        float m_MaxVerticalForce = 8;
        [SerializeField, Tooltip("The maximum forward force that can be applied to this body. Larger values will result in faster movement.")]
        float m_MaxForwardForce = 10;
        [SerializeField, Tooltip("The maximum forward velocity for this body.")]
        internal float maxSpeed = 8;
        [SerializeField, Tooltip("The avoidance strength of the body. A higher setting will result in the body moving rapidly away from obstructions, while a lower setting will result in slower, smoother movements.")]
        [Range(0.1f, 5f)]
        float m_AvoidanceStrength = 2f;
        [SerializeField, Tooltip("The body will attempt to avoid colliders on these layers.")]
        LayerMask m_AvoidanceLayers;

        [Header("Height Management")]
        [SerializeField, Tooltip("The minimum height above the ground or nearest obstacle that this rig" +
            " should be. The body will always try to get higher if it drops below this height.")]
        float m_MinHeight = 1.5f;
        [SerializeField, Tooltip("Automatically land when below the landing height?")]
        bool m_AutoLand = false;
        [SerializeField, Tooltip("The height above the ground or nearest obstacle that will cause this rig" +
            " automatically land.")]
        float m_LandingHeight = 0.5f;
        [SerializeField, Tooltip("The minimum amount of time to spend on the ground after landing. The Dragon will not take off again until this amount of time (in seconds) has passed.")]
        float m_MinTimeOnGround = 5;
        [SerializeField, Tooltip("The height at which the body is considered to be grounded. Theoretically this should be zero," +
            " however it will often be higher due to the models structure.")]
        float m_GroundedHeight = 0.08f;
        [SerializeField, Tooltip("The maximum height above the ground or nearest obstacle that this rig" +
            " should be. This is used as a validation check to ensure the object is not going too" +
            " far above the terrain or mesh obstacles in the scene. The camera may go above this height" +
            " but if it does it will have forces placed upon it to move down.")]
        float m_MaxHeight = 10f;
        [SerializeField, Tooltip("The optimal height above the ground or nearest obstacle that this rig" +
            " should be. The camera is allowed to move from this height but it will recieve gentle forces" +
            " encourage it to return to this height.")]
        float m_OptimalHeight = 2f;
        [SerializeField, Tooltip("The maximum climb angle that this body can gain vertical height with. 90 is vertically up.")]
        [Range(0, 90)]
        float m_MaxClimbAngle = 35;
        [SerializeField, Tooltip("The maximum dive angle that this body can lose vertical height with in a controlled way. 90 is stright down.")]
        [Range(0, 90)]
        float m_MaxDiveAngle = 75;
        #endregion

        private float originalAnimationSpeed;
        private Sensor[] sensorArray;
        int nextSensorToPulse = 0;
        private float approachDistanceSqr;
        private float prepareToLandDistanceSqr;
        private float landingDistanceSqr;

        /// <summary>
        /// The transform of the current destination the object is flying to.
        /// </summary>
        public Transform destination { get; set; }



        /// <summary>
        /// Returns true if the body is in an idle state.
        /// </summary>
        public override bool isIdle
        {
            get
            {
                if (m_Animator)
                {
                    return m_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimationHash.idleState;
                }
                else
                {
                    return false;
                }
            }
        }
        
        
        /// <summary>
         /// Returns true if the body is currently on the ground.
         /// </summary>
        public bool isGrounded
        {
            get
            {
                if (m_Animator)
                {
                    return m_Animator.GetBool(AnimationHash.isGrounded);
                } else
                {
                    return false;
                }
            }
            set
            {
                if (m_Animator && isGrounded != value)
                {
                    m_Agent.enabled = value;
                    if (value)
                    {
                        m_Agent.Warp(transform.position);
                    }
                    m_Animator.SetBool(AnimationHash.isGrounded, value);
                }
            }
        }

        private float m_TimeOfLastLanding;
        private float previousForwardForce;
        private float previousVerticalForce;

        /// <summary>
        /// Returns true if the body is in the process of taking off.
        /// </summary>
        public bool isTakingOff
        {
            get
            {
                if (m_Animator)
                {
                    return m_Animator.GetBool(AnimationHash.isTakingOff);
                } else
                {
                    return false;
                }
            }
            set
            {
                if (m_Animator)
                {
                    m_Animator.SetBool(AnimationHash.isTakingOff, value);
                }
            }
        }

        /// <summary>
        /// Returns true if the body is in the process of landing.
        /// </summary>
        public bool isLanding
        {
            get
            {
                if (m_Animator)
                {
                    return m_Animator.GetBool(AnimationHash.isLanding);
                } else
                {
                    return false;
                }
            }
            set
            {
                if (m_Animator)
                {
                    m_Animator.SetBool(AnimationHash.isLanding, value);
                }
            }
        }

        public bool isFlying
        {
            get 
            {
                return m_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimationHash.flightState
                    || m_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimationHash.glideState;
            }
        }

        /// <summary>
        /// Returns the current height of the body above the nearest obstacle below it.
        /// </summary>
        public float height
        {
            get
            {
                return GetObstructionHeight(rb.transform.position);
            }
        }

        private Transform cachedDestination;
        private float cachedDestinationHeight;
        private Vector3 oldRepulsion;

        /// <summary>
        /// Returns the height of the current destination relative to the nearest obstacle below it.
        /// </summary>
        public float DestinationHeight
        {
            get
            {
                if (cachedDestination != destination)
                {
                    cachedDestination = destination;
                    cachedDestinationHeight = GetObstructionHeight(destination.position);
                }
                return cachedDestinationHeight;
            }
        }

        /// <summary>
        /// Get the ground height below a given point, or the heighest point of any obstruction below that point.
        /// </summary>
        /// <param name="point">The heighest obstruction point below the supplied point or 0 if no obstruction is seen.</param>
        /// <returns></returns>
        public float GetObstructionHeight(Vector3 point)
        {
            //OPTIMIZE: cache the result of this for a few frames at a time since it is potentially called multiple times a frame
            RaycastHit hit;
            if (Physics.Raycast(point, Vector3.down, out hit, Mathf.Infinity))
            {
                return hit.distance;
            }
            else
            {
                return 0;
            }
        }
        protected override void Awake()
        {
            base.Awake();

            ConfigureSensors();
            
            float landingDistance = m_ArrivalDistance * 3f;
            landingDistanceSqr = landingDistance * landingDistance;
            
            float prepareToLandDistance = landingDistance * 4f;
            prepareToLandDistanceSqr = prepareToLandDistance * prepareToLandDistance;

            float approachDistance = landingDistance * 20f;
            approachDistanceSqr = approachDistance * approachDistance;
        }

        protected override void Update()
        {
            if (isGrounded) base.Update();    
        }

        public override void MoveTo(Transform destination)
        {
            WayPoint wp = destination.GetComponent<WayPoint>();
            if (wp)
            {
                this.destination = destination;
            } else
            {
                base.MoveTo(destination);
            }
        }

        private void ConfigureSensors()
        {
            float forwardSensitivity = 0.8f;
            float sidewaysSensitivity = 0.2f;
            float upSensitivity = 1f;
            float downSensitivity = 1f;

            List<Sensor> sensors = new List<Sensor>();
            sensors.Add(new Sensor(transform.forward, true, 3, maxSpeed * 1.5f, forwardSensitivity * 1f, m_AvoidanceLayers)); // near forward
            sensors.Add(new Sensor(transform.forward * 2 - transform.right, true, 1.5f, maxSpeed * 0.6f, forwardSensitivity * sidewaysSensitivity * 0.6f, m_AvoidanceLayers)); // forward/forward/left
            sensors.Add(new Sensor(transform.forward - transform.right, true, 1f, maxSpeed * 0.7f, forwardSensitivity * sidewaysSensitivity * 0.4f, m_AvoidanceLayers)); // forward/left
            sensors.Add(new Sensor(transform.forward - transform.right * 2, true, 0, maxSpeed * 0.8f, forwardSensitivity * sidewaysSensitivity * 0.2f, m_AvoidanceLayers)); // forward/left/left
            sensors.Add(new Sensor(transform.forward * 2 + transform.right, true, 1.5f, maxSpeed * 0.9f, forwardSensitivity * sidewaysSensitivity * 0.6f, m_AvoidanceLayers));  ; // forward/forward/right
            sensors.Add(new Sensor(transform.forward + transform.right, true, 1f, maxSpeed * 0.8f, forwardSensitivity * sidewaysSensitivity * 0.4f, m_AvoidanceLayers)); // forward/right
            sensors.Add(new Sensor(transform.forward + transform.right * 2, true, 0, maxSpeed * 0.7f, forwardSensitivity * sidewaysSensitivity * 0.2f, m_AvoidanceLayers)); // forward/right/right

            sensors.Add(new Sensor(-transform.right, true, 0, maxSpeed * 0.6f, sidewaysSensitivity * 0.3f, m_AvoidanceLayers)); // left
            sensors.Add(new Sensor(transform.right, true, 0, maxSpeed * 0.6f, sidewaysSensitivity * 0.3f, m_AvoidanceLayers)); // right

            sensors.Add(new Sensor(transform.up, false, 0, maxSpeed * 0.5f, upSensitivity * 0.6f, m_AvoidanceLayers)); // up
            sensors.Add(new Sensor(transform.up + transform.forward, false, 0, maxSpeed * 1.5f, upSensitivity * 0.6f, m_AvoidanceLayers)); // up/forward
            sensors.Add(new Sensor(transform.up + transform.forward * 2, false, 0, maxSpeed * 1.5f, upSensitivity * 0.4f, m_AvoidanceLayers)); // up/forward/forward
            sensors.Add(new Sensor(transform.up - transform.right, false, 0, maxSpeed * 0.7f, upSensitivity * sidewaysSensitivity * 0.6f, m_AvoidanceLayers)); // up/left
            sensors.Add(new Sensor(transform.up + transform.right, false, 0, maxSpeed * 0.7f, upSensitivity * sidewaysSensitivity * 0.6f, m_AvoidanceLayers)); // up/right

            sensors.Add(new Sensor(-transform.up, false, 0, m_OptimalHeight * 2, downSensitivity * 0.1f, m_AvoidanceLayers)); // down
            sensors.Add(new Sensor(-transform.up - transform.right, false, 0, m_MinHeight * 0.5f, downSensitivity * sidewaysSensitivity * 0.3f, m_AvoidanceLayers)); // down/left
            sensors.Add(new Sensor(-transform.up + transform.forward, false, 0, m_MinHeight, downSensitivity * 0.4f, m_AvoidanceLayers)); // down/forward
            sensors.Add(new Sensor(-transform.up + transform.forward * 2, false, 0, m_MinHeight * 2, downSensitivity * 0.3f, m_AvoidanceLayers)); // down/forward/forward
            sensors.Add(new Sensor(-transform.up + transform.right, false, 0, m_MinHeight * 0.7f, downSensitivity * sidewaysSensitivity * 0.3f, m_AvoidanceLayers)); // down/right

            sensorArray = sensors.ToArray();
        }

        /// <summary>
        /// Get a direction that will push the object away from detected obstacles.
        /// 
        /// <param name="updateAllSensors">If true then all sensors will be updated immediately. Use this if you need very accurate avoidance. Othersie leave as the default - false.</param>
        /// </summary>
        Vector3 GetRepulsionDirection(bool updateAllSensors = false)
        {
            if (isLanding) return Vector3.zero;

            if (!updateAllSensors)
            {
                sensorArray[nextSensorToPulse].Pulse(this);
                nextSensorToPulse++;
                if (nextSensorToPulse == sensorArray.Length) nextSensorToPulse = 0;
            }

            Vector3 strength = Vector3.zero;
            for (int i = 0; i < sensorArray.Length; i++)
            {
                if (updateAllSensors)
                {
                    sensorArray[i].Pulse(this);
                }

                if (sensorArray[i].sensorDirection.y < 0
                    && Vector3.SqrMagnitude(rb.transform.position - destination.position) < approachDistanceSqr
                    && destination.position.y < rb.transform.position.y)
                {
                    // skip downward sensors when approaching a destination that is below the current position
                    // as they would result in an undesirable push back up when we are trying to go down.
                    // Forward and up sensors can still result in a small upward force depending on 
                    // the rotation of the body. This serves to force a levelling out as obstructions get nearer.
                    continue;
                }
                strength += sensorArray[i].lastObstructionRatio;
            }

            Vector3 repulsion = strength * Mathf.Clamp(strength.magnitude, 0, m_AvoidanceStrength);
            //Debug.Log($"Repulsion direction is {repulsion}");
            return repulsion;
        }
        void Start()
        {
            if (m_Animator)
            {
                originalAnimationSpeed = m_Animator.speed;
            }

            if (height <= m_GroundedHeight) {
                isGrounded = true;
            } else
            {
                isTakingOff = true;
            }
            isLanding = false;
        }

        /// <summary>
        /// Test to see if the body has reached its destination. If no destination is currently set this will
        /// always return true.
        /// </summary>
        internal bool hasReachedDestination
        {
            get
            {
                if (destination == null) return true;

                return (rb.transform.position - destination.position).magnitude <= m_ArrivalDistance;
            }
        }
        protected virtual void FixedUpdate()
        {
            if (destination == null) return;

            if (hasReachedDestination)
            {
                destination = null;
                return;
            }

            if (isGrounded)
            {
                // FIXME: need height of destination from the ground not absolute height
                if (destination.position.y >= height + m_MinHeight && m_TimeOfLastLanding + m_MinTimeOnGround < Time.timeSinceLevelLoad)
                {
                    TakeOff();
                }
                else
                {
                    GroundMovement();
                }
            } 
            else if (isTakingOff)
            {
                // Take off is handled in the animation controller, so once we are flying we are good.
                isTakingOff = height < m_LandingHeight;
                FlightPhysics();
            }
            else if (isLanding)
            {
                LandingPhysics();
            } else 
            {
                FlightPhysics();
            }

            SetAnimationParameters();
        }

        private void LandingPhysics()
        {
            rb.freezeRotation = true;

            if (height < m_GroundedHeight)
            {
                isLanding = false;
                isGrounded = true;
                //m_Agent.enabled = true;
                //m_Agent.Warp(rb.transform.position);
                m_TimeOfLastLanding = Time.timeSinceLevelLoad;
                return;
            }

            Vector3 desiredDirection;
            Vector3 interimDestination = GetInterimPointAdjustedForApproachHeight();
            
            desiredDirection = (interimDestination - rb.position);
            Vector3 moveDirection = Vector3.zero;
            if (desiredDirection.sqrMagnitude > 1)
            {
                moveDirection += desiredDirection.normalized;
            }
            else
            {
                moveDirection += desiredDirection;
            }

            //OPTIMIZATION: is it cheaper to create a single force and add it?
            float z = rb.rotation.eulerAngles.z;
            if (z > 180f) z -= 360f;
            float force = Mathf.Clamp(z / 45f, -1f, 1f) * m_MaxTorque;
            rb.AddTorque(rb.transform.forward * -force);

            float x = rb.rotation.eulerAngles.x;
            if (x > 180f) z -= 360f;
            force = Mathf.Clamp(z / 45f, -1f, 1f) * m_MaxTorque;
            rb.AddTorque(rb.transform.right * -force);

            ApplyForces(moveDirection);

            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed / 10);
        }

        private void FlightPhysics()
        {
            rb.freezeRotation = false;

            Vector3 desiredDirection;
            Vector3 interimDestination = GetInterimPointAdjustedForApproachHeight();

            desiredDirection = (interimDestination - rb.position);
            Vector3 moveDirection = Vector3.zero;
            if (desiredDirection.sqrMagnitude > 1)
            {
                moveDirection += desiredDirection.normalized;
            }
            else
            {
                moveDirection += desiredDirection;
            }

            Vector3 repulsion = Vector3.Lerp(oldRepulsion, GetRepulsionDirection(), Time.deltaTime);
            oldRepulsion = repulsion;
            if (repulsion.sqrMagnitude > 0.01f)
            {
                moveDirection += repulsion.normalized;
            }
            else
            {
                moveDirection += repulsion * 10;
            }

            // Rotate towards the desired direction
            float angle;
            Vector3 axis;
            Quaternion desiredRotation = Quaternion.FromToRotation(rb.transform.forward, moveDirection);
            desiredRotation.ToAngleAxis(out angle, out axis);
            angle = angle > 180f ? angle - 360f : angle;
            var torque = Mathf.Clamp(angle, -1f, 1f) * m_MaxTorque;
            rb.AddTorque(axis * torque);

            // Keep the bottom facing down
            float z = rb.rotation.eulerAngles.z;
            if (z > 180f) z -= 360f;
            float force = Mathf.Clamp(z / 120f, -1f, 1f) * m_MaxTorque;
            rb.AddTorque(rb.transform.forward * -force);

            //Debug.Log($"Move direction is {moveDirection}, normalized is {moveDirection.normalized}");

            ApplyForces(moveDirection);
        }

        /// <summary>
        /// Get an interim point that is in the same x,z space as the destination but adjusted for height
        /// to provide a good approach for landing or leisurely flight.
        /// <returns></returns>
        private Vector3 GetInterimPointAdjustedForApproachHeight()
        {
            Vector3 interimDestination = destination.position;
            float desiredDirectionSqrMagnitude = (destination.position - rb.position).sqrMagnitude;
            if (desiredDirectionSqrMagnitude > approachDistanceSqr)
            {
                if (DestinationHeight < m_OptimalHeight)
                {
                    interimDestination.y += m_OptimalHeight;
                }
            }
            else if (m_AutoLand && desiredDirectionSqrMagnitude > prepareToLandDistanceSqr)
            {
                if (DestinationHeight < m_MinHeight)
                {
                    interimDestination.y += m_MinHeight;
                }
            }
            else if (m_AutoLand && desiredDirectionSqrMagnitude > landingDistanceSqr)
            {
                if (DestinationHeight < m_LandingHeight)
                {
                    interimDestination.y += m_LandingHeight;
                }
            }
            else if (m_AutoLand && DestinationHeight < m_LandingHeight)
            {
                interimDestination.y = Mathf.Lerp(transform.position.y, destination.position.y, desiredDirectionSqrMagnitude / m_LandingHeight);
                isLanding = true;
            }

            return interimDestination;
        }

        private void ApplyForces(Vector3 moveDirection)
        {
            // Forward force to add
            float forwardDotMove = Vector3.Dot(rb.transform.forward, moveDirection.normalized);
            float forwardForce = Mathf.Lerp(m_MaxStrafeForce, m_MaxForwardForce, Mathf.Clamp01(forwardDotMove));

            // Vertical force to add
            float verticalForce = Mathf.Lerp(0, m_MaxVerticalForce, Mathf.Clamp01(moveDirection.normalized.y));

            // Add the forces
            //Debug.Log($"Forward force is {forwardForce}, vertical force is {verticalForce}.");
            rb.AddForce((Mathf.Lerp(previousForwardForce, forwardForce, Time.deltaTime) * moveDirection.normalized)
                + (Mathf.Lerp(previousVerticalForce, verticalForce, Time.deltaTime) * rb.transform.up));

            // Don't go over maximum speed
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);

            previousForwardForce = forwardForce;
            previousVerticalForce = verticalForce;
        }

        /// <summary>
        /// Apply rules to cause the body to take off from a grounded position
        /// </summary>
        private void TakeOff()
        {
            Vector3 repulsion = GetRepulsionDirection(true);
            if (repulsion.y < 0) { 
                return; 
            }

            rb.freezeRotation = false;

            Vector3 moveDirection = Vector3.zero;
            moveDirection = new Vector3(0, 1000, 1000);
            isGrounded = false;
            isTakingOff = true;

            ApplyForces(moveDirection);
        }

        /// <summary>
        /// Apply on the ground movement rules.
        /// </summary>
        private void GroundMovement()
        {
            rb.freezeRotation = true;

            if (Vector3.SqrMagnitude(rb.transform.position - destination.position) > approachDistanceSqr 
                && m_TimeOfLastLanding + m_MinTimeOnGround < Time.timeSinceLevelLoad 
                && Random.value <= 0.01) {
                TakeOff();
                return;
            }

            if (m_Agent.destination != destination.position)
            {
                m_Agent.SetDestination(destination.position);
            }
        }
        void SetAnimationParameters()
        {
            if (!m_Animator) return;

            Quaternion q = rb.rotation;
            float rollRad = Mathf.Atan2(2 * q.y * q.w - 2 * q.x * q.z, 1 - 2 * q.y * q.y - 2 * q.z * q.z);
            float pitchRad = Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
            float yawRad = Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);

            float pitch;
            if (pitchRad <= 0) // up
            {
                pitch = (Mathf.Rad2Deg * -pitchRad) / m_MaxClimbAngle;
            } else
            {
                pitch = (Mathf.Rad2Deg * -pitchRad) / m_MaxDiveAngle;
            }
            float yaw = yawRad / 1.52f;
            float roll = rollRad / 3.14f;
            float strafeVelocity = transform.InverseTransformDirection(rb.velocity).normalized.x;
            float verticalVelocity = rb.velocity.normalized.y;
            float forwardVelocity = transform.InverseTransformDirection(rb.velocity).normalized.z;

            bool glide = false;
            if (forwardVelocity > 0.9 && height > m_MinHeight)
            {
                glide = pitch > -0.2 && pitch < 0.2;
            }

            m_Animator.SetFloat(AnimationHash.yaw, strafeVelocity);
            if (pitch <= 0) // up
            {
                m_Animator.SetFloat(AnimationHash.pitch, pitch);
            } else // down
            {
                m_Animator.SetFloat(AnimationHash.pitch, pitch);
            }
            m_Animator.SetFloat(AnimationHash.roll, roll);
            m_Animator.SetFloat(AnimationHash.verticalVelocity, verticalVelocity);
            m_Animator.SetFloat(AnimationHash.forwardVelocity, forwardVelocity);
            m_Animator.SetBool(AnimationHash.glide, glide);

            m_Animator.SetFloat(AnimationHash.speed, Mathf.Clamp((rb.velocity.magnitude / m_MaxSpeed) * 1.6f, 0.9f, 1.8f));
        }

        private void OnDrawGizmosSelected()
        {
            if (destination == null) return;
            if (isGrounded) return;

            // Target Direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rb.transform.position, destination.position);

            // Sensors
            if (sensorArray == null || sensorArray.Length == 0) ConfigureSensors();

            if (!isLanding)
            {
                for (int i = 0; i < sensorArray.Length; i++)
                {
                    if (sensorArray[i].sensorDirection.y < 0
                        && Vector3.SqrMagnitude(rb.transform.position - destination.position) < approachDistanceSqr
                        && destination.position.y < rb.transform.position.y)
                    {
                        // skip downward sensors when approaching a destination that is below the current position
                        // as they would result in an undesirable push back up when we are trying to go down.
                        // Forward and up sensors can still result in a small upward force depending on 
                        // the rotation of the body. This serves to force a levelling out as obstructions get nearer.
                        continue;
                    }

                    Gizmos.color = Color.white;
                    Vector3 interimPoint = GetInterimPointAdjustedForApproachHeight();
                    Gizmos.DrawSphere(interimPoint, 0.5f);
                    Gizmos.DrawLine(rb.transform.position, interimPoint);

                    Vector3 direction = rb.transform.TransformDirection(sensorArray[i].sensorDirection);
                    float length = Mathf.Lerp(0, sensorArray[i].maxLength, Mathf.Clamp01(rb.velocity.magnitude / maxSpeed));

                    Vector3 endPoint;
                    if (sensorArray[i].hit.collider == null)
                    {
                        endPoint = rb.position + (direction * length);
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        endPoint = rb.position + (direction * sensorArray[i].hit.distance);
                        Gizmos.color = Color.red;
                    }

                    if (sensorArray[i].radius > 0)
                    {
                        float currentRadius = Mathf.Lerp(0, sensorArray[i].radius, Mathf.Clamp01(rb.velocity.magnitude / maxSpeed));
                        Gizmos.DrawLine(rb.position, endPoint);
                        Gizmos.DrawWireSphere(endPoint, currentRadius);
                    }
                    else
                    {
                        Gizmos.DrawLine(rb.position, endPoint);
                    }
                }
            }
        }
        private void OnValidate()
        {
            if (rb == null) rb = GetComponentInParent<Rigidbody>();
        }
    }

    class Sensor
    {
        private bool obstructionHit = false;

        /// <summary>
        /// The RaycastHit or null depending on whether the sensor hit anything on the last pulse
        /// </summary>
        public RaycastHit hit;
        
        /// <summary>
        /// Direction relative to `transform.forward`.
        /// </summary>
        public Vector3 sensorDirection { get; set; }
        public float radius { get; set; }
        public float maxLength { get; set; }

        public float avoidanceSensitivity = 0.6f;

        public bool rotateWithRigidbody = true;

        private LayerMask avoidanceLayers;
        internal Vector3 lastObstructionRatio
        {
            get
            {
                if (obstructionHit)
                {
                    float obstructionRatio = Mathf.Pow(1f - (hit.distance / maxLength), 1f / avoidanceSensitivity);
                    return obstructionRatio * hit.normal;
                }
                else
                {
                    return Vector3.zero;
                }
            }
        }
        public Sensor(Vector3 direction, bool rotateWithRigidbody, float radius, float maxLength, float avoidanceSensitivity, LayerMask avoidanceLayers)
        {
            this.sensorDirection = direction;
            this.rotateWithRigidbody = rotateWithRigidbody;
            this.radius = radius;
            this.maxLength = maxLength;
            this.avoidanceSensitivity = avoidanceSensitivity;
            this.avoidanceLayers = avoidanceLayers;
        }

        /// <summary>
        /// Check for obstructions in the sensor. Once pulsed the value of `hit`
        /// will be the `RaycastHit` returned by the testing raycast.
        /// <paramref name="rig">The rig from which the sensors should be fired.</paramref>
        /// </summary>
        public bool Pulse(FlyingSteeringRig rig)
        {
            Vector3 direction;
            if (rotateWithRigidbody)
            {
                direction = rig.rb.transform.TransformDirection(this.sensorDirection);
            } else
            {
                direction = sensorDirection;
            }
            float length = Mathf.Lerp(0, maxLength, Mathf.Clamp01(rig.rb.velocity.magnitude / rig.maxSpeed));

            Ray ray = new Ray(rig.rb.transform.position, direction);
            if (radius > 0)
            {
                float currentRadius = Mathf.Lerp(0, radius, Mathf.Clamp01(rig.rb.velocity.magnitude / rig.maxSpeed));
                obstructionHit = Physics.SphereCast(ray, currentRadius, out hit, length, avoidanceLayers);
            }
            else
            {
                obstructionHit = Physics.Raycast(ray, out hit, length, avoidanceLayers);
            }

            return obstructionHit;
        }

        internal static class AnimationHash
        {
            #region parameters
            internal static int speed = Animator.StringToHash("speed");

            internal static int isGrounded = Animator.StringToHash("isGrounded");
            internal static int isTakingOff= Animator.StringToHash("isTakingOff");
            internal static int isLanding = Animator.StringToHash("isLanding");

            internal static int roll = Animator.StringToHash("roll");
            internal static int pitch = Animator.StringToHash("pitch");
            internal static int yaw = Animator.StringToHash("yaw");

            internal static int verticalVelocity = Animator.StringToHash("verticalVelocity");
            internal static int forwardVelocity = Animator.StringToHash("forwardVelocity");

            internal static int glide = Animator.StringToHash("glide");
            #endregion

            #region states
            internal static int idleState = Animator.StringToHash("Idle");
            internal static int takeOffState = Animator.StringToHash("Take Off");
            internal static int flightState = Animator.StringToHash("Flight");
            internal static int glideState = Animator.StringToHash("Glide");
            #endregion
        }
    }
}
