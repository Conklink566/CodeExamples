using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame
{
    public class PuzzlePlayer : MonoBehaviour
    {
        public enum PlayerRecordState
        {
            None,
            Available,
            Play,
            Error,
            Completed
        };

        public enum PlayerState
        {
            Inactive,
            Idle,
            Moving,
            Recorded
        };


        public enum PlayerColorType
        {
            Red,
            Green,
            Blue,
            Magenta,
            Yellow,
            Cyan
        };

        [SerializeField] private MeshRenderer m_puzzlePlayerColorMesh;
        [SerializeField] private float m_timePerTileMovement = 0.5f;
        [SerializeField] private MeshRenderer m_recordLightDisplay;
        [SerializeField] private Material m_whiteLight;
        [SerializeField] private Material m_greenLight;
        [SerializeField] private Material m_redLight;
        [SerializeField] private Material m_yellowLight;
        [SerializeField] private SpriteRenderer m_selectedIndicator;
        [SerializeField] private float m_indicatorSpinSpeed = 90.0f;
        [SerializeField] private float m_rotationSpeed = 1.0f;
        [SerializeField] private Vector2 m_floatingDistance;
        [SerializeField] private float m_floatingSpeed = 1.0f;

        public PlayerColorType colorType { get; private set; }
        public PlayerState playerState { get; set;}

        public bool reachedDestination { get; private set; } = true;

        public GridCoordinates currentPos = GridCoordinates.New;

        private GridCoordinates m_destinationCoords = GridCoordinates.New;
        private GridCoordinates m_cachedCurrentPos = GridCoordinates.New;
        private float m_currentTransitionTime = 0.0f;

        private List<RecordedAction> m_cachedRecordedActions;
        private float m_currentRecordedIdle = 0.0f;
        private int m_recordActionIndex = 0;
        private bool m_recordActionCompleted = false;
        private bool m_recordActionCheck = false;
        private bool m_completedActionList = false;
        private bool m_isSelected = false;
        private float m_currentFloat = 0.5f;
        private bool m_floatingUp = false;
        private float m_floatingOffset = 0.0f;
        private ColorPickerManager.DualMaterial m_dualMaterial;

        private void Awake()
        {
            m_floatingOffset = m_puzzlePlayerColorMesh.gameObject.transform.localPosition.y;
        }

        private void Start()
        {
            SetPlayerState(PlayerState.Inactive);
            SetPlayerLight(PlayerRecordState.None);
            IsSelected(false);
        }

        private void Update()
        {
            CosmeticMovement();
            if(m_isSelected)
            {
                m_selectedIndicator.transform.Rotate(Vector3.forward * -m_indicatorSpinSpeed * Time.deltaTime);
            }

            switch (playerState)
            {
                case PlayerState.Idle:
                case PlayerState.Moving:
                {
                    StandardMovement();
                }
                break;
                case PlayerState.Recorded:
                {
                    RecordedMovement();
                }
                break;
                case PlayerState.Inactive:
                {

                }
                break;
            }
        }

        private void CosmeticMovement()
        {
            m_puzzlePlayerColorMesh.gameObject.transform.Rotate(Vector3.up * m_rotationSpeed * Time.deltaTime);
            m_currentFloat += Time.deltaTime * m_floatingSpeed * (m_floatingUp == true ?  1.0f : -1.0f);
            m_puzzlePlayerColorMesh.transform.localPosition = new Vector3(
                m_puzzlePlayerColorMesh.transform.localPosition.x,
                Mathf.Lerp(m_floatingDistance.x, m_floatingDistance.y, m_currentFloat) + m_floatingOffset,
                m_puzzlePlayerColorMesh.transform.localPosition.z);
            if(m_currentFloat >= 1.0f)
            {
                m_floatingUp = false;
                m_currentFloat = 1.0f;
            }
            else if(m_currentFloat <= 0.0f)
            {
                m_floatingUp = true;
                m_currentFloat = 0.0f;
            }
        }

        public void IsSelected(bool isSelected)
        {
            m_isSelected = isSelected;
            m_selectedIndicator.gameObject.SetActive(isSelected);
        }

        private void StandardMovement()
        {
            if (playerState == PlayerState.Idle)
            {
                return;
            }
            if (m_currentTransitionTime == 0.0f &&
                TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).tileAttribute != null)
            {
                TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).tileAttribute.OnTileExit(this);
            }
            if (m_timePerTileMovement <= 0.001f)
            {
                m_timePerTileMovement = 0.001f;
            }
            m_currentTransitionTime += Time.deltaTime / m_timePerTileMovement;
            Vector3 currentTransformPosition = new Vector3(TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).transform.position.x,
                                                           this.transform.position.y,
                                                           TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).transform.position.z);
            Vector3 destinationTransformPosition = new Vector3(TileGridManager.GetTileObject(m_destinationCoords.x, m_destinationCoords.y, m_cachedCurrentPos.floor).transform.position.x,
                                                           this.transform.position.y,
                                                           TileGridManager.GetTileObject(m_destinationCoords.x, m_destinationCoords.y, m_cachedCurrentPos.floor).transform.position.z);

            transform.position = Vector3.Lerp(currentTransformPosition, destinationTransformPosition, m_currentTransitionTime);
            if (m_currentTransitionTime >= 1.0f)
            {
                reachedDestination = true;
                if(TileGridManager.GetTileObject(m_destinationCoords.x, m_destinationCoords.y, m_cachedCurrentPos.floor).tileAttribute != null)
                {
                    TileGridManager.GetTileObject(m_destinationCoords.x, m_destinationCoords.y, m_cachedCurrentPos.floor).tileAttribute.OnTileEnter(this);
                }
                m_cachedCurrentPos = m_destinationCoords;
            }
        }

        private void RecordedMovement()
        {
            if(m_completedActionList == true)
            {
                return;
            }
            if(m_recordActionCompleted == true && m_completedActionList == false)
            {
                m_recordActionIndex++;
                m_recordActionCompleted = false;
                m_recordActionCheck = false;
            }
            if(m_recordActionIndex >= m_cachedRecordedActions.Count)
            {
                m_completedActionList = true;
                SetPlayerLight(PlayerRecordState.Completed);
                return;
            }
            if(m_cachedRecordedActions[m_recordActionIndex].recordType == RecordedAction.RecordType.IdleTime)
            {
                m_currentRecordedIdle += Time.deltaTime;
                if(m_currentRecordedIdle >= m_cachedRecordedActions [m_recordActionIndex].idleTime)
                {
                    m_recordActionCompleted = true;
                    m_currentRecordedIdle = 0;
                }
            }
            else if(m_cachedRecordedActions [m_recordActionIndex].recordType == RecordedAction.RecordType.TileMovement)
            {
                GridCoordinates recordedCoords = m_cachedRecordedActions [m_recordActionIndex].recordedTileMovement;
                if (m_recordActionCheck == false)
                {
                    if (Mathf.Abs(recordedCoords.x - m_cachedCurrentPos.x) > 1 ||
                       Mathf.Abs(recordedCoords.y - m_cachedCurrentPos.y) > 1 ||
                       recordedCoords.floor != m_cachedCurrentPos.floor)
                    {
                        m_completedActionList = true;
                        SetPlayerLight(PlayerRecordState.Error);
                        return;
                    }

                    if (TileGridManager.GetTileObject(recordedCoords.x, recordedCoords.y, recordedCoords.floor).tileAttribute != null &&
                       TileGridManager.GetTileObject(recordedCoords.x, recordedCoords.y, recordedCoords.floor).tileAttribute.canEnter == false)
                    {
                        m_completedActionList = true;
                        SetPlayerLight(PlayerRecordState.Error);
                        Debug.LogError("I can't enter a tile that won't let me!");
                        return;
                    }
                    if (TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor) == null)
                    {
                        m_completedActionList = true;
                        SetPlayerLight(PlayerRecordState.Error);
                        return;
                    }
                    if (m_currentTransitionTime == 0.0f &&
                       TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).tileAttribute != null)
                    {
                        if (TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).tileAttribute.canEnter == false)
                        {
                            m_completedActionList = true;
                            SetPlayerLight(PlayerRecordState.Error);
                            Debug.LogError("I can't leave the tile, even if I want to!");
                            return;
                        }
                        TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).tileAttribute.OnTileExit(this);
                    }
                    m_recordActionCheck = true;
                }
                if (m_timePerTileMovement <= 0.001f)
                {
                    m_timePerTileMovement = 0.001f;
                }
                m_currentTransitionTime += Time.deltaTime / m_timePerTileMovement;
                Vector3 currentTransformPosition = new Vector3(TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).transform.position.x,
                                                               this.transform.position.y,
                                                               TileGridManager.GetTileObject(m_cachedCurrentPos.x, m_cachedCurrentPos.y, m_cachedCurrentPos.floor).transform.position.z);
                Vector3 destinationTransformPosition = new Vector3(TileGridManager.GetTileObject(recordedCoords.x, recordedCoords.y, recordedCoords.floor).transform.position.x,
                                                               this.transform.position.y,
                                                               TileGridManager.GetTileObject(recordedCoords.x, recordedCoords.y, recordedCoords.floor).transform.position.z);

                transform.position = Vector3.Lerp(currentTransformPosition, destinationTransformPosition, m_currentTransitionTime);
                if (m_currentTransitionTime >= 1.0f)
                {
                    if(TileGridManager.GetTileObject(recordedCoords.x, recordedCoords.y, recordedCoords.floor).tileAttribute != null)
                    {
                        TileGridManager.GetTileObject(recordedCoords.x, recordedCoords.y, recordedCoords.floor).tileAttribute.OnTileEnter(this);
                    }
                    m_recordActionCompleted = true;
                    m_cachedCurrentPos = recordedCoords;
                    m_currentTransitionTime = 0.0f;
                    currentPos = recordedCoords;
                }
            }
        }

        public void SetRecordedMovement(List<RecordedAction> recordedActions)
        {
            m_cachedRecordedActions = recordedActions;
            SetPlayerLight(PlayerRecordState.Available);
        }

        public void ClearRecordedMovement()
        {
            m_cachedRecordedActions = null;
            SetPlayerLight(PlayerRecordState.None);
        }

        public void SetMaterial(PlayerColorType colorType)
        {
            this.colorType = colorType;
            m_dualMaterial = ColorPickerManager.colorDictionary[colorType];
            m_selectedIndicator.color = new Color(m_dualMaterial.standard.color.r, m_dualMaterial.standard.color.g, m_dualMaterial.standard.color.b, 0.85f);
        }

        public void MoveToTile(GridCoordinates coords)
        {
            reachedDestination = false;
            currentPos = coords;
            m_destinationCoords = coords;
            m_currentTransitionTime = 0;
        }

        public void SetPlayerPosition(GridCoordinates coords)
        {
            m_cachedCurrentPos = coords;
            currentPos = coords;
            reachedDestination = true;
        }

        public void ResetCosmetics()
        {
            m_floatingUp = true;
            m_currentFloat = 0.5f;
            m_puzzlePlayerColorMesh.transform.localEulerAngles = Vector3.zero;
        }

        public void Stop()
        {
            playerState = PlayerState.Inactive;
        }

        public void SetPlayerState(PlayerState state)
        {
            playerState = state;
            if(state == PlayerState.Recorded && m_cachedRecordedActions == null)
            {
                playerState = PlayerState.Inactive;
            }
            else if(state == PlayerState.Recorded)
            {
                m_currentTransitionTime = 0.0f;
                m_recordActionIndex = 0;
                m_recordActionCompleted = false;
                m_recordActionCheck = false;
                m_completedActionList = false;
                SetPlayerLight(PlayerRecordState.Play);
            }
            else if(state == PlayerState.Inactive && m_cachedRecordedActions != null)
            {
                SetPlayerLight(PlayerRecordState.Available);
            }
            else if(state == PlayerState.Idle ||
                    state == PlayerState.Moving)
            {
                SetPlayerLight(PlayerRecordState.None);
            }
        }

        public void SetPlayerLight(PlayerRecordState recordState)
        {
            switch(recordState)
            {
                case PlayerRecordState.Available:
                {
                    m_recordLightDisplay.gameObject.SetActive(true);
                    m_recordLightDisplay.material = m_whiteLight;
                }
                break;
                case PlayerRecordState.Completed:
                {
                    m_recordLightDisplay.gameObject.SetActive(true);
                    m_recordLightDisplay.material = m_yellowLight;
                }
                break;
                case PlayerRecordState.Error:
                {
                    m_recordLightDisplay.gameObject.SetActive(true);
                    m_recordLightDisplay.material = m_redLight;
                }
                break;
                case PlayerRecordState.None:
                {
                    m_recordLightDisplay.gameObject.SetActive(false);
                }
                break;
                case PlayerRecordState.Play:
                {
                    m_recordLightDisplay.gameObject.SetActive(true);
                    m_recordLightDisplay.material = m_greenLight;
                }
                break;
            }
        }

        public void ShowPlayer()
        {
            m_recordLightDisplay.enabled = true;
            m_puzzlePlayerColorMesh.enabled = true;
            m_puzzlePlayerColorMesh.material = m_dualMaterial.standard;
        }

        public void DimPlayer()
        {
            m_recordLightDisplay.enabled = true;
            m_puzzlePlayerColorMesh.enabled = true;
            m_puzzlePlayerColorMesh.material = m_dualMaterial.dim;
        }

        public void HidePlayer()
        {
            m_recordLightDisplay.enabled = false;
            m_puzzlePlayerColorMesh.enabled = false;
        }
    }
}