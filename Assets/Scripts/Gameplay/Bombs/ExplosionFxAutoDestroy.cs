using UnityEngine;

// vida útil do fx da bomba
namespace IC.Gameplay.Bombs
{
    public class ExplosionFxAutoDestroy : MonoBehaviour
    {
        [SerializeField] private float lifeSeconds = 0.25f;
        private void OnEnable() => Destroy(gameObject, lifeSeconds);
    }
}
