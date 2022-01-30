using UnityEngine;
using SensorToolkit;
#if PHOTOSESSION_PRESENT
using Rowlan.PhotoSession;
#endif


namespace WizardsCode.AI
{
    public class MoveToWaypoint : MonoBehaviour
    {
        enum SelectionStrategy { nearest, furthest, random };

        [Header("Sensors")]
        [SerializeField, Tooltip("The sensor for detecting waypoints.")]
        RangeSensor m_WaypointSensor;
        [SerializeField, Tooltip("The sensor for detecting when the target waypoint has been reached.")]
        TriggerSensor m_WaypoinArrivalSensor;
        [SerializeField, Tooltip("The waypoint prefab to use when getting the object to move out of a stuck state.")]
        WayPoint m_waypointPrefab;
        [SerializeField, Tooltip("Strategy for selecting the next waypoint when one is not currently selected.")]
        SelectionStrategy m_SelectionStrategy = SelectionStrategy.nearest;
        [SerializeField, Tooltip("Randomness in the selection of the next waypoint. The higher this value the more randomness there will be."), Range(0f, 1f)]
        float m_SelectionRandomness = 0.2f;
        /*
        [SerializeField, Tooltip("The radius that is used for the turning circle of the body. 0 means the body can turn on the spot. Note that a turning radius that is too close to the Waypoint Arrival sensor range will likely cause problems.")]
        [Range(0f, 100f)]
        float m_TurningRadius = 15;
        */

        [Header("Steering")]
        [SerializeField, Tooltip("The rig for steering towards the currently selected waypoint.")]
        FlyingSteeringRig m_SteeringRig;
        [SerializeField, Tooltip("How long should the object be in the same place, if it has an existing waypoint destination, before it is assumed to be stuck. When stuck a new waypoint will be created a few meters away roughly behind the current position.")]
        float m_StuckDuration = 0.5f;
        [SerializeField, Tooltip("The tolerance to use when deciding if the item is stuck. If the object moves more than this distance in any direction in the `stuckDuration` then it will be considered to be moving.")]
        float m_StuckTolerance = 0.05f;

        [Header("Arrival Behaviours")]
        [SerializeField, Tooltip("If you have [Photo Session](https://github.com/TheWizardsCode/PhotoSession) and this setting is true then a new photo will be taken each time the target reaches a waypoint. " +
            "Note if this is set to true but the Phot Session code is not present a warning will be displayed in the console.")]
        bool m_TakePhotoOnArrival = true;

        [Header("Configuration")]
        [SerializeField, Tooltip("If true the body will select a random waypoint as its starting position. " +
            "However, these waypoints must be present in the scene on start, spawned waypoints will not be considered. " +
            "If no waypoint is found then no change will be made to the start position.")]
        bool m_RandomizeStartingPosition = false;

        WayPoint m_currentWaypoint;
        private const string stuckWaypointName = "Waypoint to Get out of Stuck State";
        Vector3 oldPosition = Vector3.zero;
        float timeToStuck;
        float sqrStuckTolerance;
#if PHOTOSESSION_PRESENT
        PhotoSession photoSession;
#endif

        private void Start()
        {
            if (m_RandomizeStartingPosition)
            {
                m_WaypointSensor.Pulse();
                if (m_WaypointSensor.DetectedObjects.Count > 0)
                {
                    WayPoint start = m_WaypointSensor.DetectedObjects[Random.Range(0, m_WaypointSensor.DetectedObjects.Count)].GetComponent<WayPoint>();
                    if (start)
                    {
                        transform.position = start.transform.position;
                        transform.rotation = start.transform.rotation;
                    }
                }
            }

            timeToStuck = m_StuckDuration;
            sqrStuckTolerance = m_StuckTolerance * m_StuckTolerance;

#if PHOTOSESSION_PRESENT
            photoSession = GameObject.FindObjectOfType<PhotoSession>();
#endif
        }

        private WayPoint nextWaypoint;
        private WayPoint lastWaypoint;

        WayPoint currentWaypoint
        {
            get
            {
                return m_currentWaypoint;
            }

            set
            {
                if (m_currentWaypoint != value)
                {
                    m_currentWaypoint = value;
                    if (currentWaypoint != null && m_SteeringRig != null)
                    {
                        m_SteeringRig.destination = currentWaypoint.transform;
                    }
                }
            }
        }

        bool IsStuck
        {
            get
            {
                if (Vector3.SqrMagnitude(oldPosition - transform.position) < sqrStuckTolerance)
                {
                    timeToStuck -= Time.deltaTime;
                } else
                {
                    oldPosition = transform.position;
                    timeToStuck = m_StuckDuration;
                }

                return timeToStuck <= 0;
            }
        }

        void Update()
        {
            if (currentWaypoint && IsStuck)
            {
                OnWaypointArrival();

                Vector3 pos = -transform.forward * Random.Range(2.5f, 3.5f);
                pos += transform.right * Random.Range(-0.5f, -0.5f);
                pos += transform.up * Random.Range(0.5f, 1f);
                
                GameObject go = Instantiate(m_waypointPrefab.gameObject, transform.position + pos, Quaternion.identity);
                go.name = stuckWaypointName;
                currentWaypoint = go.GetComponent<WayPoint>();
                oldPosition = transform.position;
                timeToStuck = m_StuckDuration;

                m_WaypointSensor.Pulse();
            }

            if (!currentWaypoint)
            {
                SelectNewWaypoint();
            } 
            else if (m_WaypoinArrivalSensor.IsDetected(currentWaypoint.gameObject))
            {
                OnWaypointArrival();
            }
        }

