namespace Game.Presentation.UI.Services
{
    public interface ITreeTalentsInputReader
    {
        void Update();
        int ConsumeRotationSteps();
        bool ConsumeCancelDrag();
    }
}
