namespace Prototypes.DioramaTurn
{
    public static class Stars
    {
        /// <summary>3 stars at par or better, 2 up to par plus the extra, else 1. There is no failing.</summary>
        public static int For(int turns, int par, int twoStarExtra)
        {
            if (turns <= par) return 3;
            if (turns <= par + twoStarExtra) return 2;
            return 1;
        }
    }
}
