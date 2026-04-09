using Game.Domain.Items;

namespace Game.Application.Ports
{
    public interface IHeroItemClassProvider
    {
        HeroItemClass GetHeroItemClass();
    }
}
