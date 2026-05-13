using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IC.Core;

namespace IC.UI
{
    /// <summary>
    /// Coloque num GO na cena Settings (ou no painel de settings do Menu).
    /// Lê e escreve nos campos do LerSensor em runtime.
    /// </summary>
    public class PlatformSettingsUI : MonoBehaviour
    {
        [Header("Sensor Ref")]
        [SerializeField] private LerSensor sensor;

        [Header("Campos de configuração")]
        [SerializeField] private TMP_InputField portField;
        [SerializeField] private TMP_InputField baudField;
        [SerializeField] private TMP_InputField xDeadZoneField;
        [SerializeField] private TMP_InputField yDeadZoneField;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusLabel;

        private void OnEnable()
        {
            // Popula os campos com os valores atuais do sensor
            if (sensor == null) return;

            portField.text = sensor.PortJog;
            baudField.text = sensor.BaudJog.ToString();
            xDeadZoneField.text = sensor.xDeadZone.ToString();
            yDeadZoneField.text = sensor.yDeadZone.ToString();
        }

        // --- Botões ---

        public void OnConnectPressed()
        {
            if (sensor == null) return;

            ApplyFields();
            sensor.ConectarPlataforma();
            SetStatus(sensor.PJogCon == 1 ? "Conectado!" : "Falha na conexão.");
        }

        public void OnDisconnectPressed()
        {
            if (sensor == null) return;
            sensor.ClosePlat();
            SetStatus("Desconectado.");
        }

        public void OnCalibratePressed()
        {
            if (sensor == null) return;
            sensor.CalibrarSensores();
            SetStatus("Calibrado!");
        }

        public void OnSavePressed()
        {
            ApplyFields();
            SetStatus("Configurações salvas.");
        }

        public void OnBackPressed()
            => GameManager.Instance.GoToMenu();

        // --- Helpers ---

        private void ApplyFields()
        {
            if (sensor == null) return;

            sensor.PortJog = portField.text;

            if (int.TryParse(baudField.text, out int baud))
                sensor.BaudJog = baud;

            if (int.TryParse(xDeadZoneField.text, out int xDz))
                sensor.xDeadZone = xDz;

            if (int.TryParse(yDeadZoneField.text, out int yDz))
                sensor.yDeadZone = yDz;
        }

        private void SetStatus(string msg)
        {
            if (statusLabel != null)
                statusLabel.text = msg;
        }
    }
}
