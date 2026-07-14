using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MyDefense.Battle
{
    public class MonsterHealthView : MonoBehaviour
    {
        [SerializeField] private MonsterStat _monsterStat;
        [SerializeField] private Renderer _monsterRenderer;
        [SerializeField] private RectTransform _healthCanvasRoot;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private float _worldYOffset = 0.25f;

        private Camera _mainCamera;
        private Vector3 _initialMonsterLossyScale = Vector3.one;
        private Vector3 _initialCanvasLocalScale = Vector3.one;

        private void Awake()
        {
            if (_monsterStat == null)
            {
                _monsterStat = GetComponentInParent<MonsterStat>();
            }

            if (_monsterRenderer == null && _monsterStat != null)
            {
                _monsterRenderer = _monsterStat.GetComponent<Renderer>();
            }

            if (_healthCanvasRoot == null)
            {
                var canvas = GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    _healthCanvasRoot = canvas.GetComponent<RectTransform>();
                }
            }

            if (_hpText == null)
            {
                _hpText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_hpSlider == null)
            {
                _hpSlider = GetComponentInChildren<Slider>(true);
            }

            _mainCamera = Camera.main;

            if (_monsterStat != null)
            {
                _initialMonsterLossyScale = _monsterStat.transform.lossyScale;
            }

            if (_healthCanvasRoot != null)
            {
                _initialCanvasLocalScale = _healthCanvasRoot.localScale;
            }
        }

        private void OnEnable()
        {
            if (_monsterStat != null)
            {
                _monsterStat.OnHpChanged += HandleHpChanged;
                _monsterStat.OnHpInitialized += HandleHpInitialized;

                UpdateHpUI(_monsterStat.CurrentHp, _monsterStat.MaxHp);
            }
        }

        private void OnDisable()
        {
            if (_monsterStat != null)
            {
                _monsterStat.OnHpChanged -= HandleHpChanged;
                _monsterStat.OnHpInitialized -= HandleHpInitialized;
            }
        }

        private void LateUpdate()
        {
            if (_healthCanvasRoot == null)
            {
                var canvas = GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    _healthCanvasRoot = canvas.GetComponent<RectTransform>();
                    _initialCanvasLocalScale = _healthCanvasRoot.localScale;
                }
            }

            if (_hpText == null)
            {
                _hpText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_hpSlider == null)
            {
                _hpSlider = GetComponentInChildren<Slider>(true);
            }

            if (_monsterRenderer == null && _monsterStat != null)
            {
                _monsterRenderer = _monsterStat.GetComponent<Renderer>();
            }

            if (_healthCanvasRoot != null && _monsterRenderer != null)
            {
                Vector3 center = _monsterRenderer.bounds.center;
                _healthCanvasRoot.position = new Vector3(
                    center.x,
                    _monsterRenderer.bounds.max.y + _worldYOffset,
                    center.z
                );

                if (_mainCamera != null)
                {
                    _healthCanvasRoot.LookAt(_healthCanvasRoot.position + _mainCamera.transform.rotation * Vector3.forward,
                                            _mainCamera.transform.rotation * Vector3.up);
                }

                Vector3 currentLossy = _monsterStat.transform.lossyScale;
                if (currentLossy.x > 0.0001f && currentLossy.y > 0.0001f && currentLossy.z > 0.0001f &&
                    _initialMonsterLossyScale.x > 0.0001f && _initialMonsterLossyScale.y > 0.0001f && _initialMonsterLossyScale.z > 0.0001f)
                {
                    _healthCanvasRoot.localScale = new Vector3(
                        _initialCanvasLocalScale.x * (_initialMonsterLossyScale.x / currentLossy.x),
                        _initialCanvasLocalScale.y * (_initialMonsterLossyScale.y / currentLossy.y),
                        _initialCanvasLocalScale.z * (_initialMonsterLossyScale.z / currentLossy.z)
                    );
                }
            }
        }

        private void HandleHpInitialized(float currentHp, float maxHp)
        {
            UpdateHpUI(currentHp, maxHp);
        }

        private void HandleHpChanged(float currentHp, float maxHp)
        {
            UpdateHpUI(currentHp, maxHp);
        }

        private void UpdateHpUI(float currentHp, float maxHp)
        {
            int curInt = Mathf.RoundToInt(currentHp);
            int maxInt = Mathf.RoundToInt(maxHp);

            if (_hpText != null)
            {
                _hpText.text = $"{curInt} / {maxInt}";
            }

            if (_hpSlider != null)
            {
                _hpSlider.maxValue = maxHp;
                _hpSlider.value = currentHp;
            }
        }
    }
}
