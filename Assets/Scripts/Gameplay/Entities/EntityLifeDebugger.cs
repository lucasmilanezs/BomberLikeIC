using UnityEngine;

namespace IC.Gameplay
{
    /// <summary>
    /// Coloque no mesmo GO que EntityLife para visualizar HP em runtime.
    /// Desenha um label acima do objeto e uma barra de vida via Gizmos.
    /// Pode ser ligado/desligado pelo toggle do componente no Inspector.
    /// </summary>
    [RequireComponent(typeof(EntityLife))]
    public class EntityLifeDebugger : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private Color fullColor = Color.green;
        [SerializeField] private Color emptyColor = Color.red;
        [SerializeField] private float barWidth = 1f;
        [SerializeField] private float barHeight = 0.15f;

        private EntityLife _life;

        private void Awake()
        {
            _life = GetComponent<EntityLife>();
        }

        // Barra de vida e label via Gizmos (visível na Scene view)
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (_life == null) return;

            var origin = transform.position + worldOffset;
            float ratio = _life.MaxHealth > 0
                ? (float)_life.CurrentHealth / _life.MaxHealth
                : 0f;

            // Fundo cinza
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawCube(origin, new Vector3(barWidth, barHeight, 0f));

            // Barra colorida
            Gizmos.color = Color.Lerp(emptyColor, fullColor, ratio);
            float filledWidth = barWidth * ratio;
            var filledOrigin = origin - new Vector3((barWidth - filledWidth) * 0.5f, 0f, 0f);
            Gizmos.DrawCube(filledOrigin, new Vector3(filledWidth, barHeight, 0f));

#if UNITY_EDITOR
            // Label com valor numérico
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                origin + Vector3.up * 0.2f,
                $"{gameObject.name} HP: {_life.CurrentHealth}/{_life.MaxHealth}{(_life.IsDead ? " [DEAD]" : "")}"
            );
#endif
        }
    }
}
