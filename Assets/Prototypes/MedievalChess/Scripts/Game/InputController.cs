using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Prototypes.MedievalChess
{
    /// <summary>
    /// Turns the mouse into squares: a ray from the camera through the pointer meets the board plane, and the
    /// square under that point is the one hovered or clicked. It uses the board plane and not the pieces, so a tall
    /// piece never hides the square behind it.
    /// </summary>
    public sealed class InputController : MonoBehaviour
    {
        public Camera cam;
        public GameController controller;

        static readonly Plane BoardPlane = new Plane(Vector3.up, Vector3.zero);

        /// <summary>The square under a point on the screen, or -1.</summary>
        public int PickSquare(Vector2 screenPoint)
        {
            var ray = cam.ScreenPointToRay(screenPoint);
            if (!BoardPlane.Raycast(ray, out float distance)) return -1;
            return BoardGeometry.SquareAt(ray.GetPoint(distance));
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || controller == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                controller.Hover(-1);
                return;
            }

            int square = PickSquare(mouse.position.ReadValue());
            controller.Hover(square);
            if (mouse.leftButton.wasPressedThisFrame) controller.Click(square);
        }
    }
}
