using System;
using System.ComponentModel;
using UnityEngine;

namespace Tour
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public enum Stance
        {
            Standing,
            CrouchingStill,
            CrouchingMoving
        }

        public delegate void SprintValueChangedDelegate(float sprintValue);
        public delegate void StanceValueChangedDelegate(Stance stance);
        public static event SprintValueChangedDelegate SprintValueChanged;
        public static event StanceValueChangedDelegate StanceValueChanged;

        private Stance m_currentStance = Stance.Standing;
        private CharacterController m_characterController;
        private Vector3 m_currentVelocity;
        [SerializeField] private Camera m_camera;
        [SerializeField] private float m_drag = 0.1f;
        [SerializeField] private float m_movementSpeed = 1.0f;
        [SerializeField] private float m_crouchWalkingSpeed = 0.5f;
        [SerializeField] private float m_slowWalkingSpeed = 0.25f;
        [SerializeField] private float m_sprintSpeedModifier = 1.5f;
        [SerializeField] private float m_sprintSpeedTime = 3.0f;
        [SerializeField] private float m_turnSpeed = 10.0f;
        [SerializeField] private float m_interactionRange = 2.0f;
        [SerializeField] private float m_exhaustionTimer = 2.0f;
        [SerializeField] private float m_sprintRecoveryTimer = 4.0f;
        [SerializeField] private float m_sprintinBetweenDelay = 0.5f;
        [SerializeField] private float m_distanceGuide = 0.75f;
        [SerializeField] private float m_distancePlayer = 1.25f;
        [SerializeField] private float m_cameraStandingHeight = 1.15f;
        [SerializeField] private float m_cameraCrouchingStillHeight = 0.4f;
        [SerializeField] private float m_cameraCrouchingMovingHeight = 0.7f;
        [SerializeField] private GameObject m_standingObject;
        [SerializeField] private GameObject m_crouchedObject;
        [SerializeField] private float m_standingHeight = 2.5f;
        [SerializeField] private float m_standingCenter = 0.25f;
        [SerializeField] private float m_crouchingHeight = 1.5f;
        [SerializeField] private float m_crouchingCenter = -0.25f;
        [SerializeField] private float m_cameraLerpSpeed = 1.0f;
        [SerializeField] private float m_gravityMagnitude = 1.0f;
        //Debug
        [SerializeField] private InteractableObject m_throwableObjectPrefab;
        [SerializeField] private float m_throwingPower;
        [SerializeField] private bool m_debugUnderHandThrow = false;
        [SerializeField] private float m_debugUnderHandThrowAngle = 60.0f;
        [SerializeField] private float m_debugUnderHandThrowOffset = 1.0f;
        [SerializeField] private string m_lefthandObjectName;
        [SerializeField] private string m_righthandObjectName;
        private float m_pushRange = 0.2f;

        public Interactable currentInteractable { get; set; } = null;
        public DoorObject currentDoorInteractable { get; set; } = null;
        public Interactable selectedLeftHandInteractable => m_selectedLeftHandInteractable;
        public Interactable selectedRightHandInteractable => m_selectedRightHandInteractable;
        public PlayerInventory inventory => m_playerInventory;
        public string playerId => m_playerID;
        public GameObject guidePoint => m_guidePoint;
        public Vector2 rotation = Vector2.zero;
        public Vector2 movement = Vector2.zero;

        private float m_xRotation = 0.0f;
        private float m_currentSprintPercentage = 1.0f;
        private bool m_isSprinting = false;
        private bool m_isExhausted = false;
        private float m_currentExhaustionTimer = 0.0f;
        private float m_currentsprintInbetweenDelay = 0.0f;
        private Interactable m_selectedLeftHandInteractable = null;
        private Interactable m_selectedRightHandInteractable = null;
        private string m_playerID;
        private GameObject m_guidePoint = null;
        private RaycastHit m_cachedRaycast;
        private bool m_isPushingDoor = false;
        private DoorObject m_cachedPushedDoor = null;
        private Vector3 m_lastPosition;
        private PlayerUserControl m_playerUserControl;
        private PlayerInventory m_playerInventory;
        private bool? m_isCrouching = null;
        private float m_currentCameraHeight = 0.0f;
        private float m_currentCameraLerpTime = 0.0f;
        private float m_previousCameraHeight = 0.0f;
        private float m_desiredCameraHeight = 0.0f;
        private bool m_isSlowWalking = false;
        private bool m_isGrounded = false;
        private float m_currentFallTime = 0.0f;
        private float m_currentFallDistance = 0.0f;
        private float m_currentFallVelocity = 0.0f;
        private float m_hasGroundedAdjustment = 0.0f;
        private bool m_addvelocity = false;
        private bool m_hasLeftHandInteractable = false;
        private bool m_hasRightHandInteractable = false;

        private const float m_gravityForce = 9.8f;

        private float vSpeed = 0.0f;

        private void Awake()
        {
            PlayerInventory.OnChangeInteractable += InteractableChange;
            m_playerUserControl = this.GetComponent<PlayerUserControl>();
            m_playerInventory = this.GetComponent<PlayerInventory>();
            m_playerID = Guid.NewGuid().ToString();
            m_characterController = this.GetComponent<CharacterController>();
            m_currentVelocity = Vector3.zero;
            StanceChange(Stance.Standing);
            m_camera.transform.localPosition = new Vector3(m_camera.transform.localPosition.x, m_cameraStandingHeight, m_camera.transform.localPosition.z);
            m_currentCameraHeight = m_cameraStandingHeight;
            m_desiredCameraHeight = m_cameraStandingHeight;
            m_previousCameraHeight = m_cameraStandingHeight;
        }

        private void Start()
        {
            m_guidePoint = new GameObject("GuidePoint");
            m_lastPosition = this.transform.position;
        }

        private void FixedUpdate()
        {
            GroundCheck();
            CrouchCheck();
            SprintCheck();
            InteractionCheck();
            Rotate(rotation);
            Move(movement);
            CameraLerp();
            PushInteractionCheck();
        }

        private void GroundCheck()
        {
            if(m_characterController.isGrounded == true)
            {
                vSpeed = 0;
            }
            vSpeed -= Time.fixedDeltaTime * (m_gravityForce / m_gravityMagnitude);
        }

        private void CrouchCheck()
        {
            if(PlayerUserControl.isCrouching == m_isCrouching)
            {
                return;
            }
            m_isCrouching = PlayerUserControl.isCrouching;
            if(m_isCrouching == true)
            {
                m_characterController.height = m_crouchingHeight;
                m_characterController.center = new Vector3(m_characterController.center.x, m_crouchingCenter, m_characterController.center.z);
                m_standingObject.SetActive(false);
                m_crouchedObject.SetActive(true);
                StanceChange(Stance.CrouchingStill);
            }
            else
            {
                m_characterController.height = m_standingHeight;
                m_characterController.center = new Vector3(m_characterController.center.x, m_standingCenter, m_characterController.center.z);
                m_standingObject.SetActive(true);
                m_crouchedObject.SetActive(false);
                StanceChange(Stance.Standing);
            }
        }

        private void StanceChange(Stance stance)
        {
            m_currentStance = stance;
            PlayerController.StanceValueChanged?.Invoke(m_currentStance);
        }

        private void InteractableChange(PlayerInventory.ToolPlacement placement, InteractableObject interactable)
        {
            if(interactable == null)
            {
                if(placement == PlayerInventory.ToolPlacement.LeftSide)
                {
                    PlayerUserControl.RemoveLeftPrimaryActions();
                }
                else if(placement == PlayerInventory.ToolPlacement.LeftSide)
                {
                    PlayerUserControl.RemoveRightPrimaryActions();
                }
                return;
            }
            if(interactable.objectPlacement == PlayerInventory.ToolPlacement.LeftSide)
            {
                PlayerUserControl.RemoveLeftPrimaryActions();
                PlayerUserControl.LeftHandPrimaryAction += interactable.Interaction;
            }
            else if(interactable.objectPlacement == PlayerInventory.ToolPlacement.RightSide)
            {
                PlayerUserControl.RemoveRightPrimaryActions();
                PlayerUserControl.RightHandPrimaryAction += interactable.Interaction;
            }
        }

        private void CameraLerp()
        {
            float desiredHeight = 0.0f;
            switch (m_currentStance)
            {
                case Stance.Standing:
                {
                    desiredHeight = m_cameraStandingHeight;
                    break;
                }
                case Stance.CrouchingMoving:
                {
                    desiredHeight = m_cameraCrouchingMovingHeight;
                    break;
                }
                case Stance.CrouchingStill:
                {
                    desiredHeight = m_cameraCrouchingStillHeight;
                    break;
                }
                default:
                {
                    Debug.LogError($"{m_currentStance} is not set, make sure to set it.");
                    break;
                }
            }
            if(m_desiredCameraHeight != desiredHeight)
            {
                m_previousCameraHeight = m_desiredCameraHeight;
                m_desiredCameraHeight = desiredHeight;
                float previousToDesiredDistance = Mathf.Abs(m_previousCameraHeight - m_desiredCameraHeight);
                float currentToPreviousDistance = Mathf.Abs(m_currentCameraHeight - m_previousCameraHeight);
                if(previousToDesiredDistance == 0.0f)
                {
                    Debug.LogError("Somehow we got 0.0f, fix this.");
                    return;
                }
                m_currentCameraLerpTime = currentToPreviousDistance / previousToDesiredDistance;
            }
            if (m_currentCameraLerpTime >= 1.0f)
                return;
            m_currentCameraLerpTime += Time.fixedDeltaTime * m_cameraLerpSpeed;
            if (m_currentCameraLerpTime > 1.0f)
                m_currentCameraLerpTime = 1.0f;
            m_currentCameraHeight = Mathf.Lerp(m_previousCameraHeight, m_desiredCameraHeight, m_currentCameraLerpTime);
            m_camera.transform.localPosition = new Vector3(m_camera.transform.localPosition.x, m_currentCameraHeight, m_camera.transform.localPosition.z);
        }

        private void InteractionCheck()
        {
            Ray ray = new Ray(m_camera.transform.position, m_camera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, m_interactionRange);
            RaycastHit raycastHit = new RaycastHit();
            bool hitCheck = false;
            float closestHit = 100.0f;
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerController player = hits[i].collider.gameObject.GetComponent<PlayerController>();
                if (player != null)
                    continue;

                Interactable isInteractable = hits[i].collider.gameObject.GetComponent<Interactable>();
                if (isInteractable != null)
                {
                    float distance = Vector3.Distance(hits[i].point, this.transform.position);
                    if (closestHit > distance)
                    {
                        hitCheck = true;
                        closestHit = distance;
                        raycastHit = hits[i];
                    }
                }
            }
            bool leftSelectedInteractable = m_playerInventory.leftSideTools.HasItemInHand();
            bool rightSelectedInteractable = m_playerInventory.rightSideTools.HasItemInHand();
            if (leftSelectedInteractable)
            {
                SetLeftHandInteractable(m_playerInventory.leftSideTools.SelectedInteractable(), false);
            }
            if (rightSelectedInteractable)
            {
                SetRightHandInteractable(m_playerInventory.rightSideTools.SelectedInteractable(), false);
            }

            if (m_playerInventory.leftSideTools.HasItemInHand() && m_playerInventory.rightSideTools.HasItemInHand())
            {
                return;
            }
            if (hitCheck == true)
            {
                Interactable interactable = raycastHit.collider.gameObject.GetComponent<Interactable>();

                switch(interactable)
                {
                    case DoorKnob knob:
                    {
                        if (currentInteractable == null)
                        {
                            m_cachedRaycast = raycastHit;
                            if(leftSelectedInteractable == false)
                            {
                                SetLeftHandInteractable(knob);
                            }
                            if (rightSelectedInteractable == false)
                            {
                                SetRightHandInteractable(knob);
                            }
                            UIManager.cursorUI.SetCursor(CursorUI.CursorType.Grab);
                        }
                    }
                    break;
                    case DoorObject doorObject:
                    {
                        if (doorObject.doorAssembly.HasOpened == true)
                        {
                            if (doorObject.ValidateTriangleIndex(raycastHit.triangleIndex, out DoorObject.DoorFaceType type))
                            {
                                if (currentInteractable == null)
                                {
                                    doorObject.SetInteraction(type);
                                }
                                if (doorObject.selectedDoorInteraction == DoorObject.DoorFaceType.Side)
                                {
                                    if (currentInteractable != null
                                       && doorObject.selectedDoorInteraction == type)
                                    {
                                        m_cachedRaycast = raycastHit;
                                    }
                                    else if (currentInteractable == null)
                                    {
                                        m_cachedRaycast = raycastHit;
                                        if(leftSelectedInteractable == false)
                                        {
                                            SetLeftHandInteractable(interactable);
                                        }
                                        if (rightSelectedInteractable == false)
                                        {
                                            SetRightHandInteractable(interactable);
                                        }
                                        UIManager.cursorUI.SetCursor(CursorUI.CursorType.Grab);
                                    }
                                }
                                else
                                {
                                    if (currentInteractable != null
                                       && doorObject.selectedDoorInteraction == type)
                                    {
                                        m_cachedRaycast = raycastHit;
                                    }
                                    else if (currentInteractable == null)
                                    {
                                        m_cachedRaycast = raycastHit;
                                        if(leftSelectedInteractable == false)
                                        {
                                            SetLeftHandInteractable(interactable);
                                        }
                                        if (rightSelectedInteractable == false)
                                        {
                                            SetRightHandInteractable(interactable);
                                        }
                                        UIManager.cursorUI.SetCursor(CursorUI.CursorType.Push);
                                    }
                                }
                            }
                            else
                            {
                                if (leftSelectedInteractable == false)
                                {
                                    SetLeftHandInteractable(interactable);
                                }
                                if (rightSelectedInteractable == false)
                                {
                                    SetRightHandInteractable(interactable);
                                }
                                UIManager.cursorUI.SetCursor(CursorUI.CursorType.Interact);
                            }
                        }
                        else
                        {
                            if (currentInteractable == null || currentInteractable is DoorObject)
                            {
                                if (leftSelectedInteractable == false)
                                {
                                    SetLeftHandInteractable(null);
                                }
                                if (rightSelectedInteractable == false)
                                {
                                    SetRightHandInteractable(null);
                                }
                                UIManager.cursorUI.SetCursor(CursorUI.CursorType.Interact);
                            }
                        }
                    }
                    break;
                    case LightSwitch lightSwitch:
                    {
                        if(currentInteractable == null)
                        {
                            if (leftSelectedInteractable == false)
                            {
                                SetLeftHandInteractable(interactable);
                            }
                            if (rightSelectedInteractable == false)
                            {
                                SetRightHandInteractable(interactable);
                            }
                            UIManager.cursorUI.SetCursor(CursorUI.CursorType.Active);
                        }
                    }
                    break;
                    case InteractableObject interactableObject:
                    {
                        if (currentInteractable == null)
                        {
                            if (leftSelectedInteractable == false)
                            {
                                SetLeftHandInteractable(interactable);
                            }
                            if (rightSelectedInteractable == false)
                            {
                                SetRightHandInteractable(interactable);
                            }
                            UIManager.cursorUI.SetCursor(CursorUI.CursorType.PickUp);
                        }
                    }
                    break;
                    default:
                    {
                        if (currentInteractable == null || currentInteractable is DoorObject)
                        {
                            if (leftSelectedInteractable == false)
                            {
                                SetLeftHandInteractable(null);
                            }
                            if (rightSelectedInteractable == false)
                            {
                                SetRightHandInteractable(null);
                            }
                            UIManager.cursorUI.SetCursor(CursorUI.CursorType.Interact);
                        }
                    }
                    break;
                }

            }
            else
            {
                if (currentInteractable != null && !(currentInteractable is LightSwitch))
                {
                    return;
                }
                if (leftSelectedInteractable == false)
                {
                    SetLeftHandInteractable(null);
                }
                if (rightSelectedInteractable == false)
                {
                    SetRightHandInteractable(null);
                }
                UIManager.cursorUI.SetCursor(CursorUI.CursorType.Interact);
            }
        }

        private void SprintCheck()
        {
            if (m_isExhausted == true)
            {
                m_currentExhaustionTimer -= Time.deltaTime;
                if (m_currentExhaustionTimer <= 0.0f)
                {
                    m_currentExhaustionTimer = 0.0f;
                    m_isExhausted = false;
                    m_currentSprintPercentage = 0.25f;
                    SprintValueChanged?.Invoke(m_currentSprintPercentage);
                }
            }
            else if (m_isSprinting == true)
            {
                m_currentSprintPercentage -= Time.deltaTime / m_sprintSpeedTime;
                if (m_currentSprintPercentage <= 0.0f)
                {
                    m_currentSprintPercentage = 0.0f;
                    m_isExhausted = true;
                    m_isSprinting = false;
                    m_currentExhaustionTimer = m_exhaustionTimer;
                }
                SprintValueChanged?.Invoke(m_currentSprintPercentage);
            }
            else
            {
                if (m_currentsprintInbetweenDelay <= 0.0f)
                {
                    m_currentSprintPercentage += Time.deltaTime / m_sprintRecoveryTimer;
                    if (m_currentSprintPercentage > 1.0f)
                    {
                        m_currentSprintPercentage = 1.0f;
                    }
                    SprintValueChanged?.Invoke(m_currentSprintPercentage);
                }
                else
                {
                    m_currentsprintInbetweenDelay -= Time.deltaTime;
                    if (m_currentsprintInbetweenDelay <= 0.0f)
                        m_currentsprintInbetweenDelay = 0.0f;
                }
            }
        }

        private void PushInteractionCheck()
        {
            if (currentInteractable != null || movement == Vector2.zero)
            {
                m_cachedPushedDoor = null;
                return;
            }
            Vector3 calculatedDirection = CalculateTransformDirection(movement);
            Vector3 point1 = transform.position + m_characterController.center;
            float height = (m_characterController.height / 2) - m_characterController.radius;
            Vector3 point2 = new Vector3(point1.x, point1.y + height, point1.z);
            point1 = new Vector3(point1.x, point1.y - height, point1.z);
            RaycastHit[] hits = Physics.CapsuleCastAll(point1, point2, m_characterController.radius, calculatedDirection, m_pushRange); //m_pushRange
            RaycastHit storedHit = new RaycastHit();
            DoorObject doorDetected = null;
            for(int i = 0; i < hits.Length; i++)
            {
                doorDetected = hits[i].collider.gameObject.GetComponent<DoorObject>();
                if (doorDetected != null)
                {
                    if(doorDetected.doorAssembly.HasOpened == false)
                    {
                        doorDetected = null;
                        break;
                    }
                    if(m_isPushingDoor == false)
                    {
                        storedHit = hits[i];
                        m_isPushingDoor = true;
                        currentDoorInteractable = doorDetected;
                    }
                    break;
                }
            }
            if (doorDetected == null)
            {
                m_isPushingDoor = false;
                currentDoorInteractable = null;
                m_cachedPushedDoor = null;
                return;
            }
            if (doorDetected.ValidateTriangleIndex(storedHit.triangleIndex, out DoorObject.DoorFaceType type))
            {
                doorDetected.SetInteraction(type);
            }
            if(doorDetected.selectedDoorInteraction == DoorObject.DoorFaceType.Side || doorDetected.selectedDoorInteraction == DoorObject.DoorFaceType.None)
            {
                m_cachedPushedDoor = null;
                m_isPushingDoor = false;
                currentDoorInteractable = null;
                return;
            }
            if(m_cachedPushedDoor == null)
            {
                m_cachedPushedDoor = doorDetected;
                m_guidePoint.transform.position = storedHit.point;
                m_guidePoint.transform.SetParent(doorDetected.transform);
                m_guidePoint.transform.localPosition = new Vector3(m_guidePoint.transform.localPosition.x, m_guidePoint.transform.localPosition.y, 0.0f);
                m_guidePoint.transform.parent = null;
                m_guidePoint.transform.position += calculatedDirection * m_pushRange;
            }
            else
            {
                if(m_cachedPushedDoor.selectedDoorInteraction != doorDetected.selectedDoorInteraction)
                {
                    m_cachedPushedDoor = null;
                    m_isPushingDoor = false;
                    currentDoorInteractable = null;
                    return;
                }
                else
                {
                    m_guidePoint.transform.position += m_lastPosition - this.transform.position;
                }
            }

            doorDetected.DoorInteraction(m_guidePoint.transform.position, DoorAssembly.DoorInteractionType.Opening, false);

        }

        private Vector3 CalculateTransformDirection(Vector2 movementInput)
        {
            Vector3 forward = this.transform.forward * (movement.y != 0.0f ? (movement.y < 0.0f ? -1.0f : 1.0f) : 0.0f);
            Vector3 right = this.transform.right * (movement.x != 0.0f ? (movement.x < 0.0f ? -1.0f : 1.0f) : 0.0f);
            return forward + right;
        }

        private void SetLeftHandInteractable(Interactable interactable, bool removeActions = true)
        {
            if (m_selectedLeftHandInteractable == interactable)
                return;

            currentInteractable = null;
            UIManager.cursorUI.ActivateCursor(false);
            if (removeActions == true)
            {
                PlayerUserControl.RemoveLeftPrimaryActions();
                PlayerUserControl.RemoveRightSecondaryActions();
            }
            m_selectedLeftHandInteractable = interactable;

            if (m_selectedLeftHandInteractable is DoorKnob)
            {
                m_playerUserControl.PrimaryLeftHandSingleClickEnabled(false);
                m_playerUserControl.SecondaryLeftHandSingleClickEnabled(false);
                if(m_playerInventory.leftSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.LeftHandPrimaryAction += DoorKnobInteraction;
                    PlayerUserControl.RightHandSecondaryAction += DoorPushInteraction;
                }
            }
            else if (m_selectedLeftHandInteractable is DoorObject)
            {
                m_playerUserControl.PrimaryLeftHandSingleClickEnabled(false);
                m_playerUserControl.SecondaryLeftHandSingleClickEnabled(false);
                if (m_playerInventory.leftSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.LeftHandPrimaryAction += DoorObjectInteraction;
                    PlayerUserControl.RightHandSecondaryAction += DoorObjectInteraction;
                }
            }
            else if (m_selectedLeftHandInteractable is LightSwitch)
            {
                m_playerUserControl.PrimaryLeftHandSingleClickEnabled(true);
                if (m_playerInventory.leftSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.LeftHandPrimaryAction += LightSwitchInteraction;
                }
            }
            else if(m_selectedLeftHandInteractable is InteractableObject)
            {
                m_playerUserControl.PrimaryLeftHandSingleClickEnabled(true);
                if (m_playerInventory.leftSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.LeftHandPrimaryAction += PickUpObject;
                }
            }
            if (m_selectedLeftHandInteractable == null)
            {
                UIManager.cursorUI.ActivateCursor(false);
            }
        }

        private void SetRightHandInteractable(Interactable interactable, bool removeActions = true)
        {
            if (m_selectedRightHandInteractable == interactable)
                return;

            currentInteractable = null;
            UIManager.cursorUI.ActivateCursor(false);
            if (removeActions == true)
            {
                PlayerUserControl.RemoveRightPrimaryActions();
                PlayerUserControl.RemoveLeftSecondaryActions();
            }
            m_selectedRightHandInteractable = interactable;

            if (m_selectedRightHandInteractable is DoorKnob)
            {
                m_playerUserControl.PrimaryRightHandSingleClickEnabled(false);
                m_playerUserControl.SecondaryRightHandSingleClickEnabled(false);
                if (m_playerInventory.rightSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.RightHandPrimaryAction += DoorKnobInteraction;
                    PlayerUserControl.LeftHandSecondaryAction += DoorPushInteraction;
                }
            }
            else if (m_selectedRightHandInteractable is DoorObject)
            {
                m_playerUserControl.PrimaryRightHandSingleClickEnabled(false);
                m_playerUserControl.SecondaryRightHandSingleClickEnabled(false);
                if (m_playerInventory.rightSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.RightHandPrimaryAction += DoorObjectInteraction;
                    PlayerUserControl.LeftHandSecondaryAction += DoorPushInteraction;
                }
            }
            else if (m_selectedRightHandInteractable is LightSwitch)
            {
                m_playerUserControl.PrimaryRightHandSingleClickEnabled(true);
                if (m_playerInventory.rightSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.RightHandPrimaryAction += LightSwitchInteraction;
                }
            }
            else if (m_selectedRightHandInteractable is InteractableObject)
            {
                m_playerUserControl.PrimaryRightHandSingleClickEnabled(true);
                if (m_playerInventory.rightSideTools.HasItemInHand() == false)
                {
                    PlayerUserControl.RightHandPrimaryAction += PickUpObject;
                }
            }
            if (m_selectedRightHandInteractable == null)
            {
                UIManager.cursorUI.ActivateCursor(false);
            }
        }

        private void DoorPushInteraction()
        {
            if (!Physics.Raycast(m_camera.transform.position, m_camera.transform.forward, out RaycastHit hit, m_interactionRange * 1.5f))
            {
                return;
            }
            Interactable interactable = hit.collider.gameObject.GetComponent<Interactable>();
            if (interactable == null)
                return;
            if (m_selectedLeftHandInteractable is DoorObject || m_selectedRightHandInteractable is DoorObject)
            {
                bool isLeftHand = m_selectedLeftHandInteractable is DoorObject;
                DoorObject doorObject = m_selectedLeftHandInteractable is DoorObject ? m_selectedLeftHandInteractable as DoorObject : m_selectedRightHandInteractable as DoorObject;
                if(doorObject.ValidateTriangleIndex(hit.triangleIndex, out DoorObject.DoorFaceType type))
                {
                    if (doorObject.selectedDoorInteraction != type)
                        return;
                }
                if(doorObject.selectedDoorInteraction == DoorObject.DoorFaceType.Front)
                {
                    doorObject.DoorInteraction(Vector3.zero, DoorAssembly.DoorInteractionType.PushOpen);
                    if (isLeftHand)
                    {
                        SetLeftHandInteractable(null);
                    }
                    else
                    {
                        SetRightHandInteractable(null);
                    }
                }
                else if(doorObject.selectedDoorInteraction == DoorObject.DoorFaceType.Back)
                {
                    doorObject.DoorInteraction(Vector3.zero, DoorAssembly.DoorInteractionType.PushClose);
                    if (isLeftHand)
                    {
                        SetLeftHandInteractable(null);
                    }
                    else
                    {
                        SetRightHandInteractable(null);
                    }
                }
            }
            else if(m_selectedLeftHandInteractable is DoorKnob || m_selectedRightHandInteractable is DoorKnob)
            {
                if(interactable is DoorKnob == false)
                {
                    return;
                }
                bool isLeftHand = m_selectedLeftHandInteractable is DoorKnob;
                DoorKnob doorKnob = m_selectedLeftHandInteractable is DoorKnob ? m_selectedLeftHandInteractable as DoorKnob : m_selectedRightHandInteractable as DoorKnob;
                if (doorKnob.isInnerKnob == false && ((DoorKnob)interactable).isInnerKnob == false)
                {
                    doorKnob.DoorInteraction(Vector3.zero, DoorAssembly.DoorInteractionType.PushOpen);
                    if (isLeftHand)
                    {
                        SetLeftHandInteractable(null);
                    }
                    else
                    {
                        SetRightHandInteractable(null);
                    }
                }
                else if (doorKnob.isInnerKnob == true && ((DoorKnob)interactable).isInnerKnob == true)
                {
                    doorKnob.DoorInteraction(Vector3.zero, DoorAssembly.DoorInteractionType.PushClose);
                    if (isLeftHand)
                    {
                        SetLeftHandInteractable(null);
                    }
                    else
                    {
                        SetRightHandInteractable(null);
                    }
                }
            }
        }

        private void LightSwitchInteraction()
        {
            UIManager.cursorUI.ActivateCursor(true);
            if (m_selectedLeftHandInteractable is LightSwitch)
            {
                ((LightSwitch)m_selectedLeftHandInteractable).Interact();
            }
            else
            {
                ((LightSwitch)m_selectedRightHandInteractable).Interact();
            }
        }

        private void DoorKnobInteraction()
        {
            Interactable selectedInteractable = m_selectedLeftHandInteractable is DoorKnob ? m_selectedLeftHandInteractable : m_selectedRightHandInteractable;
            UIManager.cursorUI.ActivateCursor(true);
            m_guidePoint.transform.position = this.transform.position + this.transform.forward * m_interactionRange;
            float guideDistance = Vector3.Distance(m_guidePoint.transform.position, selectedInteractable.transform.position);
            float playerDistance = Vector3.Distance(this.transform.position, selectedInteractable.transform.position);
            if(guideDistance > m_interactionRange * m_distanceGuide || playerDistance > m_interactionRange * m_distancePlayer)
            {
                if (m_selectedLeftHandInteractable is DoorKnob)
                {
                    SetLeftHandInteractable(null);
                }
                else
                {
                    SetRightHandInteractable(null);
                }
                return;
            }
            ((DoorKnob)selectedInteractable).DoorInteraction(m_guidePoint.transform.position, DoorAssembly.DoorInteractionType.Opening);
        }

        private void DoorObjectInteraction()
        {
            Interactable selectedInteractable = m_selectedLeftHandInteractable is DoorObject ? m_selectedLeftHandInteractable : m_selectedRightHandInteractable;
            UIManager.cursorUI.ActivateCursor(true);
            m_guidePoint.transform.position = this.transform.position + this.transform.forward * m_interactionRange;
            float guideDistance = Vector3.Distance(m_guidePoint.transform.position, m_cachedRaycast.point);
            float playerDistance = Vector3.Distance(this.transform.position, m_cachedRaycast.point);
            if (guideDistance > m_interactionRange * m_distanceGuide || playerDistance > m_interactionRange * m_distancePlayer)
            {
                if (m_selectedLeftHandInteractable is DoorObject)
                {
                    SetLeftHandInteractable(null);
                }
                else
                {
                    SetRightHandInteractable(null);
                }
                return;
            }
            ((DoorObject)selectedInteractable).DoorInteraction(m_guidePoint.transform.position, DoorAssembly.DoorInteractionType.Opening);
        }

        public void Move(Vector2 velocity)
        {
            m_isSlowWalking = PlayerUserControl.isSlowWalking;
            if (velocity == Vector2.zero && m_currentStance == Stance.CrouchingMoving)
            {
                StanceChange(Stance.CrouchingStill);
            }
            else if(velocity != Vector2.zero && m_currentStance == Stance.CrouchingStill)
            {
                StanceChange(Stance.CrouchingMoving);
            }

            if (m_isExhausted == false)
            {
                if(m_isSprinting && !PlayerUserControl.isSprinting)
                {
                    m_currentsprintInbetweenDelay = m_sprintinBetweenDelay;
                }
                m_isSprinting = PlayerUserControl.isSprinting;
            }
            else
            {
                m_isSprinting = false;
            }

            if(velocity == Vector2.zero)
            {
                Vector3 adjustedVelocity = Vector3.zero;
                adjustedVelocity = Vector3.Lerp(m_currentVelocity, Vector3.zero, m_drag);
                m_currentVelocity = adjustedVelocity * m_movementSpeed * Time.fixedDeltaTime;
            }
            else
            {
                float crouchingAdjustment = m_isCrouching == true ? m_crouchWalkingSpeed : 1.0f;
                m_currentVelocity = velocity * m_movementSpeed * Time.fixedDeltaTime;
                if (m_isSprinting == true)
                {
                    m_currentVelocity *= m_sprintSpeedModifier * crouchingAdjustment;
                }
                else if(m_isSlowWalking == true)
                {
                    m_currentVelocity *= m_slowWalkingSpeed;
                }
                else if(m_isCrouching == true)
                {
                    m_currentVelocity *= m_crouchWalkingSpeed;
                }
            }
            m_pushRange = Mathf.Max(Mathf.Abs(m_currentVelocity.x), Mathf.Abs(m_currentVelocity.y)) * m_movementSpeed;
            m_lastPosition = this.transform.position;
            ApplyGravity(m_currentVelocity, out Vector3 velocityWithGravity);
            m_characterController.Move((this.transform.right * velocityWithGravity.x) + (this.transform.up * velocityWithGravity.y) + (this.transform.forward * velocityWithGravity.z ));
        }

        private void ApplyGravity(Vector3 currentVelocity, out Vector3 velocityWithGravity)
        {
            Vector3 correctedVelocity = new Vector3(currentVelocity.x, vSpeed, currentVelocity.y);
            velocityWithGravity = correctedVelocity;
        }

        public void Rotate(Vector2 rotation)
        {
            m_xRotation -= rotation.y * m_turnSpeed * Time.fixedDeltaTime;
            m_xRotation = Mathf.Clamp(m_xRotation, -89.0f, 89.0f);

            m_camera.transform.localRotation = Quaternion.Euler(m_xRotation, 0.0f, 0.0f);
            this.transform.Rotate(Vector3.up * rotation.x * m_turnSpeed * Time.fixedDeltaTime);
        }

        public void ThrowObject()
        {
            InteractableObject interactable = Instantiate(m_throwableObjectPrefab);
            if (m_debugUnderHandThrow == true)
            {
                interactable.transform.position = this.transform.position + this.transform.forward + (Vector3.right * m_debugUnderHandThrowOffset);
                interactable.transform.rotation = this.transform.rotation;
            }
            else
            {
                interactable.transform.position = m_camera.transform.position + m_camera.transform.forward;
                interactable.transform.rotation = m_camera.transform.rotation;
            }
            interactable.Initialize(false);
        }

        public void PickUpObject()
        {
            if (PlayerUserControl.leftHandPrimaryDown == true)
            {
                if(m_playerInventory.AddTool(PlayerInventory.ToolPlacement.LeftSide, m_selectedLeftHandInteractable as InteractableObject) == true)
                {
                    Destroy(m_selectedLeftHandInteractable.gameObject);
                    SetLeftHandInteractable(null);
                }
                else
                {
                    Debug.Log("Cannot pick up, full inventory!");
                }
            }
            else if(PlayerUserControl.rightHandPrimaryDown == true)
            {
                if(m_playerInventory.AddTool(PlayerInventory.ToolPlacement.RightSide, m_selectedRightHandInteractable as InteractableObject) == true)
                {
                    Destroy(m_selectedRightHandInteractable.gameObject);
                    SetLeftHandInteractable(null);
                }
                else
                {
                    Debug.Log("Cannot pick up, full inventory!");
                }
            }
        }
    }
}