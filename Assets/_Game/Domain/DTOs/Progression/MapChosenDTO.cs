namespace Game.Domain.DTOs.Progression
{
    public readonly struct MapChosenDTO
    {
        public int ChosenOptionIndex { get; }

        public MapChosenDTO(int chosenOptionIndex)
        {
            ChosenOptionIndex = chosenOptionIndex;
        }
    }
}
