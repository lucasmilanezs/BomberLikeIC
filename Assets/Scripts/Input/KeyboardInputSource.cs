using UnityEngine;
using UnityEngine.InputSystem;

namespace IC.Inputs
{
    public interface IInputSource
    {
        Vector2 ReadMove();
        bool ConsumePlaceBombPressed();
    }

    /// Lê as Actions do Input System (teclado para testagem, depois serve p/ device externo).
    public class KeyboardInputSource : MonoBehaviour, IInputSource
    {
        [Header("Input Actions (References)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference placeBombAction;

        private bool placePressed;

        private void OnEnable()
        {
            if (moveAction) moveAction.action.Enable();
            if (placeBombAction)
            {
                placeBombAction.action.Enable();
                placeBombAction.action.performed += OnPlace;
            }
        }
        private void OnDisable()
        {
            if (placeBombAction)
                placeBombAction.action.performed -= OnPlace;
        }
        private void OnPlace(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) placePressed = true;
        }

        public Vector2 ReadMove() => moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        public bool ConsumePlaceBombPressed()
        {
            if (!placePressed) return false;
            placePressed = false;
            return true;
        }
    }
}
