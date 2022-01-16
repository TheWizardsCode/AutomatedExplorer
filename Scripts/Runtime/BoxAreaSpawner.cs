using UnityEngine;
using System.Collections;
using System;
using Random = UnityEngine.Random;

namespace WizardsCode.Spawning
{
    public class BoxAreaSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("The item we want to spawn using this spawner.")]
        public GameObject ToSpawn;
        public int Number;
        public float SpawnInterval;
        public int StartSpawnAmount;

        public float SizeX = 10f;
        private float SizeY = 10f; // Height is only used in the visuals, the position of the spawned objects is controlled by maxHeight
        public float SizeZ = 10f;

        [SerializeField, Tooltip("The maximum height above the terrain to spawn waypoints.")]
        public float maxHeight = 7;

        [Header("Obstruction")]
        public float ClearRadius = 1.8f;
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
            spawned = new GameObject[Number];
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
                pos = chooseLocation();
            } while (LocationIsObstructed(pos));

            var newInst = Instantiate(ToSpawn, pos, transform.rotation) as GameObject;
            newInst.transform.SetParent(transform.parent);
            spawned[nextSlot] = newInst;
        }

        Vector3 chooseLocation()
        {
            Vector3 dimensions = new Vector3(SizeX / 2f, maxHeight, SizeZ / 2f);
            Vector3 randNormalizedVector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            Vector3 pos = Vector3.Scale(dimensions, randNormalizedVector) + transform.position;

            pos = AdjustHeight(pos);

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
                for (int i = 0; i < Terrain.activeTerrains.Length; i++)
                {
                    terrain = Terrain.activeTerrains[i];
                    if ((terrain.transform.position.x >= 0 && pos.x >= terrain.transform.position.x && pos.x < terrain.transform.position.x + terrain.terrainData.size.x)
                        || (terrain.transform.position.x <= 0 && pos.x >= terrain.transform.position.x - terrain.terrainData.size.x && pos.x < terrain.transform.position.x))
                    {
                        if ((terrain.transform.position.z >= 0 && pos.z >= terrain.transform.position.z && pos.z < terrain.transform.position.z + terrain.terrainData.size.z)
                        || (terrain.transform.position.z <= 0 && pos.z >= terrain.transform.position.z - terrain.terrainData.size.z && pos.z < terrain.transform.position.z))
                        {
                            Debug.Log($"Terrain at {pos} is {terrain.name}");
                            break;
                        }
                    }
                    terrain = null;
                }
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
                    pos.y = Random.Range(terrainHeight + clearance, terrainHeight + maxHeight);
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
            Gizmos.DrawCube(transform.position, new Vector3(SizeX, SizeY, SizeZ));
            Gizmos.color = new Color(0, 0, 1, 1);
            Gizmos.DrawWireCube(transform.position, new Vector3(SizeX, SizeY, SizeZ));

            // Red sphere marking the center of the box
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(AdjustHeight(transform.position), Mathf.Max(0.2f, ClearRadius));

            if (m_ShowAvailable || m_ShowUnavailable)
            {
                float depth = SizeX / 2;
                float xInterval = Mathf.Max(0.3f, SizeX / 20);
                float height = SizeY / 2;
                float yInterval = Mathf.Max(0.3f, SizeY / 20);
                float width = SizeZ / 2;
                float zInterval = Mathf.Max(0.3f, SizeZ / 20);

                for (float x = 0; x < SizeX; x += xInterval)
                {
                    for (float z = 0; z < SizeZ; z += zInterval)
                    {
                        for (float y = 0; y < SizeY; y += yInterval)
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