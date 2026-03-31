using Game.Domain.Progression.TreeTalents;

namespace Game.Application.Ports
{
    public interface ITreeTalentsRepository
    {
        void Save(TreeTalentsState state);
        TreeTalentsState Load();
    }
}
