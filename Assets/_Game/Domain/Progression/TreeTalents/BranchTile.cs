namespace Game.Domain.Progression.TreeTalents
{
    public sealed class BranchTile
    {
        public GridCoord Offset { get; }
        public BranchNode Node { get; }

        public BranchTile(GridCoord offset, BranchNode node)
        {
            Offset = offset;
            Node = node;
        }
    }
}
