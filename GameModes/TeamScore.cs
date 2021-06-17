namespace UnboundLib.GameModes
{
    public struct TeamScore
    {
        public readonly int points;
        public readonly int rounds;

        public TeamScore(int points, int rounds)
        {
            this.points = points;
            this.rounds = rounds;
        }
    }
}
