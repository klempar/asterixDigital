namespace Asterix.Core.Interfaces
{
    public interface IRandomSource
    {
        ulong Seed { get; }
        int NextInt(int exclusiveUpperBound);
        double NextDouble();
        byte[] GetState();
        IRandomSource Fork(ulong streamId);
    }
}
