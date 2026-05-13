using UnityEngine;
using IC.Core;

namespace IC.UI
{
    /// <summary>
    /// Coloque num GO na cena Menu.
    /// Cada botão do Canvas chama os métodos públicos deste script.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        public void OnPlayPressed()
            => GameManager.Instance.GoToLevelSelect();

        public void OnSettingsPressed()
            => GameManager.Instance.LoadLevel("Settings");

        public void OnQuitPressed()
            => GameManager.Instance.QuitGame();

        public void OnCalibratePressed()
            => GameManager.Instance.GoToCalibration();
    }
}
