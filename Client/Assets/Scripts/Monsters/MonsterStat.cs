using UnityEngine;
using System.Collections;
using MyDefense.Shared.Contracts;

public class MonsterStat : MonoBehaviour, IDamageable
{
    public float hp = 30f;
    
    // ★ 서버 DB에 있는 몬스터 ID (테스트용으로 1번이라고 칩시다)
    public long monsterSpecId = 1; 

    private bool isDead = false;

    // IDamageable 속성 구현
    public float CurrentHp => hp;
    public bool IsDead => isDead;

    // IDamageable 메서드 구현
    public void ApplyDamage(DamagePayload payload)
    {
        if (isDead) return;

        hp -= payload.Amount;
        if (hp <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(float amount)
    {
        ApplyDamage(new DamagePayload { AttackerId = 0, Amount = amount, IsCritical = false });
    }

    void Die()
    {
        isDead = true;

        FindObjectOfType<GameManager>().OnKillMonster(monsterSpecId);

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