using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class AlienMergeHintView : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private UnitData _myUnitData;
        private GameManager _gameManager;
        private bool _isDragging = false;

        private readonly List<RendererHighlightState> _highlightedStates = new List<RendererHighlightState>();

        private class RendererHighlightState
        {
            public Renderer TargetRenderer;
            public MaterialPropertyBlock OriginalBlock;
        }

        private void Awake()
        {
            _myUnitData = GetComponent<UnitData>();
            _gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        private void OnDisable()
        {
            if (_isDragging)
            {
                ClearAllHighlights();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_myUnitData == null || _myUnitData.grade == "MYTHIC") return;

            if (_isDragging)
            {
                ClearAllHighlights();
            }

            _isDragging = true;
            HighlightMergeTargets();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDragging)
            {
                ClearAllHighlights();
            }
        }

        private void HighlightMergeTargets()
        {
            if (_gameManager == null || _gameManager.myGridParent == null)
            {
                Debug.LogWarning("[AlienMergeHintView] GameManager or myGridParent is null. Reverting drag flag.");
                _isDragging = false;
                return;
            }

            UnitData[] boardUnits = _gameManager.myGridParent.GetComponentsInChildren<UnitData>(false);
            List<string> highlightedList = new List<string>();

            foreach (var unit in boardUnits)
            {
                if (unit == null || unit == _myUnitData) continue;
                if (!unit.gameObject.activeInHierarchy) continue;

                if (unit.specId == _myUnitData.specId
                    && unit.grade == _myUnitData.grade)
                {
                    Renderer targetRenderer = unit.GetComponent<Renderer>();
                    if (targetRenderer != null)
                    {
                        if (ContainsRenderer(targetRenderer)) continue;

                        MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
                        targetRenderer.GetPropertyBlock(originalBlock);

                        MaterialPropertyBlock highlightBlock = new MaterialPropertyBlock();
                        targetRenderer.GetPropertyBlock(highlightBlock);

                        Color highlightColor = new Color(0.2f, 1.0f, 0.2f, 1.0f);
                        bool propertyApplied = false;

                        if (targetRenderer.sharedMaterial != null)
                        {
                            if (targetRenderer.sharedMaterial.HasProperty("_Color"))
                            {
                                highlightBlock.SetColor("_Color", highlightColor);
                                propertyApplied = true;
                            }
                            else if (targetRenderer.sharedMaterial.HasProperty("_BaseColor"))
                            {
                                highlightBlock.SetColor("_BaseColor", highlightColor);
                                propertyApplied = true;
                            }
                        }

                        if (propertyApplied)
                        {
                            targetRenderer.SetPropertyBlock(highlightBlock);

                            _highlightedStates.Add(new RendererHighlightState
                            {
                                TargetRenderer = targetRenderer,
                                OriginalBlock = originalBlock
                            });

                            highlightedList.Add(unit.serverId.ToString());
                        }
                    }
                }
            }

            if (highlightedList.Count > 0)
            {
                Debug.Log("[AlienMergeHintView] Drag started. Highlighted serverIds: " + string.Join(", ", highlightedList));
            }
            else
            {
                Debug.Log("[AlienMergeHintView] Drag started. No mergeable targets on board.");
            }
        }

        private bool ContainsRenderer(Renderer rend)
        {
            foreach (var state in _highlightedStates)
            {
                if (state.TargetRenderer == rend) return true;
            }
            return false;
        }

        private void ClearAllHighlights()
        {
            _isDragging = false;
            foreach (var state in _highlightedStates)
            {
                if (state.TargetRenderer != null)
                {
                    state.TargetRenderer.SetPropertyBlock(state.OriginalBlock);
                }
            }
            _highlightedStates.Clear();
        }
    }
}
