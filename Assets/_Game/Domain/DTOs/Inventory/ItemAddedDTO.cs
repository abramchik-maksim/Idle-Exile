namespace Game.Domain.DTOs.Inventory
{
    public readonly struct ItemAddedDTO
    {
        public string ItemUid { get; }
        public string DefinitionId { get; }
        public ItemAddedDTO(string itemUid, string definitionId)
        {
            ItemUid = itemUid;
            DefinitionId = definitionId;
        }
    }
}
