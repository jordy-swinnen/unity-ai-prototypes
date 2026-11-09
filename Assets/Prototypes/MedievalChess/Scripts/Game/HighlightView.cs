using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.MedievalChess
{
    public enum RingKind
    {
        Selection,
        Move,
        Capture,
        LastMove,
        Check,
        Hover,
    }

    /// <summary>
    /// Flat rings on squares: the selected piece, where it can go (captures in red), the last move, a king in
    /// check and the square under the mouse. The rings are copies of one template, reused from a pool.
    /// </summary>
    public sealed class HighlightView : MonoBehaviour
    {
        public GameObject ringTemplate;
        public MedievalChessSettings settings;
        public Material selectionMaterial, moveMaterial, captureMaterial, lastMoveMaterial, checkMaterial, hoverMaterial;

        readonly Dictionary<RingKind, List<GameObject>> active = new Dictionary<RingKind, List<GameObject>>();
        readonly Dictionary<RingKind, List<int>> squares = new Dictionary<RingKind, List<int>>();
        readonly Stack<GameObject> pool = new Stack<GameObject>();

        void Awake()
        {
            foreach (RingKind kind in System.Enum.GetValues(typeof(RingKind)))
            {
                active[kind] = new List<GameObject>();
                squares[kind] = new List<int>();
            }
        }

        public IReadOnlyList<int> Squares(RingKind kind) => squares[kind];
        public int Count(RingKind kind) => active[kind].Count;

        public void ShowSelection(int square) => Replace(RingKind.Selection, new[] { square });

        public void ShowMoves(IEnumerable<int> quiet, IEnumerable<int> captures)
        {
            Replace(RingKind.Move, quiet);
            Replace(RingKind.Capture, captures);
        }

        public void ShowLastMove(int from, int to) => Replace(RingKind.LastMove, new[] { from, to });
        public void ShowCheck(int kingSquare) => Replace(RingKind.Check, new[] { kingSquare });
        public void HideCheck() => Replace(RingKind.Check, new int[0]);
        public void ShowHover(int square) => Replace(RingKind.Hover, square >= 0 ? new[] { square } : new int[0]);

        /// <summary>Hides the selection and the move rings (the last move and a check stay).</summary>
        public void ClearSelection()
        {
            Replace(RingKind.Selection, new int[0]);
            Replace(RingKind.Move, new int[0]);
            Replace(RingKind.Capture, new int[0]);
        }

        public void ClearAll()
        {
            foreach (RingKind kind in System.Enum.GetValues(typeof(RingKind))) Replace(kind, new int[0]);
        }

        void Replace(RingKind kind, IEnumerable<int> newSquares)
        {
            foreach (var ring in active[kind])
            {
                ring.SetActive(false);
                pool.Push(ring);
            }
            active[kind].Clear();
            squares[kind].Clear();
            if (newSquares == null) return;

            foreach (int sq in newSquares)
            {
                var ring = pool.Count > 0 ? pool.Pop() : Instantiate(ringTemplate, transform);
                ring.name = $"Ring {kind} {Squares_Name(sq)}";
                var size = settings.game.ringSize;
                // the top ring is drawn a little higher, so overlapping rings (a selection over the last move) both show
                float lift = settings.game.ringHeight * (1 + (int)kind * 0.5f);
                ring.transform.position = BoardGeometry.Centre(sq) + Vector3.up * lift;
                ring.transform.localScale = new Vector3(size, 0.01f, size);
                ring.GetComponent<Renderer>().sharedMaterial = MaterialFor(kind);
                ring.SetActive(true);
                active[kind].Add(ring);
                squares[kind].Add(sq);
            }
        }

        static string Squares_Name(int sq) => Prototypes.MedievalChess.Squares.Name(sq);

        Material MaterialFor(RingKind kind)
        {
            switch (kind)
            {
                case RingKind.Selection: return selectionMaterial;
                case RingKind.Move: return moveMaterial;
                case RingKind.Capture: return captureMaterial;
                case RingKind.LastMove: return lastMoveMaterial;
                case RingKind.Check: return checkMaterial;
                default: return hoverMaterial;
            }
        }
    }
}
