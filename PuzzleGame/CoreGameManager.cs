using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PuzzleGame
{
    public class CoreGameManager : MonoBehaviour
    {
        public enum GameState
        {
            Start,
            Stop,
            Play,
            Completed
        };

        public static CoreGameManager Instance = null;

        [SerializeField] private TextMeshProUGUI m_timerText;
        [SerializeField] private Button m_playButton;
        [SerializeField] private Button m_stopButton;
        [SerializeField] private Button m_resetButton;
        [SerializeField] private Image m_colorSelectedBanner;

        public TileObject selectedSpawner { get; private set; }
        public static GameState currentGameState => Instance.m_currentGameState;

        public static int currentFloorFocus => Instance.m_currentFloorFocus;

        private List<PathFinderManager.PathNode> m_acquiredPath;
        private int m_pathIndex = 0;
        private float m_recordingTimeLimit;
        private float m_currentRecordingTime = 0.0f;
        private GameState m_currentGameState = GameState.Stop;
        private Dictionary<PuzzlePlayer.PlayerColorType, List<RecordedAction>> m_recordedActionsList = new Dictionary<PuzzlePlayer.PlayerColorType, List<RecordedAction>>();
        private bool m_recordingIdle = false;
        private float m_idleAmount = 0.0f;
        private int m_currentFloorFocus = 0;
        private Color m_inactiveColor = new Color(0.5f, 0.5f, 0.5f);

        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this.gameObject);
            }
            m_playButton.onClick.AddListener(OnPlayButton);
            m_stopButton.onClick.AddListener(OnStopButton);
            m_resetButton.onClick.AddListener(OnResetButton);
            m_playButton.interactable = false;
            m_stopButton.interactable = false;
            m_resetButton.interactable = false;
        }

        private void Start()
        {
            StartSession();
        }

        public static PuzzlePlayer GetSelectedPuzzlePlayer()
        {
            return ((SpawnerPlatform)Instance.selectedSpawner.tileAttribute).puzzlePlayer;
        }

        public static PuzzlePlayer GetPuzzlePlayer(PuzzlePlayer.PlayerColorType colorType)
        {
            List<PuzzlePlayer> playerList = TileGridManager.GetAllPlayers();
            PuzzlePlayer puzzlePlayer = null;

            for(int i = 0; i < playerList.Count; i++)
            {
                if (playerList [i].colorType != colorType)
                    continue;
                puzzlePlayer = playerList [i];
                break;
            }
            return puzzlePlayer;
        }

        public void StartSession()
        {
            m_recordingTimeLimit = GameConfigManager.GetCurrentLevel.time;
            SelectedSpawner(null);
            m_acquiredPath = null;
            m_pathIndex = 0;
            m_currentRecordingTime = 0.0f;
            m_recordingIdle = false;
            m_idleAmount = 0.0f;
            m_recordedActionsList = new Dictionary<PuzzlePlayer.PlayerColorType, List<RecordedAction>>();
            TileGridManager.Instance.CreateGrid();
            SetGameState(GameState.Start);
        }

        public void SetFloor(int floor)
        {
            m_currentFloorFocus = floor;
            TileGridManager.Instance.MoveCameraToFloor(m_currentFloorFocus);
            LevelLayerControlManager.Instance.AdjustLevelLayers();
            LevelLayerControlManager.Instance.floorController.AdjustFloorDisplay();
        }

        private void Update()
        {
            if (m_currentGameState != GameState.Play)
                return;
            
            SelectedPlayerControl();


            m_currentRecordingTime -= Time.deltaTime;
            if (m_currentRecordingTime <= 0.0f)
            {
                m_currentRecordingTime = 0.0f;
                SetGameState(GameState.Stop);
                StopLevel();
            }
            m_timerText.text = string.Format("{0:00.00}", m_currentRecordingTime);
        }

        private void SelectedPlayerControl()
        {
            if (m_acquiredPath == null)
            {
                m_idleAmount += Time.deltaTime;
                return;
            }
            SpawnerPlatform spawner = selectedSpawner.tileAttribute as SpawnerPlatform;
            if (spawner.puzzlePlayer.reachedDestination == false)
            {
                return;
            }
            if (m_pathIndex >= m_acquiredPath.Count)
            {
                m_acquiredPath = null;
                m_recordingIdle = true;
                m_idleAmount = 0.0f;
                spawner.puzzlePlayer.SetPlayerState(PuzzlePlayer.PlayerState.Idle);
                return;
            }
            m_pathIndex++;
            if (spawner.puzzlePlayer.currentPos == m_acquiredPath [m_acquiredPath.Count - m_pathIndex].currentPos)
            {
                m_pathIndex++;
            }
            if(m_recordingIdle == true)
            {
                Debug.Log(string.Format("Idle amount recorded for {0:00.00}s, has been added", m_idleAmount));
                m_recordingIdle = false;
                m_recordedActionsList [spawner.puzzlePlayer.colorType].Add(new RecordedAction(m_idleAmount));
            }
            TileObject tileObject = TileGridManager.GetTileObject(m_acquiredPath [m_acquiredPath.Count - m_pathIndex].currentPos.row, 
                                                                  m_acquiredPath [m_acquiredPath.Count - m_pathIndex].currentPos.column,
                                                                  m_acquiredPath [m_acquiredPath.Count - m_pathIndex].currentPos.floor);
            if(tileObject.tileAttribute != null &&
               tileObject.tileAttribute.canEnter == false)
            {
                m_acquiredPath = null;
                m_recordingIdle = true;
                m_idleAmount = 0.0f;
                spawner.puzzlePlayer.SetPlayerState(PuzzlePlayer.PlayerState.Idle);
                return;
            }
            Debug.Log("Recorded movement tile");
            m_recordedActionsList[spawner.puzzlePlayer.colorType].Add(new RecordedAction(m_acquiredPath [m_acquiredPath.Count - m_pathIndex].currentPos));
            spawner.puzzlePlayer.MoveToTile(m_acquiredPath [m_acquiredPath.Count - m_pathIndex].currentPos);
            spawner.puzzlePlayer.SetPlayerState(PuzzlePlayer.PlayerState.Moving);
        }

        private void PlayRecordedPlayers()
        {
            for(int i = 0; i < TileGridManager.Instance.SpawnerList.Count; i++)
            {
                if (((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.colorType == TileGridManager.Instance.SpawnerList[i].puzzlePlayer.colorType)
                {
                    continue;
                }
                TileGridManager.Instance.SpawnerList [i].puzzlePlayer.SetPlayerState(PuzzlePlayer.PlayerState.Recorded);
            }
        }

        private void StopLevel()
        {
            List<SpawnerPlatform> spawnerPlatforms = TileGridManager.Instance.SpawnerList;
            for(int i = 0; i < spawnerPlatforms.Count; i++)
            {
                spawnerPlatforms [i].ResetPlayer();
                spawnerPlatforms [i].puzzlePlayer.SetPlayerState(PuzzlePlayer.PlayerState.Inactive);
            }
            for (int i = 0; i < TileGridManager.Instance.TileObjectList.Length; i++)
            {
                foreach (TileObject tileObject in TileGridManager.Instance.TileObjectList[i])
                {
                    if (tileObject == null ||
                        tileObject.tileAttribute == null)
                    {
                        continue;
                    }
                    tileObject.tileAttribute.OnReset();
                }
            }
            LevelLayerControlManager.Instance.AdjustLevelLayers();
        }

        private void ClearRecordedRecords()
        {
            m_recordedActionsList = new Dictionary<PuzzlePlayer.PlayerColorType, List<RecordedAction>>();
            List<PuzzlePlayer> puzzlePlayers = TileGridManager.GetAllPlayers();
            for(int i = 0; i < puzzlePlayers.Count; i++)
            {
                puzzlePlayers[i].ClearRecordedMovement();
            }
        }

        private void OnPlayButton()
        {
            if(selectedSpawner == null)
            {
                m_playButton.interactable = false;
                return;
            }
            SetGameState(GameState.Play);
        }

        private void OnStopButton()
        {
            SetGameState(GameState.Stop);
            StopLevel();
        }

        private void OnResetButton()
        {
            ClearRecordedRecords();
            m_resetButton.interactable = false;

        }

        public void SetGameState(GameState state)
        {
            m_currentGameState = state;
            switch(state)
            {
                case GameState.Start:
                {
                    m_timerText.text = string.Format("{0:00.00}", m_recordingTimeLimit);
                    m_colorSelectedBanner.color = m_inactiveColor;
                    m_currentRecordingTime = m_recordingTimeLimit;
                    m_stopButton.interactable = false;
                    m_playButton.interactable = false;
                    m_resetButton.interactable = false;
                    for (int i = 0; i < TileGridManager.Instance.SpawnerList.Count; i++)
                    {
                        TileGridManager.Instance.SpawnerList [i].puzzlePlayer.ColliderEnabled(true);
                    }
                }
                break;
                case GameState.Stop:
                {
                    m_timerText.text = string.Format("{0:00.00}", m_recordingTimeLimit);
                    m_colorSelectedBanner.color = m_inactiveColor;
                    m_currentRecordingTime = m_recordingTimeLimit;
                    m_stopButton.interactable = false;
                    m_playButton.interactable = true;
                    m_resetButton.interactable = true;
                    if (m_recordingIdle == true)
                    {
                        m_recordingIdle = false;
                        m_recordedActionsList [((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.colorType].Add(new RecordedAction(m_idleAmount));
                    }
                    ((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.SetRecordedMovement(m_recordedActionsList[((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.colorType]);
                    ((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.SetPlayerState(PuzzlePlayer.PlayerState.Inactive);
                    m_acquiredPath = null;
                    for (int i = 0; i < TileGridManager.Instance.SpawnerList.Count; i++)
                    {
                        TileGridManager.Instance.SpawnerList [i].puzzlePlayer.ColliderEnabled(true);
                    }
                }
                break;
                case GameState.Play:
                {
                    bool foundPlayerOnRecord = false;
                    for(int i = 0; i < m_recordedActionsList.Keys.Count;i++)
                    {
                        if(m_recordedActionsList.ContainsKey(((SpawnerPlatform)(selectedSpawner.tileAttribute)).puzzlePlayer.colorType))
                        {
                            m_recordedActionsList[((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.colorType] = new List<RecordedAction>();
                            foundPlayerOnRecord = true;
                            ((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.SetPlayerLight(PuzzlePlayer.PlayerRecordState.None);
                            break;
                        }
                    }
                    if(foundPlayerOnRecord == false)
                    {
                        m_recordedActionsList.Add(((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.colorType, new List<RecordedAction>());
                    }
                    for(int i = 0; i < TileGridManager.Instance.SpawnerList.Count; i++)
                    {
                        TileGridManager.Instance.SpawnerList [i].puzzlePlayer.ColliderEnabled(false);
                    }
                    m_stopButton.interactable = true;
                    m_colorSelectedBanner.color = ColorPickerManager.Instance.SelectColor(((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.colorType).color;
                    m_playButton.interactable = false;
                    m_resetButton.interactable = false;
                    m_recordingIdle = true;
                    m_idleAmount = 0.0f;
                    PlayRecordedPlayers();
                }
                break;
                case GameState.Completed:
                {
                    List<PuzzlePlayer> playerList = TileGridManager.GetAllPlayers();
                    for(int i = 0; i < playerList.Count; i++)
                    {
                        playerList[i].Stop();
                    }
                }
                break;
            }
        }
        
        private void SetPlayerIndicators()
        {
            List<SpawnerPlatform> spawners = TileGridManager.Instance.SpawnerList;
            for (int i = 0; i < spawners.Count; i++)
            {
                spawners [i].puzzlePlayer.IsSelected(spawners[i].linkedTileObject == selectedSpawner);
            }
        }

        public void SelectedSpawner(TileObject tileObject)
        {
            selectedSpawner = tileObject;
            m_playButton.interactable = true;
            SetPlayerIndicators();
            if (tileObject == null)
            {
                SetCameraToPlayerController.SetPlayerView(null);
            }
            else
            {
                SetCameraToPlayerController.SetPlayerView(((SpawnerPlatform)tileObject.tileAttribute).puzzlePlayer.colorType);
            }
        }

        public void GamePointerPicker()
        {
            if (!Physics.Raycast(CameraManager.Instance.mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                return;
            }
            TileObject tileObject = hit.collider.GetComponent<TileObject>();
            if(tileObject == null)
            {
                PuzzlePlayer puzzlePlayer = hit.collider.GetComponent<PuzzlePlayer>();
                if (puzzlePlayer != null)
                {
                    if (m_currentGameState == GameState.Play)
                    {
                        puzzlePlayer.ColliderEnabled(false);
                        GamePointerPicker();
                        return;
                    }
                    else
                    {
                        SelectedSpawner(TileGridManager.Instance.GetPlayerSpawner(puzzlePlayer.colorType).linkedTileObject);
                        return;
                    }
                }
                else
                {
                    Debug.LogError($"{hit.collider.name} was hit, and shouldn't have been hit.");
                    return;
                }
            }
            if(tileObject.pos.floor != m_currentFloorFocus ||
               (selectedSpawner != null &&
               tileObject.pos.floor != ((SpawnerPlatform)selectedSpawner.tileAttribute).puzzlePlayer.currentPos.floor))
            {
                return;
            }
            if(tileObject.gridTile.tileType == GridTile.TileType.Spawner &&
                selectedSpawner != tileObject)
            {
                if(m_currentGameState == GameState.Play)
                {
                    return;
                }
                if (selectedSpawner == null ||
                    selectedSpawner != tileObject)
                {
                    SelectedSpawner(tileObject);
                    
                }
                else if(selectedSpawner == tileObject)
                {
                    Debug.Log("Selecting the same spawner tile.");
                }
            }
            else
            {
                if(selectedSpawner == null)
                {
                    Debug.Log("Select a spawner before selecting a tile");
                    return;
                }
                if(m_currentGameState != GameState.Play)
                {
                    Debug.Log("Must be in the Play State to move the player");
                    return;
                }
                SpawnerPlatform spawner = selectedSpawner.tileAttribute as SpawnerPlatform;
                if(tileObject.pos == spawner.puzzlePlayer.currentPos)
                {
                    return;
                }
                if(TileGridManager.GetTileObject(spawner.puzzlePlayer.currentPos.row, spawner.puzzlePlayer.currentPos.column, spawner.puzzlePlayer.currentPos.floor).tileAttribute != null &&
                    TileGridManager.GetTileObject(spawner.puzzlePlayer.currentPos.row, spawner.puzzlePlayer.currentPos.column, spawner.puzzlePlayer.currentPos.floor).tileAttribute.canEnter == false)
                {
                    Debug.Log("Cannot leave this tile position.");
                    return;
                }
                if(tileObject.tileAttribute != null &&
                    tileObject.tileAttribute.canEnter == false)
                {
                    Debug.Log("Tile cannot be entered.");
                    return;
                }
                Debug.Log($"{(PuzzlePlayer.PlayerColorType)spawner.id} is going to move to tile ({tileObject.pos.row},{tileObject.pos.column},floor:{tileObject.pos.floor})");
                List<PathFinderManager.PathNode> createdPath = PathFinderManager.FindPath(spawner.puzzlePlayer.currentPos, tileObject.pos, TileGridManager.Instance.gridDimensions);
                if (createdPath != null)
                {
                    m_acquiredPath = createdPath;
                }
                if (createdPath == null)
                {
                    Debug.LogError("Path cannot be found because it is not possible");
                }
                m_pathIndex = 0;
            }
        }
    }
}