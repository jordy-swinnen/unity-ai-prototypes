namespace Prototypes.Pong
{
    /// <summary>One number per player from -1 (toward -z) to +1 (toward +z).</summary>
    public readonly struct PongInput
    {
        public readonly float Player1;
        public readonly float Player2;

        public PongInput(float player1, float player2)
        {
            Player1 = player1;
            Player2 = player2;
        }
    }
}
