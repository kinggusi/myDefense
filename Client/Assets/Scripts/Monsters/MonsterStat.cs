using UnityEngine;
using MyDefense.Battle;
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
    public event System.Action OnDied;

    public long monsterSpecId = 1;

    private bool isDead = false;
    private LaneType battleLane;
    private bool countsTowardLaneLimit;
    private bool battleContextInitialized;

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

    public void InitializeBattleContext(LaneType lane, bool shouldCountTowardLaneLimit)
    {
        battleLane = lane;
        countsTowardLaneLimit = shouldCountTowardLaneLimit;
        battleContextInitialized = true;
    }

    public void ApplyDamage(DamagePayload payload)
    {
        if (payload.Amount <= 0f) return;
        TakeDamage(payload.Amount);
    }

    public void TakeDamage(float amount)
    {
        BattleMonsterNetworkState networkState = GetComponent<BattleMonsterNetworkState>();
        if (networkState != null
            && networkState.Object != null
            && networkState.Object.IsValid
            && !networkState.HasStateAuthority)
        {
            return;
        }

        if (isDead) return;
        hp -= amount;
        if (hp < 0f) hp = 0f;

        OnHpChanged?.Invoke(hp, maxHp);

        if (hp <= 0) Die();
    }

    public void ApplyNetworkState(float currentHp, float networkMaxHp, bool dead)
    {
        maxHp = Mathf.Max(1f, networkMaxHp);
        hp = Mathf.Clamp(currentHp, 0f, maxHp);
        isDead = dead;
        OnHpChanged?.Invoke(hp, maxHp);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDied?.Invoke();

        if (battleContextInitialized
            && countsTowardLaneLimit
            && BattleWaveExecutor.Instance != null)
        {
            BattleWaveExecutor.Instance.RegisterMonsterKilled(battleLane);
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
