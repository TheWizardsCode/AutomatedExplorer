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
        Sensor WaypointSensor;
        [SerializeField, Tooltip("The sensor for detecting when the target waypoint has been reached.")]
        Sensor WaypoinArrivalSensor;
        [SerializeField, Tooltip("Strategy for selecting the next waypoint when one is not currently selected.")]
        SelectionStrategy strategy = SelectionStrategy.nearest;
        [SerializeField, Tooltip("Randomness in the selection of the next waypoint. The higher this value the more randomness there will be."), Range(0f, 1f)]
        float randomness = 0.2f;

        [Header("Steering")]
        [SerializeField, Tooltip("The waypoint prefab to use when getting the object to move out of a stuck state.")]
        WayPoint waypointPrefab;
        [SerializeField, Tooltip("The rig for steering towards the currently selected waypoint.")]
        SteeringRig Steering;
        [SerializeField, Tooltip("How long should the object be in the same place, if it has an existing waypoint destination, before it is assumed to be stuck. When stuck a new waypoint will be created a few meters away roughly behind the current position.")]
        float stuckDuration = 0.5f;
        [SerializeField, Tooltip("The tolerance to use when deciding if the item is stuck. If the object moves more than this distance in any direction in the `stuckDuration` then it will be considered to be moving.")]
        float stuckTolerance = 0.05f;
        [SerializeField, Tooltip("If set to true the sensor rig will be set to rotate towards the selected target waypoint.")]
        bool faceTowardsTarget = false;

        [Header("Arrival Behaviours")]
        [SerializeField, Tooltip("Should the camera take a photo when arriving at a sensor.")]
        bool m_TakePhotoOnArrival = true;

        [Header("Configuration")]
        [SerializeField, Tooltip("If set to true then the camera will look for waypoints on start, select one at random and the camera follow target will be teleported to it. If set to false, or if no waypoint is found, then the camera will not be moved at startup.")]
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
                WaypointSensor.Pulse();
                if (WaypointSensor.DetectedObjects.Count > 0)
                {
                    WayPoint start = WaypointSensor.DetectedObjects[Random.Range(0, WaypointSensor.DetectedObjects.Count)].GetComponent<WayPoint>();
                    if (start)
                    {
                        transform.position = start.transform.position;
                        transform.rotation = start.transform.rotation;
                    }
                }
            }

            timeToStuck = stuckDuration;
            sqrStuckTolerance = stuckTolerance * stuckTolerance;

#if PHOTOSESSION_PRESENT
            photoSession = GameObject.FindObjectOfType<PhotoSession>();
#endif
        }

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
                    if (m_currentWaypoint != null)
                    {
                        Steering.IgnoreList.Clear();
                        Steering.IgnoreList.Add(m_currentWaypoint.gameObject);
                        Steering.IgnoreList.Add(gameObject);
                        Steering.DestinationTransform = m_currentWaypoint.transform;

                        Steering.RotateTowardsTarget = faceTowardsTarget;
                        if (faceTowardsTarget)
                        {
                            Steering.FaceTowardsTransform = m_currentWaypoint.transform; 
                        }
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
                    timeToStuck = stuckDuration;
                }

                return timeToStuck <= 0;
            }
        }

        void Update()
        {
            if (currentWaypoint && IsStuck)
            {
                OnWaypointArrival();

                Vector3 pos = -transform.forward * Random.Range(Steering.StoppingDistance * 2.5f, Steering.StoppingDistance * 3.5f);
                pos += transform.right * Random.Range(-0.5f, -0.5f);
                pos += transform.up * Random.Range(0.5f, 1f);
                GameObject go = Instantiate(waypointPrefab.gameObject, transform.position + pos, Quaternion.identity);
                go.name = stuckWaypointName;
                currentWaypoint = go.GetComponent<WayPoint>();
                oldPosition = transform.position;
                timeToStuck = stuckDuration;

                WaypointSensor.Pulse();
            }

            if (!currentWaypoint)
            {
                SelectNewWaypoint();
            } 
            else if (WaypoinArrivalSensor.IsDetected(currentWaypoint.gameObject))
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
            WaypointSensor.Pulse();
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

        private void SelectNewWaypoint()
        {
            switch (strategy)
            {
                case SelectionStrategy.nearest:
                    currentWaypoint = GetWeightedNearestFromPointWithComponent(transform.position);
                    break;
                case SelectionStrategy.furthest:
                    currentWaypoint = GetWeightedFurthestFromPointWithComponent(transform.position);
                    break;
                case SelectionStrategy.random:
                    if (WaypointSensor.DetectedObjects.Count > 0)
                    {
                        currentWaypoint = WaypointSensor.DetectedObjects[Random.Range(0, WaypointSensor.DetectedObjects.Count)].GetComponent<WayPoint>();
                    }
                    break;
                default:
                    Debug.LogError("Unknown selection strategy: " + strategy);
                    break;
            }
        }

        private WayPoint GetWeightedFurthestFromPointWithComponent(Vector3 point)
        {
            WayPoint furthest = null;
            var furthestDistance = 0f;
            var gos = WaypointSensor.DetectedObjectsOrderedByDistance;
            for (int i = 0; i < gos.Count; i++)
            {
                WayPoint waypoint = gos[i].GetComponent<WayPoint>();
                if (waypoint == null) { continue; }

                float weight = waypoint.weight + Random.Range(0, randomness);
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
            var gos = WaypointSensor.DetectedObjectsOrderedByDistance;
            for (int i = 0; i < gos.Count; i++)
            {
                WayPoint waypoint = gos[i].GetComponent<WayPoint>();
                if (waypoint == null) { continue; }

                float weight = 1.001f - (waypoint.weight + Random.Range(0, randomness));

                float weightedDistance = Vector3.SqrMagnitude(waypoint.transform.position - point) * weight;
                if (nearest == null || weightedDistance < nearestDistance)
                {
                    nearest = waypoint;
                    nearestDistance = weightedDistance;
                }
            }
            return nearest;
        }
    }
}