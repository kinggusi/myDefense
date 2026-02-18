using UnityEngine;

public class UnitAttack : MonoBehaviour
{
    [Header("설정")]
    public float range = 100f;      // 사거리 엄청 늘림 (테스트용)
    public float fireRate = 1f;    
    public GameObject bulletPrefab;

    private float fireCountdown = 0f;
    private Transform target;

    void Update()
    {
        // 1. 타겟 찾는 중인지 로그 찍기
        if (target == null)
        {
            UpdateTarget();
        }

        fireCountdown -= Time.deltaTime;

        if (target != null && fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Monster");
        
        // ★ [디버그] 몬스터 몇 마리 찾았는지 확인
        if (enemies.Length == 0) {
            // 이 로그가 계속 뜨면 -> 태그(Tag) 설정 안 한 거임!
            // Debug.LogWarning("주변에 'Monster' 태그 달린 놈이 한 명도 없어요!"); 
            return;
        }

        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= range)
        {
            target = nearestEnemy.transform;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) {
            return;
        }

        Vector3 spawnPos = transform.position + Vector3.up * 1.0f;
        GameObject bulletGO = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet bullet = bulletGO.GetComponent<Bullet>();
        if (bullet != null)
            bullet.Seek(target);
    }
}