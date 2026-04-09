using Game.Domain.Items;

namespace Game.Application.Ports
{
    public interface IModCatalogProvider
    {
        bool TryGetEntry(string modId, out ModCatalogEntry entry);
    }
}
