using UnityEngine;
using System.Collections;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using UnityEngine.Serialization;
using WizardsCode.AI;

namespace WizardsCode.Spawning
{
    public class BoxAreaSpawner : MonoBehaviour
    {
        //FIXME: generalize this so the spawner can be used for other items, not just WayPoints
        [SerializeField, Tooltip("The item we want to spawn using this spawner.")]
        public WayPoint ToSpawn;
        [SerializeField, Tooltip("The number of items to spawn at start.")]
        public int StartSpawnAmount;
        [SerializeField, Tooltip("The total number of items to have spawned. If an of the spawned items are destroyed then new ones will be spawned.")]
        [FormerlySerializedAs("Number")]
        public int TotalNumber;
        [SerializeField, Tooltip("If there are not currently the TotalNumber of objects spawned into the world then how much time should pass between new ones being spawned.")]
        public float SpawnInterval;
        
        [SerializeField, Tooltip("The x dimension of the box within which the objects will be spawned.")]
        public float SizeX = 10f;
        [SerializeField, Tooltip("The z dimension of the box within which the objects will be spawned.")]
        public float SizeZ = 10f;
        [SerializeField, Tooltip("The minimum height at which objects will spawn. This can be affected by the clear radius below (i.e. the larger of the two will take precedent).")]
        public float minHeight = 7;
        [SerializeField, Tooltip("Either the y dimension of the spawn box or the maximum height above the terrain to spawn waypoints. Which this represents depends on the `AdjustToTerrainHeight` setting below.")]
        public float maxHeight = 7;

        [Header("Positioning")]
        [SerializeField, Tooltip("If true then items spawned by this spawner will have their height adjusted to be under the max height above the terrain or mesh objects at the coordinates.")]
        [FormerlySerializedAs("AdjustToTerrainHeight")]
        public bool AdjustHeight = true;
        [SerializeField, Tooltip("Set to true if you want to allow spawn points below water. If false then items spawned by this spawner will have their height adjusted to be under the max height of the water surface if appropriate at the spawn coordinates. This setting overrides the AdjustToTerrainHeight where approrpiate, that is if this is false then the spawn point will be above water regardless of the size height of the terrain and the AsjustToTerrainHeight setting.")]
        [FormerlySerializedAs("SpawnAboveWater")] // changed 1/29
        public bool SpawnBelowWater = false;
        [SerializeField, Tooltip("The layers to look for obsructions when spawning.")]
        public LayerMask ObstructingLayers;

        [Header("Debug")]
        [SerializeField, Tooltip("Show available spawn Gizmos.")]
        bool m_ShowAvailable = false;
        [SerializeField, Tooltip("Show unavailable spawn Gizmos.")]
        bool m_ShowUnavailable = false;

        float spawnCountdown;
        GameObject[] spawned;
        private int instanceCount;

        void Awake()
        {
            spawned = new GameObject[TotalNumber];
        }

