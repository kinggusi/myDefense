using UnityEngine;
using MyDefense.Shared.Contracts;
using System.Collections;

public class MonsterStat : MonoBehaviour, IDamageable
{
    public float CurrentHp => hp;
    public float MaxHp => maxHp;
    public bool IsDead => isDead;

    public float hp = 30f;
    public float maxHp = 30f;

    public event System.Action<float, float> OnHpChanged;
    public event System.Action<float, float> OnHpInitialized;

    public long monsterSpecId = 1;

    private bool isDead = false;

    private void Awake()
    {
        maxHp = hp;
    }

    public void InitializeHp(float newMaxHp)
    {
        if (newMaxHp < 1f) newMaxHp = 1f;
        maxHp = newMaxHp;
        hp = newMaxHp;
        OnHpInitialized?.Invoke(hp, maxHp);
    }

    public void ApplyDamage(DamagePayload payload)
    {
        if (payload.Amount <= 0f) return;
        TakeDamage(payload.Amount);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        hp -= amount;
        if (hp < 0f) hp = 0f;

        OnHpChanged?.Invoke(hp, maxHp);

        if (hp <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        if (MyDefense.Battle.BattleWaveExecutor.Instance != null)
        {
            MyDefense.Battle.BattleWaveExecutor.Instance.RegisterMonsterKilled();
        }

        GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.OnKillMonster(monsterSpecId);
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    IEnumerator FadeOutAndDestroy()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Color originalColor = rend.material.color;
            float alpha = 1.0f;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime;
                rend.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }
        Destroy(gameObject);
    }
}
