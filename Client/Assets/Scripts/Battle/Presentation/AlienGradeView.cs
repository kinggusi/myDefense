using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class AlienGradeView : MonoBehaviour
    {
        private UnitData _unitData;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private Color _gradeColor = Color.white;

        private void Awake()
        {
            _unitData = GetComponent<UnitData>();
            GetComponentsInChildren<Renderer>(true, _renderers);
        }

        private void Start()
        {
            StartCoroutine(ApplyGradeColorRoutine());
        }

        private IEnumerator ApplyGradeColorRoutine()
        {
            int waitFrames = 0;
            while (_unitData != null && string.IsNullOrEmpty(_unitData.grade) && waitFrames < 10)
            {
                waitFrames++;
                yield return null;
            }

            if (_unitData == null)
            {
                Debug.LogWarning("[AlienGradeView] UnitData component is missing on " + gameObject.name);
                yield break;
            }

            string gradeStr = _unitData.grade;
            _gradeColor = GetColorByGrade(gradeStr);

            ApplyColorToRenderers(_gradeColor);
        }

        private Color GetColorByGrade(string grade)
        {
            if (string.IsNullOrEmpty(grade))
            {
                Debug.LogWarning("[AlienGradeView] Grade is empty on " + gameObject.name + ". Defaulting to White.");
                return Color.white;
            }

            string cleanGrade = grade.Trim().ToUpper();
            switch (cleanGrade)
            {
                case "NORMAL":
                    return Color.blue;
                case "EPIC":
                    return Color.magenta;
                case "UNIQUE":
                    return Color.cyan;
                case "LEGEND":
                    return Color.yellow;
                case "MYTHIC":
                    return Color.red;
                default:
                    Debug.LogWarning("[AlienGradeView] Unknown grade '" + grade + "' on " + gameObject.name + ". Defaulting to White.");
                    return Color.white;
            }
        }

        public void ApplyColorToRenderers(Color targetColor)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (var rend in _renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(block);
                bool applied = false;

                if (rend.sharedMaterial != null)
                {
                    if (rend.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        block.SetColor("_BaseColor", targetColor);
                        applied = true;
                    }
                    else if (rend.sharedMaterial.HasProperty("_Color"))
                    {
                        block.SetColor("_Color", targetColor);
                        applied = true;
                    }
                }

                if (applied)
                {
                    rend.SetPropertyBlock(block);
                }
                else
                {
                    Debug.LogWarning("[AlienGradeView] Shader does not support _BaseColor or _Color on Renderer " + rend.gameObject.name);
                }
            }
        }
    }
}