        void Start()
        {
            for (int i = 0; i < StartSpawnAmount; i++)
            {
                Spawn();
            }
        }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(SpawnRoutine());
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                StopAllCoroutines();
            }
        }

        IEnumerator SpawnRoutine()
        {
            spawnCountdown = SpawnInterval;
            while (true)
            {
                spawnCountdown -= Time.deltaTime;
                if (spawnCountdown <= 0f)
                {
                    Spawn();
                }
                yield return null;
            }
        }

        int nextAvailableSlot
        {
            get
            {
                for (int i = 0; i < spawned.Length; i++)
                {
                    if (spawned[i] == null) return i;
                }
                return -1;
            }
        }

        void Spawn()
        {
            spawnCountdown = SpawnInterval;
            var nextSlot = nextAvailableSlot;
            if (nextSlot == -1) return; // No spawn slots available
            
            //OPTIMIZATION: Use a pool
            WayPoint go = Instantiate(ToSpawn) as WayPoint;

            int nTrys = 0;
            Vector3 pos;
            do
            {
                nTrys++;
                if (nTrys > 50)
                {
                    Debug.LogWarning("Failed to find spawn location after 10 tries, aborting.", gameObject);
                    return;
                }
                pos = ChooseLocation(go);
            } while (LocationIsObstructed(pos, go));

            go.transform.SetPositionAndRotation(pos, transform.rotation);
            go.name += instanceCount++;
            go.transform.SetParent(transform.parent);
            spawned[nextSlot] = go.gameObject;
        }

        /// <summary>
        /// Shoose a suitable location for an object to spawn.
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        Vector3 ChooseLocation(WayPoint go)
        {
            Vector3 dimensions = new Vector3(SizeX / 2f, maxHeight - minHeight, SizeZ / 2f);
            Vector3 randNormalizedVector = new Vector3(Random.Range(-1f, 1f), Random.Range(0, 1f), Random.Range(-1f, 1f));
            Vector3 pos = Vector3.Scale(dimensions, randNormalizedVector) + transform.position;
            pos.y += minHeight - transform.position.y;

            pos = GetHeightAdjusted(pos, go);

            return pos;
        }

        /// <summary>
        /// Adjust the height of a position in the terrain to allow for any obstructions on the terrain.
        /// </summary>
        /// <param name="pos">The approximate position the object should be placed</param>
        /// <param name="go">The game object to place at the position</param>
        /// <returns></returns>
        private Vector3 GetHeightAdjusted(Vector3 pos, WayPoint go)
        {
            if (!AdjustHeight && SpawnBelowWater) return pos;

            float clearance;
            if (go == null)
            {
                clearance = 0.2f;
            }
            else
            {
                clearance = (go.ClearRadius * 1.05f);
            }

            //TODO: consider using just raycast, do we really need to measure terrain heigh. Consider that this will not take into account items onthe terrain,
            RaycastHit hit;
            float height = 0;
            bool hasHit = Physics.Raycast(pos, Vector3.down, out hit, Mathf.Infinity);
            if (hasHit)
            {
                height = hit.distance;

                if (AdjustHeight)
                {
                    if (pos.y < height + clearance)
                    {
                        pos.y = height + clearance;
                    }
                    else if (pos.y - height > maxHeight)
                    {
                        pos.y = height + Random.Range(height + clearance, height + maxHeight);
                    }
                    else
                    {
                        Debug.LogWarning($"{name} has `AdjustHeight` enabled but there is no raycast hit below {pos}. Relying on the initial spawn positions height.");
                    }
                }

                if (!SpawnBelowWater)
                {
                    float waterHeight = hit.distance;

                    if (pos.y < waterHeight + clearance)
                    {
                        pos.y = waterHeight + clearance;
                    }
                    else if (pos.y - waterHeight > maxHeight)
                    {
                        pos.y = waterHeight + Random.Range(clearance, maxHeight);
                    }
                }
            }

            return pos;
        }

        bool LocationIsObstructed(Vector3 location, WayPoint go)
        {
            return Physics.CheckSphere(location, go.ClearRadius, ObstructingLayers);
        }

        public void OnDrawGizmosSelected()
        {
            if (!isActiveAndEnabled) return;

            // Blue box marking the boundary of the spawn area
            Gizmos.color = new Color(0, 0, 1, 0.35f);
            Gizmos.DrawCube(transform.position, new Vector3(SizeX, maxHeight - minHeight, SizeZ));
            Gizmos.color = new Color(0, 0, 1, 1);
            Gizmos.DrawWireCube(transform.position, new Vector3(SizeX, maxHeight - minHeight, SizeZ));

            // Red sphere marking the center of the box
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(GetHeightAdjusted(transform.position, null), Mathf.Max(0.2f, 0.2f));

            if (m_ShowAvailable || m_ShowUnavailable)
            {
                //OPTIMIZATION: Use a cached sample object (See start of method as well)
                WayPoint go = Instantiate(ToSpawn) as WayPoint;

                float depth = SizeX / 2;
                float xInterval = Mathf.Max(0.3f, SizeX / 20);
                float height = maxHeight / 2;
                float yInterval = Mathf.Max(0.3f, maxHeight / 20);
                float width = SizeZ / 2;
                float zInterval = Mathf.Max(0.3f, SizeZ / 20);

                for (float x = 0; x < SizeX; x += xInterval)
                {
                    for (float z = 0; z < SizeZ; z += zInterval)
                    {
                        for (float y = 0; y < maxHeight; y += yInterval)
                        {
                            {
                                Vector3 pos = transform.position + new Vector3(-width + x, -height + y, -depth + z);
                                pos = GetHeightAdjusted(pos, go);
                                if (m_ShowAvailable && !LocationIsObstructed(pos, go))
                                {
                                    Gizmos.color = Color.green;
                                    Gizmos.DrawSphere(pos, 0.2f);
                                }
                                else if (m_ShowUnavailable && LocationIsObstructed(pos, go))
                                {
                                    Gizmos.color = Color.red;
                                    Gizmos.DrawSphere(pos, 0.2f);
                                }
                            }
                        }
                    }
                }

                //OPTIMIZATION: Use a cached sample object (See start of method as well)
                DestroyImmediate(go);
            }
        }
    }
}