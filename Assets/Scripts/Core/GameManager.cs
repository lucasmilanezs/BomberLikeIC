using UnityEngine;
using UnityEngine.SceneManagement;

namespace IC.Core
{
    /// <summary>
    /// Singleton leve de navegação. Não destrói entre cenas.
    /// Gerencia também o ciclo de vida da conexão com a plataforma de pressão.
    /// Acesse via GameManager.Instance de qualquer lugar.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string menuSceneName = "Menu";
        [SerializeField] private string levelSelectSceneName = "LevelSelect";
        [SerializeField] private string calibrationSceneName = "Calibration";

        private LerSensor _sensor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sensor = GetComponent<LerSensor>();
        }

        private void Start()
        {
            if (_sensor == null)
            {
                Debug.LogWarning("[GameManager] LerSensor não encontrado no GameManager GO.");
                return;
            }

            _sensor.ConectarPlataforma();

            if (_sensor.PJogCon == 1)
                Debug.Log("[GameManager] Plataforma conectada com sucesso.");
            else
                Debug.LogWarning("[GameManager] Plataforma não encontrada ou falhou ao conectar.");
        }

        private void OnApplicationQuit()
        {
            if (_sensor != null && _sensor.PJogCon == 1)
            {
                _sensor.ClosePlat();
                Debug.Log("[GameManager] Conexão com plataforma encerrada.");
            }
        }

        // --- Navegação ---

        public void GoToMenu()
            => SceneManager.LoadScene(menuSceneName);

        public void GoToLevelSelect()
            => SceneManager.LoadScene(levelSelectSceneName);

        public void GoToCalibration()
            => SceneManager.LoadScene(calibrationSceneName);

        public void LoadLevel(string sceneName)
            => SceneManager.LoadScene(sceneName);

        public void RestartCurrentLevel()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}