using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Battle
{
    public class BattleWaveHudView : MonoBehaviour
    {
        [SerializeField] private BattleWaveExecutor _waveExecutor;
        [SerializeField] private Text _waveText;

        private void OnEnable()
        {
            if (_waveExecutor == null)
            {
                _waveExecutor = BattleWaveExecutor.Instance;
            }

            if (_waveExecutor != null)
            {
                _waveExecutor.OnRoundChanged += UpdateWaveText;
                UpdateWaveText(_waveExecutor.CurrentRound);
            }
        }

        private void OnDisable()
        {
            if (_waveExecutor != null)
            {
                _waveExecutor.OnRoundChanged -= UpdateWaveText;
            }
        }

        private void UpdateWaveText(int round)
        {
            if (_waveText != null)
            {
                _waveText.text = $"WAVE {round}";
            }
        }
    }
}
