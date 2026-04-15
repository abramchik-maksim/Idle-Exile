using Game.Domain.Items;

namespace Game.Application.Ports
{
    public interface IDropQualityProvider
    {
        DropQualityBand GetBandForStage(int globalStage);
    }
}
