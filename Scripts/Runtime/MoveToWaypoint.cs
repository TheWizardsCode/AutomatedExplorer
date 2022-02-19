using UnityEngine;
using System.Collections.Generic;
#if PHOTOSESSION_PRESENT
using Rowlan.PhotoSession;
#endif


namespace WizardsCode.AI
{
    public class MoveToWaypoint : MonoBehaviour
    {
        enum SelectionStrategy { nearest, furthest, random };

        [Header("Sensors")]
        [SerializeField, Tooltip("The waypoint prefab to use when getting the object to move out of a stuck state.")]
        WayPoint m_waypointPrefab;
        [SerializeField, Tooltip("The range of the waypoint sensor this body has.")]
        float m_sensorRange = 200;
        [SerializeField, Tooltip("The layers on which waypoints will be detected by this body")]
        LayerMask m_detectsOnLayers = 1 << 0; // default
        [SerializeField, Tooltip("Strategy for selecting the next waypoint when one is not currently selected.")]
        SelectionStrategy m_SelectionStrategy = SelectionStrategy.nearest;
        [SerializeField, Tooltip("Randomness in the selection of the next waypoint. The higher this value the more randomness there will be."), Range(0f, 1f)]
        float m_SelectionRandomness = 0.2f;

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

        [Header("Auto Enable/Disable")]
        [SerializeField, Tooltip("If true this object will be enabled and disabled according to the paramters set below.")]
        bool m_AutoDisable = true;
        [SerializeField, Tooltip("If the object is at least this distance from the maincamera the object will be disabled.")]
        int sqrDisableDistance = 1000;
        [SerializeField, Tooltip("How frequently, in seconds, should this distance be checked?")]
        float m_EnabledCheckInterval = 2.0f;

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
        private Transform cameraTransform;
        List<WayPoint> m_DetectedWayPoints = new List<WayPoint>();

        private WayPoint nextWaypoint;
        private WayPoint lastWaypoint;
#if PHOTOSESSION_PRESENT
        PhotoSession photoSession;
#endif

        private void Start()
        {
            if (m_RandomizeStartingPosition)
            {
                Scan();
                if (m_DetectedWayPoints.Count > 0)
                {
                    WayPoint start = m_DetectedWayPoints[Random.Range(0, m_DetectedWayPoints.Count)].GetComponent<WayPoint>();
                    if (start)
                    {
                        transform.position = start.transform.position;
                        transform.rotation = start.transform.rotation;
                    }
                }
            }

            timeToStuck = m_StuckDuration;
            sqrStuckTolerance = m_StuckTolerance * m_StuckTolerance;

            if (m_AutoDisable)
            {
                cameraTransform = Camera.main.transform;
                if (cameraTransform == null) {
                    Debug.LogError($"{name} is set to auto disable but no MainCamera is available in the scene.");
                }

                sqrDisableDistance = sqrDisableDistance * sqrDisableDistance;

                InvokeRepeating("CheckEnableDisable", Random.value * m_EnabledCheckInterval, m_EnabledCheckInterval);
                CheckEnableDisable();
            }

#if PHOTOSESSION_PRESENT
            photoSession = GameObject.FindObjectOfType<PhotoSession>();
#endif
        }

        private void OnEnable()
        {
            timeToStuck = m_StuckDuration;
        }

        void CheckEnableDisable()
        {
            if (gameObject.activeInHierarchy) {
                if ((transform.position - cameraTransform.position).sqrMagnitude >= sqrDisableDistance)  
                {
                    gameObject.SetActive(false);
                }
            }
            else if ((transform.position - cameraTransform.position).sqrMagnitude < sqrDisableDistance)
            {
                gameObject.SetActive(true);
            }
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
                if (m_SteeringRig.isLanding) return false;
                if (m_SteeringRig.isIdle) return false;
                if (m_SteeringRig.isTakingOff) return false;

                if (Vector3.SqrMagnitude(oldPosition - transform.position) < sqrStuckTolerance)
                {
                    timeToStuck -= Time.deltaTime;
                }
                else
                {
                    oldPosition = transform.position;
                    timeToStuck = m_StuckDuration;
                }

                return timeToStuck <= 0;
            }
        }

        /// <summary>
        /// Scan the sensor area for Waypoints.
        /// </summary>
        void Scan()
        {
            //Optimization: Only scan after a period of time has elapsed since the last scan
            //Optimization: Scan close by more frequently than at the full range
            WayPoint waypoint;
            m_DetectedWayPoints.Clear();
            //Optimization: use NonAlloc and manually managed cache size.s
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_sensorRange, m_detectsOnLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < colliders.Length; i++)
            {
                waypoint = colliders[i].GetComponent<WayPoint>();
                if (waypoint)
                {
                    m_DetectedWayPoints.Add(waypoint);
                }
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

                m_DetectedWayPoints.Add(currentWaypoint);
            }

            if (!currentWaypoint)
            {
                Scan();
                SelectNewWaypoint();
            }
            else if (m_SteeringRig.hasReachedDestination)
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
                            if (m_DetectedWayPoints.Count > 0)
                            {
                                currentWaypoint = m_DetectedWayPoints[Random.Range(0, m_DetectedWayPoints.Count)].GetComponent<WayPoint>();
                            }
                            break;
                        default:
                            Debug.LogError("Unknown selection strategy: " + m_SelectionStrategy);
                            break;
                    }
                }
            }

            if (currentWaypoint == null) return;
        }

        /// <summary>
        /// Returns a list of detected Waypoints ordered by their distance from the body.
        /// </summary>
        public List<WayPoint> DetectedObjectsOrderedByDistance
        {
            get
            {
                List<WayPoint> detectedWaypointsOrderedByDistance = new List<WayPoint>();
                detectedWaypointsOrderedByDistance.Clear();
                detectedWaypointsOrderedByDistance.AddRange(m_DetectedWayPoints);
                DistanceComparer comparer = new DistanceComparer();
                comparer.position = transform.position;
                detectedWaypointsOrderedByDistance.Sort(comparer);
                return detectedWaypointsOrderedByDistance;
            }
        }

        private WayPoint GetWeightedFurthestFromPointWithComponent(Vector3 point)
        {
            WayPoint furthest = null;
            var furthestDistance = 0f;
            var gos = DetectedObjectsOrderedByDistance;
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
            var gos = DetectedObjectsOrderedByDistance;
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
        }
    }

    public class DistanceComparer : IComparer<WayPoint>
    {
        public Vector3 position;
        public int Compare(WayPoint x, WayPoint y)
        {
            var d1 = Vector3.SqrMagnitude(x.transform.position - position);
            var d2 = Vector3.SqrMagnitude(y.transform.position - position);
            if (d1 < d2)
            {
                return -1;
            }
            else if (d1 > d2)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}