        /// <summary>
        /// Called whenever a waypoint is reached. 
        /// 
        /// Remove the current waypoint as a target destination by either destroying it
        /// or disabling it depending on the kind of waypoint it is.
        /// 
        /// Optionally take a photo with the PhotoSession system.
        /// </summary>
        private void OnWaypointArrival()
        {
            //TODO if waypoint is an auto generated one to get out of a stuck space store the photo in a debugging space
            /*
            if (currentWaypoint.name.StartsWith(stuckWaypointName)) ...
            */

            if (currentWaypoint.reEnableWaitTime == 0)
            {
                Destroy(currentWaypoint.gameObject);
            }
            else
            {
                currentWaypoint.Disable();
            }
            m_WaypointSensor.Pulse();
            lastWaypoint = currentWaypoint;
            currentWaypoint = null;

            if (m_TakePhotoOnArrival)
            {
#if PHOTOSESSION_PRESENT
                if (photoSession)
                {
                    StartCoroutine(photoSession.CaptureScreenshot());
                }
                else
                {
                    Debug.LogWarning("Configured to take a photo when reaching a waypoint, but there is no PhotoSession in the scene.");
                }
#else
                Debug.LogWarning("You have configured the camera to take a photo on arrival at the waypoint. However, PhotoSession is not installed. See https://github.com/TheWizardsCode/AutomatedExplorer");
#endif
            }
        }

        /// <summary>
        /// Selects a new waypoint according to the defined strategy. If the selected waypoint is > 90 degrees to the left or right an interim
        /// waypoint will be generated to smooth the flight curve.
        /// </summary>
        private void SelectNewWaypoint()
        {
            if (nextWaypoint != null)
            {
                currentWaypoint = nextWaypoint;
                nextWaypoint = null;
            }
            else
            {
                if (lastWaypoint && lastWaypoint.NextWaypoint && lastWaypoint.NextWaypoint.IsEnabled)
                {
                    currentWaypoint = lastWaypoint.NextWaypoint;
                }
                else
                {
                    switch (m_SelectionStrategy)
                    {
                        case SelectionStrategy.nearest:
                            currentWaypoint = GetWeightedNearestFromPointWithComponent(transform.position);
                            break;
                        case SelectionStrategy.furthest:
                            currentWaypoint = GetWeightedFurthestFromPointWithComponent(transform.position);
                            break;
                        case SelectionStrategy.random:
                            if (m_WaypointSensor.DetectedObjects.Count > 0)
                            {
                                currentWaypoint = m_WaypointSensor.DetectedObjects[Random.Range(0, m_WaypointSensor.DetectedObjects.Count)].GetComponent<WayPoint>();
                            }
                            break;
                        default:
                            Debug.LogError("Unknown selection strategy: " + m_SelectionStrategy);
                            break;
                    }
                }
            }

            if (currentWaypoint == null) return;

            /* create a turning circle if the next waypoint will cause the body to flip
            Transform root = transform.root;
            Vector3 heading = currentWaypoint.transform.position - root.position;
            float dot = Vector3.Dot(heading.normalized, root.forward.normalized);
            if ( dot < 0.3)
            {
                nextWaypoint = currentWaypoint;
                currentWaypoint = Instantiate(m_waypointPrefab);
                currentWaypoint.name = $"Turning waypoint (heading to {nextWaypoint}).";
                currentWaypoint.reEnableWaitTime = 0;

                //TODO: Don't always turn right, decide the least obstructed path and go that way
                Vector3 pos = transform.position + (root.right * m_TurningRadius) + (root.forward * m_TurningRadius) + (root.up * 2);
                //FIXME: this needs to be set at a height above the terrain as there is a danger that it will be set into a slope and be inacessible
                pos.y = root.position.y;
                currentWaypoint.transform.position = pos;
            }
            */
        }

        private WayPoint GetWeightedFurthestFromPointWithComponent(Vector3 point)
        {
            WayPoint furthest = null;
            var furthestDistance = 0f;
            var gos = m_WaypointSensor.DetectedObjectsOrderedByDistance;
            for (int i = 0; i < gos.Count; i++)
            {
                WayPoint waypoint = gos[i].GetComponent<WayPoint>();
                if (waypoint == null) { continue; }

                float weight = waypoint.weight + Random.Range(0, m_SelectionRandomness);
                var weightedDistance = Vector3.SqrMagnitude(waypoint.transform.position - point) * weight;
                if (furthest == null || weightedDistance > furthestDistance)
                {
                    furthest = waypoint;
                    furthestDistance = weightedDistance;
                }
            }
            return furthest;
        }

        private WayPoint GetWeightedNearestFromPointWithComponent(Vector3 point)
        {
            WayPoint nearest = null;
            var nearestDistance = 0f;
            var gos = m_WaypointSensor.DetectedObjectsOrderedByDistance;
            for (int i = 0; i < gos.Count; i++)
            {
                WayPoint waypoint = gos[i].GetComponent<WayPoint>();
                if (waypoint == null) { continue; }

                float weight = 1.001f - (waypoint.weight + Random.Range(0, m_SelectionRandomness));

                float weightedDistance = Vector3.SqrMagnitude(waypoint.transform.position - point) * weight;
                if (nearest == null || weightedDistance < nearestDistance)
                {
                    nearest = waypoint;
                    nearestDistance = weightedDistance;
                }
            }
            return nearest;
        }

        private void OnValidate()
        {
            if (m_SteeringRig == null) m_SteeringRig = GetComponentInChildren<FlyingSteeringRig>();

            if (m_WaypointSensor == null) m_WaypointSensor = GetComponentInChildren<RangeSensor>();

            if (m_WaypoinArrivalSensor == null) m_WaypoinArrivalSensor = GetComponentInChildren<TriggerSensor>();
        }
    }
}