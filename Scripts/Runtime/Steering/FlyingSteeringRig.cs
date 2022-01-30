using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WizardsCode.AI
{
    /// <summary>
    /// This rig provides a 3D navigation for flying creatures and objects. It detects opstacles on the expected path an flies over
    /// them or around them if it can. 
    /// </summary>
    public class FlyingSteeringRig : MonoBehaviour
    {
        [Header("Flight Controls")]
        [SerializeField, Tooltip("The rigid body that forces will be applied to to make the object fly (or fall in the case of gravity).")]
        internal Rigidbody rigidbody;
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
        [SerializeField, Tooltip("The body will attempt to avoid colliders on these layers.")]
        LayerMask m_AvoidanceLayers;

        [Header("Height Management")]
        [SerializeField, Tooltip("Try to keep the camera within a certain height range (true) or allow any height (false)." +
            " If this is true the following settings will confine the height.")]
        bool m_MaintainHeight = true;
        [SerializeField, Tooltip("The minimum height above the ground or nearest obstacle that this rig" +
            " should be. This is used as a validation check to ensure the object is not going below" +
            " the terrain or through a mesh obstacles in the scene.")]
        float m_MinHeight = 1.5f;
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

        [Header("Animation")]
        [SerializeField, Tooltip("The animation controller for updating speed accordingly.")]
        private Animator m_Animator;

        private float originalAnimationSpeed;
        private static Mesh cylinderCache;
        private Sensor[] sensorArray;

        /// <summary>
        /// The transform of the current destination the object is flying to.
        /// </summary>
        public Transform destination { get; set; }

        private void Awake()
        {
            ConfigureSensors();
        }

        private void ConfigureSensors()
        {
            List<Sensor> sensors = new List<Sensor>();
            sensors.Add(new Sensor(transform.forward, 3, maxSpeed * 1.5f, m_AvoidanceLayers)); // forward
            sensors.Add(new Sensor(transform.forward * 2 - transform.right, 1.5f, maxSpeed * 0.6f, m_AvoidanceLayers)); // forward/forward/left
            sensors.Add(new Sensor(transform.forward - transform.right, 1f, maxSpeed * 0.7f, m_AvoidanceLayers)); // forward/left
            sensors.Add(new Sensor(transform.forward - transform.right * 2, 0, maxSpeed * 0.8f, m_AvoidanceLayers)); // forward/left/left
            sensors.Add(new Sensor(transform.forward * 2 + transform.right, 1.5f, maxSpeed * 0.6f, m_AvoidanceLayers)); // forward/forward/right
            sensors.Add(new Sensor(transform.forward + transform.right, 1f, maxSpeed * 0.7f, m_AvoidanceLayers)); // forward/right
            sensors.Add(new Sensor(transform.forward + transform.right * 2, 0, maxSpeed * 0.8f, m_AvoidanceLayers)); // forward/right/right

            sensors.Add(new Sensor(-transform.right, 0, maxSpeed * 0.6f, m_AvoidanceLayers)); // left
            sensors.Add(new Sensor(transform.right, 0, maxSpeed * 0.6f, m_AvoidanceLayers)); // right

            sensors.Add(new Sensor(transform.up, 0, maxSpeed * 0.5f, m_AvoidanceLayers)); // up
            sensors.Add(new Sensor(transform.up + transform.forward, 0, maxSpeed * 0.7f, m_AvoidanceLayers)); // up/forward
            sensors.Add(new Sensor(transform.up - transform.right, 0, maxSpeed * 0.7f, m_AvoidanceLayers)); // up/left
            sensors.Add(new Sensor(transform.up + transform.right, 0, maxSpeed * 0.7f, m_AvoidanceLayers)); // up/right

            sensors.Add(new Sensor(-transform.up, 0, m_OptimalHeight, 0.2f, 0.5f, m_AvoidanceLayers)); // down
            sensors.Add(new Sensor(-transform.up - transform.right, 0, m_MinHeight, m_AvoidanceLayers)); // down/left
            //sensors.Add(new Sensor(-transform.up + transform.forward, 0, m_MinHeight, m_AvoidanceLayers)); // down/forward
            sensors.Add(new Sensor(-transform.up + transform.right, 0, m_MinHeight, m_AvoidanceLayers)); // down/right
            sensorArray = sensors.ToArray();
        }

        /// <summary>
        /// Get a direction that will push the object away from detected obstacles.
        /// </summary>
        Vector3 GetCalculateRepulsionDirection()
        {
            Vector3 strength = Vector3.zero;

            for (int i = 0; i < sensorArray.Length; i++)
            {
                strength += CheckRepulsionFrom(sensorArray[i]);
            }

            return strength * Mathf.Clamp(strength.magnitude, 0, m_MaxAvoidanceLength);
        }

        private Vector3 CheckRepulsionFrom(Sensor sensor)
        {
            //OPTIMIZATION: don't pulse every frame
            if (sensor.Pulse(this))
            {
                float obstructionRatio = Mathf.Pow(1f - (sensor.hit.distance / sensor.maxLength), 1f / m_AvoidanceSensitivity);
                return obstructionRatio * sensor.hit.normal;
            }
            else
            {
                return Vector3.zero;
            }
        }
        private void Start()
        {
            if (m_Animator)
            {
                originalAnimationSpeed = m_Animator.speed;
            }
        }
        private bool hasReachedDestination
        {
            get
            {
                return (rigidbody.transform.position - destination.position).magnitude <= m_ArrivalDistance;
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

            Vector3 desiredDirection = (destination.position - rigidbody.transform.position);
            Vector3 moveDirection = Vector3.zero; 
            if (desiredDirection.sqrMagnitude > 1)
            {
                moveDirection += desiredDirection.normalized;
            } else
            {
                moveDirection += desiredDirection;
            }
            
            Vector3 repulsion = GetCalculateRepulsionDirection();
            if (repulsion.sqrMagnitude > 0.01f)
            {
                moveDirection += repulsion.normalized;
            } else
            {
                moveDirection += repulsion * 100;
            }

            // Rotate towards the desired direction
            float angle;
            Vector3 axis;
            Quaternion desiredRotation = Quaternion.FromToRotation(rigidbody.transform.forward, moveDirection);
            desiredRotation.ToAngleAxis(out angle, out axis);
            angle = angle > 180f ? angle - 360f : angle;
            var torque = Mathf.Clamp(angle / 20f, -1f, 1f) * m_MaxTorque;
            rigidbody.AddTorque(axis * torque);

            // Keep the bottom facing down
            float z = rigidbody.rotation.eulerAngles.z;
            if (z > 180f) z -= 360f;
            float force = Mathf.Clamp(z / 45f, -1f, 1f) * m_MaxTorque;
            rigidbody.AddTorque(rigidbody.transform.forward * -force);

            SetAnimationParameters(moveDirection);

            // Forward force to add
            float forwardDotMove = Vector3.Dot(rigidbody.transform.forward, moveDirection.normalized);
            float forwardForce = Mathf.Lerp(m_MaxStrafeForce, m_MaxForwardForce, Mathf.Clamp01(forwardDotMove));

            // Vertical force to add
            float verticalDotMove = Vector3.Dot(Vector3.up, moveDirection.normalized);
            float verticalForce = 0;
            if (verticalDotMove > 0) {
                verticalForce = Mathf.Lerp(0, m_MaxVerticalForce, Mathf.Clamp01(verticalDotMove));
            } else
            {
                verticalForce = Mathf.Lerp(0, -m_MaxVerticalForce, Mathf.Clamp01(verticalDotMove));
            }

            // Add the forces
            rigidbody.AddForce((forwardForce * moveDirection.normalized) 
                + (verticalForce * rigidbody.transform.up));

            // Don't go over maximum speed
            rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxSpeed);
        }

        void SetAnimationParameters(Vector3 moveDirection)
        {
            if (!m_Animator) return;

            Quaternion q = rigidbody.rotation;
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
            float strafeVelocity = transform.InverseTransformDirection(rigidbody.velocity).normalized.x;
            float verticalVelocity = rigidbody.velocity.normalized.y;
            float forwardVelocity = transform.InverseTransformDirection(rigidbody.velocity).normalized.z;

            bool glide = false;
            if (forwardVelocity > 0.9)
            {
                glide = pitch > -0.2 && pitch < 0.2;
            }

            //OPTIMIZATION: Use Hash not string
            m_Animator.SetFloat("yaw", strafeVelocity);
            if (pitch <= 0) // up
            {
                m_Animator.SetFloat("pitch", pitch);
            } else // down
            {
                m_Animator.SetFloat("pitch", pitch);
            }
            m_Animator.SetFloat("roll", roll);
            m_Animator.SetFloat("verticalVelocity", verticalVelocity);
            m_Animator.SetFloat("forwardVelocity", forwardVelocity);
            m_Animator.SetBool("glide", glide);
        }

        private void OnDrawGizmosSelected()
        {
            // Target Direction
            if (destination != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rigidbody.transform.position, destination.position);
            }

            // Sensors
            if (sensorArray == null || sensorArray.Length == 0) ConfigureSensors();

            for (int i = 0; i < sensorArray.Length; i++)
            {
                Vector3 direction = rigidbody.transform.TransformDirection(sensorArray[i].sensorDirection);
                float length = Mathf.Lerp(0, sensorArray[i].maxLength, Mathf.Clamp01(rigidbody.velocity.magnitude / maxSpeed));

                Vector3 endPoint;
                if (sensorArray[i].hit.collider == null)
                {
                    endPoint = rigidbody.position + (direction * length);
                    Gizmos.color = Color.green;
                } else
                {
                    endPoint = rigidbody.position + (direction * sensorArray[i].hit.distance);
                    Gizmos.color = Color.red;
                }

                if (sensorArray[i].radius > 0)
                {
                    Gizmos.DrawLine(rigidbody.position, endPoint);
                    Gizmos.DrawWireSphere(endPoint, sensorArray[i].radius);
                }
                else
                {
                    Gizmos.DrawLine(rigidbody.position, endPoint);
                }
            }
        }
        private void OnValidate()
        {
            if (rigidbody == null) rigidbody = GetComponentInParent<Rigidbody>();
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

        public float maxAvoidanceLength = 1.2f;

        private LayerMask avoidanceLayers;

        public Sensor(Vector3 direction, float radius, float maxLength, LayerMask avoidanceLayers) {
            this.sensorDirection = direction;
            this.radius = radius;
            this.maxLength = maxLength;
            this.avoidanceLayers = avoidanceLayers;
        }

        public Sensor(Vector3 direction, float radius, float maxLength, float avoidanceSensitivity, float maxAvoidanceLength, LayerMask avoidanceLayers)
        {
            this.sensorDirection = direction;
            this.radius = radius;
            this.maxLength = maxLength;
            this.avoidanceSensitivity = avoidanceSensitivity;
            this.maxAvoidanceLength = maxAvoidanceLength;
            this.avoidanceLayers = avoidanceLayers;
        }

        /// <summary>
        /// Check for obstructions in the sensor. Once pulsed the value of `hit`
        /// will be the `RaycastHit` returned by the testing raycast.
        /// <paramref name="rig">The rig from which the sensors should be fired.</paramref>
        /// </summary>
        public bool Pulse(FlyingSteeringRig rig)
        {
            Vector3 direction = rig.rigidbody.transform.TransformDirection(this.sensorDirection);
            float length = Mathf.Lerp(0, maxLength, Mathf.Clamp01(rig.rigidbody.velocity.magnitude / rig.maxSpeed));

            Ray ray = new Ray(rig.rigidbody.transform.position, direction);
            if (radius > 0)
            {
                obstructionHit = Physics.SphereCast(ray, radius, out hit, length, avoidanceLayers);
            }
            else
            {
                obstructionHit = Physics.Raycast(ray, out hit, length, avoidanceLayers);
            }

            return obstructionHit;
        }
    }
}
