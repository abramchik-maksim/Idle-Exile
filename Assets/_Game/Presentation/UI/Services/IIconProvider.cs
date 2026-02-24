using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Presentation.UI.Services
{
    public interface IIconProvider
    {
        UniTask<Sprite> LoadIconAsync(string address);
        void ReleaseIcon(string address);
    }
}
