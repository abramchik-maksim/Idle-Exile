using Game.Domain.Items;

namespace Game.Application.Ports
{
    public interface IItemAffixDisplayTextFormatter
    {
        string FormatRolledLine(RolledItemAffix affix);
    }
}
