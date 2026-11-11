using UnityEngine;

namespace Prototypes.DioramaTurn
{
    /// <summary>Holds the star above the Goal. Stage 2 shows a simple bright shape; the polish stage swaps in the sprite.</summary>
    public sealed class StarView : MonoBehaviour
    {
        [Tooltip("The star's own transform, built by the scene builder.")] public Transform star;

        public void Place(Cell goal)
        {
            if (star != null) star.position = RoomView.TopFace(goal) + Vector3.up * 0.65f;
        }
    }
}
