namespace Game.Domain.Progression.TreeTalents
{
    public sealed class BranchNode
    {
        public string Id { get; }
        public BranchNodeType NodeType { get; }
        public NodeAllianceType AllianceType { get; }
        public float Value { get; }
        public bool IsSocket => NodeType == BranchNodeType.Socket;

        public BranchNode(string id, BranchNodeType nodeType, NodeAllianceType allianceType, float value)
        {
            Id = id;
            NodeType = nodeType;
            AllianceType = allianceType;
            Value = value;
        }
    }
}
