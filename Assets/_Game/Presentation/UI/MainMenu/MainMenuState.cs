namespace Game.Presentation.UI.MainMenu
{
    public sealed class MainMenuState
    {
        public MainMenuFlowMode Mode { get; set; } = MainMenuFlowMode.None;
        public int SelectedSlotIndex { get; set; } = -1;
    }
}
