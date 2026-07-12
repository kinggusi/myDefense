using UnityEngine;
using MyDefense.Shared.Contracts;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Transform target;
    private bool hasHit = false;
    private DamagePayload damagePayload;
    private bool hasDamagePayload;

    public void SetDamagePayload(DamagePayload payload)
    {
        damagePayload = payload;
        hasDamagePayload = true;
    }

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject); // 목표가 사라지면 총알도 삭제
            return;
        }

        // 1. 목표 방향으로 날아가기
        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        // 2. 부딪혔다! (거리가 매우 가까워짐)
        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
        transform.LookAt(target); // 총알이 목표를 바라봄
    }

    void HitTarget()
    {
        if (hasHit) return;
        hasHit = true;
        // 몬스터의 체력 깎기
        if (target != null && target.gameObject.activeInHierarchy)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DamagePayload payload = hasDamagePayload
                    ? damagePayload
                    : new DamagePayload
                    {
                        AttackerId = 0,
                        Amount = damage,
                        IsCritical = false
                    };
                damageable.ApplyDamage(payload);
            }
        }
        Destroy(gameObject); // 총알 삭제
    }
}
