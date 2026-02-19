namespace Game.Application.Ports
{
    public interface IRandomService
    {
        double NextDouble();
        int Next(int minInclusive, int maxExclusive);
        float NextFloat(float min, float max);
    }
}
