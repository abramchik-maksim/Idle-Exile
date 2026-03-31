using System.Collections.Generic;
using Game.Application.Ports;

namespace Game.Application.Progression.TreeTalents
{
    public sealed class TreeUnlockProfileService
    {
        private readonly ITreeTalentsConfigProvider _config;

        public TreeUnlockProfileService(ITreeTalentsConfigProvider config)
        {
            _config = config;
        }

        public IReadOnlyList<int> GetHalfWidthsForLevel(int level)
        {
            return _config.GetUnlockHalfWidthsForLevel(level);
        }
    }
}
