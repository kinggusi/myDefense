namespace MyDefense.Shared.Contracts
{
    public interface IDamageable
    {
        float CurrentHp { get; }
        bool IsDead { get; }
        void ApplyDamage(DamagePayload payload);
    }
}
