namespace MyDefense.Shared.Contracts
{
    public interface IAlienAttackSnapshotConsumer
    {
        void ApplyAttackSnapshot(AlienAttackSnapshot snapshot);
    }
}
