using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tour
{
    public class DoorObject : Interactable
    {
        public enum DoorFaceType
        {
            None,
            Front,
            Back,
            Side
        }

        public DoorAssembly doorAssembly => m_doorAssembly;
        public DoorFaceType selectedDoorInteraction => m_selectedInteraction;
        public bool collidedWithPlayer => m_collidedWithPlayer;

        private DoorAssembly m_doorAssembly;
        private MeshCollider m_meshCollider;
        private List<Vector3> m_front = new List<Vector3>();
        private List<Vector3> m_back = new List<Vector3>();
        private List<Vector3> m_side = new List<Vector3>();
        private DoorFaceType m_selectedInteraction = DoorFaceType.None;
        private bool m_collidedWithPlayer = false;
        [SerializeField] private bool m_debugEnable = false;

        public void Initialize(DoorAssembly doorAssembly)
        {
            m_meshCollider = this.GetComponent<MeshCollider>();
            if(m_meshCollider == null)
            {
                Debug.LogError($"{this.gameObject.name} doesn't have a meshcollider...");
                return;
            }
            m_doorAssembly = doorAssembly;
            ConstructVerticeLists();
        }

        private void FixedUpdate()
        {
            
        }

        private void ConstructVerticeLists()
        {
            for (int i = 0; i < m_meshCollider.sharedMesh.vertices.Length; i++)
            {
                Vector3 localvertice = m_meshCollider.sharedMesh.vertices[i];
                if (localvertice.z > 0.0f)
                {
                    m_front.Add(localvertice);
                }
                else if (localvertice.z < 0.0f)
                {
                    m_back.Add(localvertice);
                }

                if (doorAssembly.doorOpeningType == DoorAssembly.DoorOpeningType.LeftToRight)
                {
                    if (localvertice.x > 0.0f)
                    {
                        m_side.Add(localvertice);
                    }
                }
                else
                {
                    if (localvertice.x < 0.0f)
                    {
                        m_side.Add(localvertice);
                    }
                }
            }
        }

        public void SetInteraction(DoorFaceType type)
        {
            m_selectedInteraction = type;
        }

        public bool ValidateTriangleIndex(int index, out DoorFaceType type)
        {
            type = DoorFaceType.None;
            Mesh mesh = m_meshCollider.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 p0 = vertices[triangles[index * 3 + 0]];
            Vector3 p1 = vertices[triangles[index * 3 + 1]];
            Vector3 p2 = vertices[triangles[index * 3 + 2]];
            bool foundP0 = m_front.Contains(p0);
            bool foundP1 = m_front.Contains(p1);
            bool foundP2 = m_front.Contains(p2);
            if (foundP0 && foundP1 && foundP2)
            {
                type = DoorFaceType.Front;
                Debug.Log("Front");
                return true;
            }

            foundP0 = m_side.Contains(p0);
            foundP1 = m_side.Contains(p1);
            foundP2 = m_side.Contains(p2);
            if (foundP0 && foundP1 && foundP2)
            {
                type = DoorFaceType.Side;
                Debug.Log("Side");
                return true;
            }

            foundP0 = m_back.Contains(p0);
            foundP1 = m_back.Contains(p1);
            foundP2 = m_back.Contains(p2);
            if (foundP0 && foundP1 && foundP2)
            {
                type = DoorFaceType.Back;
                Debug.Log("Back");
                return true;
            }
            return false;
        }

        public void DoorInteraction(Vector3 targetPivot, DoorAssembly.DoorInteractionType interactionType, bool isInstant = false)
        {
            if (m_doorAssembly.HasOpened == false)
                return;
            m_doorAssembly.DoorObjectInteraction(targetPivot, interactionType, isInstant);
        }
    }
}