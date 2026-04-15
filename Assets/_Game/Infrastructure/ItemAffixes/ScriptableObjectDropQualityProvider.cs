using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Infrastructure.ItemAffixes
{
    public sealed class ScriptableObjectDropQualityProvider : IDropQualityProvider
    {
        private readonly List<DropQualityBand> _bands = new();
        private readonly DropQualityBand _fallback;

        public ScriptableObjectDropQualityProvider(DropQualityDatabaseSO database)
        {
            if (database?.bands != null)
            {
                foreach (var r in database.bands)
                {
                    _bands.Add(new DropQualityBand(
                        r.progressBand,
                        r.idleStageMin,
                        r.idleStageMax,
                        r.allowedTierMin,
                        r.allowedTierMax,
                        r.tierBias,
                        r.qualityMultiplier,
                        r.weightNormal,
                        r.weightMagic,
                        r.weightRare,
                        r.weightMythic));
                }
            }

            _fallback = _bands.Count > 0
                ? _bands[0]
                : new DropQualityBand(1, 1, 999, 1, 8, 0f, 1f, 35, 45, 15, 5);
        }

        public DropQualityBand GetBandForStage(int globalStage)
        {
            foreach (var band in _bands)
            {
                if (globalStage >= band.StageMin && globalStage <= band.StageMax)
                    return band;
            }

            return _fallback;
        }
    }
}
