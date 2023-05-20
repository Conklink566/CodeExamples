using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tour
{
    public class PlayerInventory : MonoBehaviour
    {
        public enum ToolPlacement
        {
            LeftSide,
            RightSide
        };

        public delegate void ChangeInteractableDelegate(PlayerInventory.ToolPlacement placement, InteractableObject interactable);
        public static ChangeInteractableDelegate OnChangeInteractable;

        public class PlayerToolBelt
        {
            public InteractableObject[] interactables = new InteractableObject [3];
            public int index => m_index;
            private int m_index = 0;
            private int m_savedIndex = -1;
            public ToolPlacement placementType { get; private set; }

            public PlayerToolBelt(ToolPlacement placement)
            {
                placementType = placement;
            }

            public void ChangeIndex(PlayerInventory.ToolPlacement placement, int changeIndex)
            {
                m_index += changeIndex;
                if(m_index >= interactables.Length)
                {
                    m_index = 0;
                }
                else if(m_index < 0)
                {
                    m_index = interactables.Length - 1;
                }
                OnChangeInteractable?.Invoke(placement, interactables[m_index]);
            }

            public void SetIndex(PlayerInventory.ToolPlacement placement, int setIndex)
            {
                m_index = setIndex;
                if(m_index < -1)
                {
                    m_index = -1;
                }
                else if (m_index >= interactables.Length)
                {
                    m_index = interactables.Length - 1;
                }
                if (m_index == -1)
                {
                    OnChangeInteractable?.Invoke(placement, null);
                }
                else
                {
                    OnChangeInteractable?.Invoke(placement, interactables[m_index]);
                }
            }

            public bool HasItemInHand()
            {
                return interactables[index] != null;
            }

            public InteractableObject SelectedInteractable()
            {
                return interactables[index];
            }
        }

        [SerializeField] private Transform[] m_leftSideToolSlots;
        [SerializeField] private Transform[] m_rightSideToolSlots;

        public PlayerToolBelt leftSideTools => m_leftSideTools;
        public PlayerToolBelt rightSideTools => m_rightSideTools;
        public ToolPlacement selectedToolPlacement => m_toolPlacement;

        private PlayerToolBelt m_leftSideTools;
        private PlayerToolBelt m_rightSideTools;
        private ToolPlacement m_toolPlacement = ToolPlacement.RightSide;

        public PlayerToolBelt activeToolBelt
        {
            get
            {
                return m_toolPlacement == ToolPlacement.LeftSide ? m_leftSideTools : m_rightSideTools;
            }
        }

        private void Awake()
        {
            m_leftSideTools = new PlayerToolBelt(ToolPlacement.LeftSide);
            m_rightSideTools = new PlayerToolBelt(ToolPlacement.RightSide);
        }

        private void Start()
        {
            UIManager.inventoryUI.SetToolBarFrame(m_leftSideTools.placementType, 0);
            UIManager.inventoryUI.SetToolBarFrame(m_rightSideTools.placementType, 0);
            for (int i = 0; i < m_leftSideTools.interactables.Length; i++)
            {
                UIManager.inventoryUI.SetToolBarIcon(m_leftSideTools.placementType, i, null);
                UIManager.inventoryUI.SetToolBarIcon(m_rightSideTools.placementType, i, null);
            }
        }

        public bool AddTool(ToolPlacement placement, InteractableObject interactable)
        {
            PlayerToolBelt beltDestination = placement == ToolPlacement.LeftSide ? leftSideTools : rightSideTools;
            Transform[] toolPlacement = placement == ToolPlacement.LeftSide ? m_leftSideToolSlots : m_rightSideToolSlots;
            bool foundEmptySlot = false;
            int slotLocation = -1;
            for(int i = 0; i < beltDestination.interactables.Length; i++)
            {
                if (beltDestination.interactables[i] != null)
                {
                    continue;
                }
                if(beltDestination.index != i)
                {
                    InteractableObject objectInstance = null;
                    if (interactable.objectType == InteractableObject.ObjectType.Level)
                    {
                        objectInstance = Instantiate(DataBase.levelObjectDataBase.FindAsset(interactable.reference.guid).sourceObject);

                    }
                    else if (interactable.objectType == InteractableObject.ObjectType.Gadget)
                    {
                        objectInstance = Instantiate(DataBase.gadgetObjectDataBase.FindAsset(interactable.reference.guid).sourceObject);
                    }
                    beltDestination.interactables [i] = objectInstance;
                    objectInstance.objectPlacement = beltDestination.placementType;
                    UIManager.inventoryUI.SetToolBarIcon(beltDestination.placementType, i, objectInstance.objectIcon);
                    objectInstance.gameObject.transform.SetParent(toolPlacement[i]);
                    objectInstance.transform.localPosition = Vector3.zero;
                    return true;
                }
                foundEmptySlot = true;
                slotLocation = i;
            }
            if(foundEmptySlot == true)
            {
                InteractableObject objectInstance = null;
                if (interactable.objectType == InteractableObject.ObjectType.Level)
                {
                    objectInstance = Instantiate(DataBase.levelObjectDataBase.FindAsset(interactable.reference.guid).sourceObject);

                }
                else if (interactable.objectType == InteractableObject.ObjectType.Gadget)
                {
                    objectInstance = Instantiate(DataBase.gadgetObjectDataBase.FindAsset(interactable.reference.guid).sourceObject);
                }
                beltDestination.interactables [slotLocation] = objectInstance;
                objectInstance.objectPlacement = beltDestination.placementType;
                UIManager.inventoryUI.SetToolBarIcon(beltDestination.placementType, slotLocation, objectInstance.objectIcon);
                OnChangeInteractable?.Invoke(placement, beltDestination.interactables [slotLocation]);
                return true;
            }

            return false;
        }

        public void ChangeToolBeltPriority()
        {
            m_toolPlacement = m_toolPlacement == ToolPlacement.LeftSide ? ToolPlacement.RightSide : ToolPlacement.LeftSide;
        }
    }
}