using UnityEngine;
using IC.Core;

namespace IC.Gameplay
{
    /// <summary>
    /// Coloque no Player prefab junto com EntityLife.
    /// Ao morrer, pausa o jogo e exibe o painel de morte.
    /// O painel tem botões que chamam GameManager diretamente.
    /// </summary>
    [RequireComponent(typeof(EntityLife))]
    public class PlayerDeath : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Painel de Game Over. Ative/desative via este script.")]
        [SerializeField] private GameObject deathPanel;

        private EntityLife _life;

        private void Awake()
        {
            _life = GetComponent<EntityLife>();
        }

        private void OnEnable()
        {
            _life.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _life.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            Time.timeScale = 0f; // pausa o jogo

            if (deathPanel != null)
                deathPanel.SetActive(true);
            else
                Debug.LogWarning("[PlayerDeath] Death panel não configurado.");
        }

        // --- Chamados pelos botões do painel ---

        public void OnRestartPressed()
        {
            Time.timeScale = 1f;
            GameManager.Instance.RestartCurrentLevel();
        }

        public void OnMenuPressed()
        {
            Time.timeScale = 1f;
            GameManager.Instance.GoToMenu();
        }
    }
}