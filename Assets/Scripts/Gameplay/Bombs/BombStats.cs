using UnityEngine;

namespace IC.Gameplay.Bombs
{
    [CreateAssetMenu(fileName = "BombStats", menuName = "IC/Bombs/BombStats")]
    public class BombStats : ScriptableObject
    {
        [Min(1)] public int range = 3;           // quantos tiles a chama percorre por direção
        [Min(0.1f)] public float fuseSeconds = 2f;
        [Min(1)] public int maxSimultaneous = 1; // quantas bombas ativas ao mesmo tempo
        [Min(0.05f)] public float explosionFxSeconds = 0.25f; // duração do FX
    }
}
