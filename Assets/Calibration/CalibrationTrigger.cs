using UnityEngine;

namespace IC.Calibration
{
    /// <summary>
    /// Coloque num GO com BoxCollider2D (Is Trigger = true) em cada extremidade
    /// do corredor de calibração. Quando o player entrar, notifica o CalibrationManager.
    /// </summary>
    public class CalibrationTrigger : MonoBehaviour
    {
        [SerializeField] private CalibrationStep direction;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            CalibrationManager.Instance?.RegisterDirectionPeak(direction);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor();
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }

        private Color GizmoColor() => direction switch
        {
            CalibrationStep.CalibratingRight => Color.red,
            CalibrationStep.CalibratingLeft  => Color.blue,
            CalibrationStep.CalibratingUp    => Color.green,
            CalibrationStep.CalibratingDown  => Color.yellow,
            _                                => Color.white
        };
    }
}
