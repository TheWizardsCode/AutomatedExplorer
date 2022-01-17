using UnityEngine;
using System.Collections;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using UnityEngine.Serialization;

namespace WizardsCode.Spawning
{
    public class BoxAreaSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("The item we want to spawn using this spawner.")]
        public GameObject ToSpawn;
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
        [SerializeField, Tooltip("Either the y dimension of the spawn box or the maximum height above the terrain to spawn waypoints. Which this represents depends on the `AdjustToTerrainHeight` setting below.")]
        public float maxHeight = 7;

        [Header("Positioning")]
        [SerializeField, Tooltip("If true then items spawned by this spawner will have their height adjusted to be within the mx height of the terrain height at the spawn coordinates.")]
        public bool AdjustToTerrainHeight = true;
        [SerializeField, Tooltip("The radiues that will be tested for obstructions. If an obstruction is found within this radius then a new spawn point will be generated.")]
        public float ClearRadius = 1.8f;
        [SerializeField, Tooltip("The layers to look for obsructions when spawning.")]
        public LayerMask ObstructingLayers;

        [Header("Debug")]
        [SerializeField, Tooltip("Show available spawn Gizmos.")]
        bool m_ShowAvailable = false;
        [SerializeField, Tooltip("Show unavailable spawn Gizmos.")]
        bool m_ShowUnavailable = false;

        float spawnCountdown;
        GameObject[] spawned;

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
            StartCoroutine(SpawnRoutine());
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
                pos = ChooseLocation();
            } while (LocationIsObstructed(pos));

            var newInst = Instantiate(ToSpawn, pos, transform.rotation) as GameObject;
            newInst.transform.SetParent(transform.parent);
            spawned[nextSlot] = newInst;
        }

        Vector3 ChooseLocation()
        {
            Vector3 dimensions = new Vector3(SizeX / 2f, maxHeight, SizeZ / 2f);
            Vector3 randNormalizedVector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            Vector3 pos = Vector3.Scale(dimensions, randNormalizedVector) + transform.position;

            if (AdjustToTerrainHeight)
            {
                pos = AdjustHeight(pos);
            }

            return pos;
        }

        /// <summary>
        /// Adjust the height of a position in the terrain to allow for any obstructions on the terrain.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Vector3 AdjustHeight(Vector3 pos)
        {
            float clearance = (ClearRadius * 1.2f);
            
            Terrain terrain = null;
            if (Terrain.activeTerrains.Length == 1)
            {
                terrain = Terrain.activeTerrain;
            } else
            {
                terrain = Terrain.activeTerrains.OrderBy(x =>
                {
                    Vector3 terrainPosition = x.transform.position;
                    Vector3 terrainSize = x.terrainData.size * 0.5f;
                    Vector3 terrainCenter = new Vector3(terrainPosition.x + terrainSize.x, terrainPosition.y, terrainPosition.z + terrainSize.z);
                    return Vector3.SqrMagnitude(terrainCenter - pos);
                }).First();
            }                           

            if (terrain != null)
            {
                float terrainHeight = terrain.SampleHeight(pos);
                
                if (pos.y < terrainHeight + clearance)
                {
                    pos.y = terrain.transform.position.y + terrainHeight + clearance;
                }
                if (pos.y - terrainHeight > maxHeight)
                {
                    pos.y = terrain.transform.position.y + Random.Range(terrainHeight + clearance, terrainHeight + maxHeight);
                }
            } else
            {
                Debug.LogError($"No terrain found for {pos}.");
            }

            return pos;
        }

        bool LocationIsObstructed(Vector3 location)
        {
            return Physics.CheckSphere(location, ClearRadius, ObstructingLayers);
        }

        public void OnDrawGizmosSelected()
        {
            if (!isActiveAndEnabled) return;

            // Blue box marking the boundary of the spawn area
            Gizmos.color = new Color(0, 0, 1, 0.35f);
            Gizmos.DrawCube(transform.position, new Vector3(SizeX, maxHeight, SizeZ));
            Gizmos.color = new Color(0, 0, 1, 1);
            Gizmos.DrawWireCube(transform.position, new Vector3(SizeX, maxHeight, SizeZ));

            // Red sphere marking the center of the box
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(AdjustHeight(transform.position), Mathf.Max(0.2f, ClearRadius));

            if (m_ShowAvailable || m_ShowUnavailable)
            {
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
                                pos = AdjustHeight(pos);
                                if (m_ShowAvailable && !LocationIsObstructed(pos))
                                {
                                    Gizmos.color = Color.green;
                                    Gizmos.DrawSphere(pos, 0.2f);
                                }
                                else if (m_ShowUnavailable && LocationIsObstructed(pos))
                                {
                                    Gizmos.color = Color.red;
                                    Gizmos.DrawSphere(pos, 0.2f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}