using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.SaveSystem;

namespace Game.Application.Ports
{
    public interface ICharacterConfigProvider
    {
        IReadOnlyList<CharacterDefinition> GetAll();
        CharacterDefinition GetByClass(HeroItemClass heroClass);
        string GetPresetId(HeroItemClass heroClass);
    }
}
