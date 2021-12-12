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

        private Terrain terrain;

        private void Start()
        {
            terrain = Terrain.activeTerrain;
        }

        void LateUpdate()
        {
            if (!m_MaintainHeight) return;
                
            if (RB == null || RB.isKinematic || !IsSeeking) return;


            RaycastHit hit;
            float terrainHeight = 0;
            if (terrain != null)
            {
                terrainHeight = terrain.SampleHeight(RB.transform.position);
            } else
            {
                if (Physics.Raycast(RB.transform.position, Vector3.down, out hit, Mathf.Infinity))
                {
                    terrainHeight = hit.distance;
                } else
                {
                    return;
                }
            }

            float height = RB.transform.position.y - terrainHeight;
            if (height > optimalHeight)
            {
                if (height > maxHeight)
                {
                    RB.AddForce(RB.transform.up * -(MoveForce));
                }
                else
                {
                    RB.AddForce(RB.transform.up * -(MoveForce / 2));
                }
            }
            else if (height < optimalHeight)
            {
                if (height < minHeight)
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
