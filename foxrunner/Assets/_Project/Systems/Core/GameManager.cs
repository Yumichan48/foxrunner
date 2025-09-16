using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Core
{
    /// <summary>
    /// Central game manager that coordinates all game systems
    /// Implements singleton pattern with DontDestroyOnLoad
    /// Manages game state, system initialization, and cross-system communication
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Configuration
        [Header("=== SYSTEM REFERENCES ===")]
        [SerializeField] private PlayerDataSystem playerDataSystem;

        [Header("=== GAME STATE ===")]
        [SerializeField] private GameState currentGameState = GameState.MainMenu;
        [SerializeField] private bool isInitialized = false;
        #endregion

        #region Events
        public static System.Action<GameState, GameState> OnGameStateChanged;
        public static System.Action OnSystemsInitialized;
        #endregion

        #region Properties
        public GameState CurrentGameState => currentGameState;
        public bool IsInitialized => isInitialized;
        public PlayerDataSystem PlayerData => playerDataSystem;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        void Start()
        {
            ChangeGameState(GameState.MainMenu);
        }

        void Update()
        {
            if (!isInitialized) return;

            // Update systems that need per-frame updates
            playerDataSystem?.UpdateSystem();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                playerDataSystem?.SavePlayerData();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                playerDataSystem?.SavePlayerData();
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                playerDataSystem?.SavePlayerData();
            }
        }
        #endregion

        #region System Initialization
        private void InitializeSystems()
        {
            try
            {
                // Initialize existing systems only
                InitializePlayerDataSystem();

                isInitialized = true;
                OnSystemsInitialized?.Invoke();

                Debug.Log("[GameManager] Core systems initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Failed to initialize systems: {e.Message}");
                isInitialized = false;
            }
        }

        private void InitializePlayerDataSystem()
        {
            if (!playerDataSystem)
            {
                GameObject go = new GameObject("PlayerDataSystem");
                go.transform.SetParent(transform);
                playerDataSystem = go.AddComponent<PlayerDataSystem>();
            }
            playerDataSystem.Initialize();
        }
        #endregion

        #region Game State Management
        public void ChangeGameState(GameState newState)
        {
            if (currentGameState == newState) return;

            GameState previousState = currentGameState;
            currentGameState = newState;

            OnGameStateChanged?.Invoke(previousState, newState);

            Debug.Log($"[GameManager] Game state changed: {previousState} -> {newState}");

            HandleGameStateChange(previousState, newState);
        }

        private void HandleGameStateChange(GameState from, GameState to)
        {
            // Handle state-specific logic
            switch (to)
            {
                case GameState.MainMenu:
                    HandleMainMenuState();
                    break;
                case GameState.Playing:
                    HandlePlayingState();
                    break;
                case GameState.Paused:
                    HandlePausedState();
                    break;
                case GameState.GameOver:
                    HandleGameOverState();
                    break;
                case GameState.Loading:
                    HandleLoadingState();
                    break;
            }
        }

        private void HandleMainMenuState()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Entering Main Menu state");
        }

        private void HandlePlayingState()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Entering Playing state");
            playerDataSystem?.StartGameSession();
        }

        private void HandlePausedState()
        {
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Entering Paused state");
        }

        private void HandleGameOverState()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Entering Game Over state");
            playerDataSystem?.EndGameSession();
        }

        private void HandleLoadingState()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Entering Loading state");
        }
        #endregion

        #region Public API
        public void StartGame()
        {
            ChangeGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            ChangeGameState(GameState.Paused);
        }

        public void ResumeGame()
        {
            ChangeGameState(GameState.Playing);
        }

        public void GameOver()
        {
            ChangeGameState(GameState.GameOver);
        }

        public void ReturnToMainMenu()
        {
            ChangeGameState(GameState.MainMenu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion
    }

    /// <summary>
    /// Enumeration of all possible game states
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Loading,
        Settings,
        Equipment,
        Village,
        Companions,
        Crafting,
        Shop
    }
}