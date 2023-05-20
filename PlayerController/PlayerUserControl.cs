using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Tour
{
    public class PlayerUserControl : Singleton<PlayerUserControl>
    {
        public delegate void LeftHandPrimaryActionDelegate();
        public delegate void LeftHandSecondaryActionDelegate();
        public delegate void RightHandPrimaryActionDelegate();
        public delegate void RightHandSecondaryActionDelegate();
        public delegate void SprintActionDelegate();
        public delegate void GadgetActionDelegate();

        public static event LeftHandPrimaryActionDelegate LeftHandPrimaryAction;
        public static event LeftHandSecondaryActionDelegate LeftHandSecondaryAction;
        public static event RightHandPrimaryActionDelegate RightHandPrimaryAction;
        public static event RightHandSecondaryActionDelegate RightHandSecondaryAction;
        public static event SprintActionDelegate SprintAction;
        public static event GadgetActionDelegate GadgetAction;

        public static bool leftHandPrimaryDown => currentInstance.m_leftHandPrimaryDown;
        public static bool rightHandPrimaryDown => currentInstance.m_rightHandPrimaryDown;

        private bool m_leftHandPrimaryDown = false;
        private bool m_leftHandSecondaryDown = false;
        private bool m_rightHandPrimaryDown = false;
        private bool m_rightHandSecondaryDown = false;

        private Dictionary<Type, Delegate> test = new Dictionary<Type, Delegate>();

        public static bool isSprinting = false;
        public static bool isCrouching = false;
        public static bool isSlowWalking = false;
        private bool m_primaryLeftHandSingleClick = false;
        private bool m_secondaryLeftHandSingleClick = false;
        private bool m_primaryLeftHandSingleClickAvailable = false;
        private bool m_secondaryLeftHandSingleClickAvailable = false;

        private bool m_primaryRightHandSingleClick = false;
        private bool m_secondaryRightHandSingleClick = false;
        private bool m_primaryRightHandSingleClickAvailable = false;
        private bool m_secondaryRightHandSingleClickAvailable = false;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start()
        {
            UIManager.cursorUI.cursorEnabled = true;
            UIManager.cursorUI.ActivateCursor(false);
            UIManager.cursorUI.SetCursor(CursorUI.CursorType.Interact);
        }

        private void Update()
        {
            isSprinting = false;
            isSlowWalking = false;
            PlayerManager.player.movement.x = Input.GetAxis("Horizontal");
            PlayerManager.player.movement.y = Input.GetAxis("Vertical");
            PlayerManager.player.rotation.x = Input.GetAxis("Mouse X");
            PlayerManager.player.rotation.y = Input.GetAxis("Mouse Y");
            PlayerManager.player.movement = Vector2.ClampMagnitude(PlayerManager.player.movement, 1.0f);


            if (Input.mouseScrollDelta.y >= 1 || Input.mouseScrollDelta.y <= -1)
            {
                PlayerManager.player.inventory.activeToolBelt.ChangeIndex(PlayerManager.player.inventory.activeToolBelt.placementType, Input.mouseScrollDelta.y > 0 ? -1 : 1);
                UIManager.inventoryUI.SetToolBarFrame(PlayerManager.player.inventory.activeToolBelt.placementType, PlayerManager.player.inventory.activeToolBelt.index);
            }

            if (Input.GetMouseButtonDown(2))
            {
                PlayerManager.player.inventory.ChangeToolBeltPriority();
                UIManager.inventoryUI.SwitchToolBarFramePriority();
            }

            LeftHandInteractionCheck();
            RightHandInteractionCheck();


            if (Input.GetKey(KeyCode.LeftShift) && PlayerManager.player.movement.x == 0.0f && PlayerManager.player.movement.y > 0.0f)
            {
                isSprinting = true;
            }

            if(Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching;
            }

            if (Input.GetKey(KeyCode.X))
            {
                isSlowWalking = true;
            }

            if(Input.GetKeyDown(KeyCode.Return))
            {
                PlayerManager.player.ThrowObject();
            }
        }

        private bool LeftHandInteractionCheck()
        {
            if(m_rightHandPrimaryDown == true)
            {
                return false;
            }
            bool interaction = false;
            if (PlayerManager.player.selectedLeftHandInteractable == null)
            {
                m_leftHandPrimaryDown = false;
                m_leftHandSecondaryDown = false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (PlayerManager.player.selectedLeftHandInteractable != null)
                {
                    PlayerManager.player.currentInteractable = PlayerManager.player.selectedLeftHandInteractable;
                    m_leftHandPrimaryDown = true;
                    interaction = true;
                }
            }

            if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(1) && m_leftHandPrimaryDown == true)
            {
                if (m_primaryLeftHandSingleClick == true)
                {
                    if (m_primaryLeftHandSingleClickAvailable != false)
                    {
                        m_primaryLeftHandSingleClickAvailable = false;
                        interaction = true;
                        LeftHandPrimaryAction?.Invoke();
                        m_leftHandPrimaryDown = false;
                    }
                }
                else
                {
                    interaction = true;
                    LeftHandPrimaryAction?.Invoke();
                }
            }
            else if (Input.GetMouseButton(0) && Input.GetMouseButton(1) && m_leftHandPrimaryDown == true)
            {
                if (m_secondaryLeftHandSingleClick == true)
                {
                    if (m_secondaryLeftHandSingleClickAvailable != false)
                    {
                        m_secondaryLeftHandSingleClickAvailable = false;
                        interaction = true;
                        RightHandSecondaryAction?.Invoke();
                        m_leftHandPrimaryDown = false;
                    }
                }
                else
                {
                    interaction = true;
                    RightHandSecondaryAction?.Invoke();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                m_primaryLeftHandSingleClickAvailable = true;
                PlayerManager.player.currentInteractable = null;
                UIManager.cursorUI.ActivateCursor(false);
                interaction = true;
                m_leftHandPrimaryDown = false;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                interaction = true;
                m_leftHandSecondaryDown = false;
            }

            return interaction;
        }

        private bool RightHandInteractionCheck()
        {
            if (m_leftHandPrimaryDown == true)
            {
                return false;
            }
            bool interaction = false;
            if (PlayerManager.player.selectedRightHandInteractable == null)
            {
                m_rightHandPrimaryDown = false;
                m_rightHandSecondaryDown = false;
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (PlayerManager.player.selectedRightHandInteractable != null)
                {
                    PlayerManager.player.currentInteractable = PlayerManager.player.selectedRightHandInteractable;
                    m_rightHandPrimaryDown = true;
                    interaction = true;
                }
            }

            if (Input.GetMouseButton(1) && !Input.GetMouseButtonDown(0) && m_rightHandPrimaryDown == true)
            {
                if (m_primaryRightHandSingleClick == true)
                {
                    if (m_primaryRightHandSingleClickAvailable != false)
                    {
                        m_primaryRightHandSingleClickAvailable = false;
                        interaction = true;
                        RightHandPrimaryAction?.Invoke();
                        m_rightHandPrimaryDown = false;
                    }
                }
                else
                {
                    interaction = true;
                    RightHandPrimaryAction?.Invoke();
                }
            }
            else if (Input.GetMouseButton(1) && Input.GetMouseButton(0) && m_rightHandPrimaryDown == true)
            {
                if (m_secondaryRightHandSingleClick == true)
                {
                    if (m_secondaryRightHandSingleClickAvailable != false)
                    {
                        m_secondaryRightHandSingleClickAvailable = false;
                        interaction = true;
                        LeftHandSecondaryAction?.Invoke();
                        m_rightHandPrimaryDown = false;
                    }
                }
                else
                {
                    interaction = true;
                    LeftHandSecondaryAction?.Invoke();
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                m_primaryRightHandSingleClickAvailable = true;
                PlayerManager.player.currentInteractable = null;
                UIManager.cursorUI.ActivateCursor(false);
                interaction = true;
                m_rightHandPrimaryDown = false;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                interaction = true;
                m_rightHandSecondaryDown = false;
            }

            return interaction;
        }

        public void PrimaryLeftHandSingleClickEnabled(bool clickEnabled)
        {
            m_primaryLeftHandSingleClick = clickEnabled;
            m_primaryLeftHandSingleClickAvailable = clickEnabled;
        }

        public void SecondaryLeftHandSingleClickEnabled(bool clickEnabled)
        {
            m_secondaryLeftHandSingleClick = clickEnabled;
            m_secondaryLeftHandSingleClickAvailable = clickEnabled;
        }

        public void PrimaryRightHandSingleClickEnabled(bool clickEnabled)
        {
            m_primaryRightHandSingleClick = clickEnabled;
            m_primaryRightHandSingleClickAvailable = clickEnabled;
        }

        public void SecondaryRightHandSingleClickEnabled(bool clickEnabled)
        {
            m_secondaryRightHandSingleClick = clickEnabled;
            m_secondaryRightHandSingleClickAvailable = clickEnabled;
        }

        public static void RemoveLeftPrimaryActions()
        {
            if(LeftHandPrimaryAction == null)
            {
                return;
            }
            foreach(Delegate @event in LeftHandPrimaryAction.GetInvocationList())
            {
                LeftHandPrimaryAction -= (LeftHandPrimaryActionDelegate)@event;
            }
        }

        public static void RemoveLeftSecondaryActions()
        {
            if (LeftHandSecondaryAction == null)
            {
                return;
            }
            foreach (Delegate @event in LeftHandSecondaryAction.GetInvocationList())
            {
                LeftHandSecondaryAction -= (LeftHandSecondaryActionDelegate)@event;
            }
        }

        public static void RemoveRightPrimaryActions()
        {
            if (RightHandPrimaryAction == null)
            {
                return;
            }
            foreach (Delegate @event in RightHandPrimaryAction.GetInvocationList())
            {
                RightHandPrimaryAction -= (RightHandPrimaryActionDelegate)@event;
            }
        }

        public static void RemoveRightSecondaryActions()
        {
            if (RightHandSecondaryAction == null)
            {
                return;
            }
            foreach (Delegate @event in RightHandSecondaryAction.GetInvocationList())
            {
                RightHandSecondaryAction -= (RightHandSecondaryActionDelegate)@event;
            }
        }
    }
}