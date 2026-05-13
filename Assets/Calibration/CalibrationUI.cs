using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IC.Calibration;
using IC.Core;

namespace IC.UI
{
    /// <summary>
    /// Conecta os eventos do CalibrationManager à UI da cena de calibração.
    /// A UI controla a visibilidade e o comportamento do botão de confirmação.
    /// </summary>
    public class CalibrationUI : MonoBehaviour
    {
        [Header("Geral")]
        [SerializeField] private TextMeshProUGUI textStep;
        [SerializeField] private TextMeshProUGUI textInstruction;
        [SerializeField] private Slider sliderProgress;
        [SerializeField] private GameObject buttonConfirm;
        [SerializeField] private TextMeshProUGUI buttonConfirmLabel;
        [SerializeField] private GameObject buttonSkip;

        [Header("Painel de Perfil")]
        [SerializeField] private GameObject panelProfile;
        [SerializeField] private TMP_InputField inputFieldName;
        [SerializeField] private Transform profileListContent;
        [SerializeField] private GameObject profileButtonPrefab;

        private static readonly string[] StepLabels = new[]
        {
            "Passo 1 de 7 — Selecione ou crie um perfil",
            "Passo 2 de 7 — Calibração de zero",
            "Passo 3 de 7 — Calibração de baseline",
            "Passo 4 de 7 — Mova-se para a DIREITA",
            "Passo 5 de 7 — Mova-se para a ESQUERDA",
            "Passo 6 de 7 — Mova-se para CIMA",
            "Passo 7 de 7 — Mova-se para BAIXO",
            "Calibração concluída!"
        };

        private static readonly string[] StepInstructions = new[]
        {
            "Selecione um perfil existente ou crie um novo.",
            "Aguarde — medindo plataforma vazia...",
            "Peça ao paciente que suba e fique parado. Pressione 'Estou pronto' quando estiver.",
            "Mova-se para a DIREITA até o fim do corredor.",
            "Mova-se para a ESQUERDA até o fim do corredor.",
            "Mova-se para CIMA até o fim do corredor.",
            "Mova-se para BAIXO até o fim do corredor.",
            "Perfil salvo com sucesso! Você já pode jogar."
        };

        private CalibrationStep _currentStep;

        private void Start()
        {
            if (CalibrationManager.Instance == null)
            {
                Debug.LogError("[CalibrationUI] CalibrationManager.Instance é nulo.");
                return;
            }

            CalibrationManager.Instance.OnStepChanged += HandleStepChanged;
            CalibrationManager.Instance.OnProgressChanged += HandleProgressChanged;
            CalibrationManager.Instance.OnCalibrationCompleted += HandleCompleted;
            CalibrationManager.Instance.OnCardinalReached += HandleCardinalReached;

            HandleStepChanged(CalibrationManager.Instance.CurrentStep);
            PopulateProfileList();
        }

        private void OnDestroy()
        {
            if (CalibrationManager.Instance == null) return;
            CalibrationManager.Instance.OnStepChanged -= HandleStepChanged;
            CalibrationManager.Instance.OnProgressChanged -= HandleProgressChanged;
            CalibrationManager.Instance.OnCalibrationCompleted -= HandleCompleted;
            CalibrationManager.Instance.OnCardinalReached -= HandleCardinalReached;
        }

        // =========================================================
        //  Handlers de evento
        // =========================================================

        private void HandleStepChanged(CalibrationStep step)
        {
            _currentStep = step;
            int index = (int)step;

            if (textStep != null)
                textStep.text = StepLabels[index];

            if (textInstruction != null)
                textInstruction.text = StepInstructions[index];

            if (sliderProgress != null)
                sliderProgress.value = 0f;

            // Painel de perfil só no primeiro passo
            if (panelProfile != null)
                panelProfile.SetActive(step == CalibrationStep.SelectProfile);

            // Slider só nas coletas automáticas
            bool isAutoCollection = step == CalibrationStep.CollectingZero ||
                                    step == CalibrationStep.CollectingBaseline;
            if (sliderProgress != null)
                sliderProgress.gameObject.SetActive(isAutoCollection);

            // Botão confirm — só no baseline inicialmente
            // Nos steps cardinais aparece após o trigger (HandleCardinalReached)
            bool showConfirm = step == CalibrationStep.CollectingBaseline;
            SetConfirmButton(showConfirm, "Estou pronto");

            // Skip em todos exceto SelectProfile e Completed
            bool showSkip = step != CalibrationStep.SelectProfile &&
                            step != CalibrationStep.Completed;
            if (buttonSkip != null)
                buttonSkip.SetActive(showSkip);
        }

        private void HandleProgressChanged(float progress)
        {
            if (sliderProgress != null)
                sliderProgress.value = progress;
        }

        private void HandleCompleted()
        {
            SetConfirmButton(false, "");
            if (buttonSkip != null) buttonSkip.SetActive(false);
            StartCoroutine(ReturnToMenuAfterDelay(2f));
        }

        /// <summary>
        /// Chamado quando o player chega no trigger cardinal.
        /// A UI mostra o botão "Pronto" para o profissional confirmar.
        /// </summary>
        private void HandleCardinalReached()
        {
            SetConfirmButton(true, "Pronto");

            if (textInstruction != null)
                textInstruction.text = "Ótimo! Pressione 'Pronto' quando o paciente voltar ao centro.";
        }

        private void SetConfirmButton(bool active, string label)
        {
            if (buttonConfirm != null)
                buttonConfirm.SetActive(active);

            if (buttonConfirmLabel != null && !string.IsNullOrEmpty(label))
                buttonConfirmLabel.text = label;
        }

        private IEnumerator ReturnToMenuAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            GameManager.Instance.GoToMenu();
        }

        // =========================================================
        //  Botões — chamados pelo OnClick no Inspector
        // =========================================================

        public void OnConfirmPressed()
        {
            if (_currentStep == CalibrationStep.CollectingBaseline)
            {
                SetConfirmButton(false, "");
                CalibrationManager.Instance.ConfirmBaseline();
            }
            else
            {
                // Step cardinal — confirma e respawna
                SetConfirmButton(false, "");
                CalibrationManager.Instance.ConfirmCardinal();
            }
        }

        // =========================================================
        //  Perfis
        // =========================================================

        public void OnNewProfilePressed()
        {
            if (inputFieldName == null) return;

            var name = inputFieldName.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                if (textInstruction != null)
                    textInstruction.text = "Digite um nome para o perfil.";
                return;
            }

            var profile = SensorProfileRepository.Instance.CreateAndSave(name);
            CalibrationManager.Instance.SelectProfile(profile);
        }

        private void PopulateProfileList()
        {
            if (profileListContent == null || profileButtonPrefab == null) return;

            foreach (Transform child in profileListContent)
                Destroy(child.gameObject);

            var profiles = SensorProfileRepository.Instance.LoadAll();

            foreach (var profile in profiles)
            {
                var go = Instantiate(profileButtonPrefab, profileListContent);

                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = profile.patientName;

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var captured = profile;
                    btn.onClick.AddListener(() =>
                    {
                        SensorProfileRepository.Instance.SetActive(captured);
                        GameManager.Instance.GoToMenu();
                    });
                }
            }
        }
    }
}