using IC.Core.Utility;
using IC.Core;
using IC.Inputs;
using UnityEngine;

namespace IC.Gameplay.Movement
{
    // Movimento cardinal, célula por célula, usando o watchdog para não para não deixar ele travar depois de tentar se mover em uma célula ocupada por um sólido
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerGridMover : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridService2D gridService;
        [SerializeField] private LayerMask obstacleMask;   // ( Solid e Breakable )

        [Header("Movement")]
        [SerializeField] private float tilesPerSecond = 7f;       
        [SerializeField] private float arriveEpsilon = 0.001f;    

        [Header("Anti-stall (watchdog)")]
        [Tooltip("Mínimo de progresso esperado por frame de física; abaixo disso conta como 'sem progresso'.")]
        [SerializeField] private float minProgress = 0.0005f;
        [Tooltip("Quantos frames de física sem progresso até cancelar o passo atual.")]
        [SerializeField] private int stallFramesLimit = 3;

        private Rigidbody2D rb;
        private IInputSource input;

        private Vector3Int currentCell;
        private Vector3Int targetCell;
        private Vector3 targetCenter;
        private bool isMoving;

        // --- Watchdog
        private float lastDist;
        private int stalledFrames;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<IInputSource>();
        }

        private void Start()
        {
            if (!gridService && SceneContext.Instance != null)
            {
                gridService = SceneContext.Instance.Grid;
                obstacleMask = SceneContext.Instance.ObstacleMask;
            }

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Snap inicial
            gridService.SnapTransformCenter(transform);
            currentCell = gridService.WorldToCell(transform.position);
            targetCell = currentCell;
            targetCenter = gridService.CellCenterWorld(targetCell);
            rb.position = targetCenter;
            isMoving = false;

            lastDist = 0f;
            stalledFrames = 0;
        }

        private void Update()
        {
            if (!isMoving)
            {
                // Garantir movimentação cardinal 
                Vector2 dir = ToCardinal(input != null ? input.ReadMove() : Vector2.zero);
                if (dir != Vector2.zero)
                {
                    var candidate = currentCell + new Vector3Int((int)dir.x, (int)dir.y, 0);
                    if (!IsBlocked(candidate))
                    {
                        targetCell = candidate;
                        targetCenter = gridService.CellCenterWorld(targetCell);
                        isMoving = true;


                        lastDist = Vector2.Distance(rb.position, targetCenter);
                        stalledFrames = 0;
                    }

                }
                else
                {

                    var center = gridService.CellCenterWorld(currentCell);
                    if ((rb.position - (Vector2)center).sqrMagnitude > arriveEpsilon * arriveEpsilon)
                        rb.position = center;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isMoving) { rb.velocity = Vector2.zero; return; }

            // Move para a próxima célula mais próxima com base no input
            float speed = tilesPerSecond; 
            Vector2 next = Vector2.MoveTowards(rb.position, targetCenter, speed * Time.fixedDeltaTime);

            // --- Watchdog: mede a movimentação do player para decidir mais tarde para qual tile ele volta depois de colidir com algo
            float distNow = Vector2.Distance(rb.position, targetCenter);
            if (lastDist - distNow < minProgress)
            {
                stalledFrames++;
            }
            else
            {
                stalledFrames = 0;
            }
            lastDist = distNow;

            // Bateu na parede
            if (stalledFrames >= stallFramesLimit)
            {
                CancelStepSnapToCurrent();
                return;
            }

            rb.MovePosition(next);

            // Chegou?
            if ((rb.position - (Vector2)targetCenter).sqrMagnitude <= arriveEpsilon * arriveEpsilon)
            {
                rb.MovePosition(targetCenter);
                currentCell = targetCell;
                isMoving = false;
                rb.velocity = Vector2.zero;
            }
        }

        private void CancelStepSnapToCurrent()
        {
            var center = gridService.CellCenterWorld(currentCell);
            rb.MovePosition(center);
            isMoving = false;
            rb.velocity = Vector2.zero;
            stalledFrames = 0;
            lastDist = 0f;
        }

        private bool IsBlocked(Vector3Int cell)
        {
            var center = gridService.CellCenterWorld(cell);
            return Physics2D.OverlapPoint(center, obstacleMask) != null;
        }

        private Vector2 ToCardinal(Vector2 raw)
        {
            if (raw.sqrMagnitude < 0.25f) return Vector2.zero;
            return Mathf.Abs(raw.x) > Mathf.Abs(raw.y)
                ? new Vector2(Mathf.Sign(raw.x), 0f)
                : new Vector2(0f, Mathf.Sign(raw.y));
        }

        private void OnDrawGizmosSelected()
        {
            if (!gridService) return;
            Gizmos.color = Color.yellow;
            var c = Application.isPlaying ? currentCell : gridService.WorldToCell(transform.position);
            Gizmos.DrawWireCube(gridService.CellCenterWorld(c), Vector3.one * 0.25f);
        }
    }
}
