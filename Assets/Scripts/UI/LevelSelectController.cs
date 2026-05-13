using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IC.Core;

namespace IC.UI
{
    [System.Serializable]
    public struct LevelEntry
    {
        public string sceneName;
        public string displayName;
        public Sprite preview;
    }

    /// <summary>
    /// Gera botões de fase dinamicamente a partir da lista configurada no Inspector.
    /// Coloque num GO na cena LevelSelect.
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField] private List<LevelEntry> levels = new();

        [Header("UI")]
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Transform buttonContainer;

        private void Start()
        {
            foreach (var level in levels)
            {
                var go = Instantiate(levelButtonPrefab, buttonContainer);

                // Label
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = level.displayName;

                // Preview
                var previewImg = go.transform.Find("Image_preview")?.GetComponent<Image>();
                if (previewImg != null && level.preview != null)
                    previewImg.sprite = level.preview;

                // Click
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var sceneName = level.sceneName;
                    btn.onClick.AddListener(() => GameManager.Instance.LoadLevel(sceneName));
                }
            }
        }

        public void OnBackPressed()
            => GameManager.Instance.GoToMenu();
    }
}