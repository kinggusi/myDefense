// UnitStat.cs
using UnityEngine;

namespace MyDefense
{
    public class UnitStat : MonoBehaviour
    {
        [Header("--- 왹져 생체 데이터 ---")]
        [SerializeField] private string _unitName = "실험체_01";
        [SerializeField] private Define.MutationPrefix _mutation = Define.MutationPrefix.None;
        [SerializeField] private float _hp = 100f;

        public float Hp => _hp; // Getter

        public void OnDamage(float damage)
        {
            _hp -= damage;
            Debug.Log($"{_unitName} 피격! 남은 체력: {_hp}");
            if (_hp <= 0) gameObject.SetActive(false);
        }
    }
}