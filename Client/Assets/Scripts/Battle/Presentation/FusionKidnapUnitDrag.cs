using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MyDefense.Battle;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Local drag input for an authoritative Fusion board unit. It only sends
    /// a move request; the State Authority decides whether the move is valid.
    /// </summary>
    public sealed class FusionKidnapUnitDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private FusionKidnapBoardView _view;
        private BattleWaveStateAuthority _authority;
        private int _playerSlot;
        private int _slotIndex;
        private Transform _originalParent;
        private Vector3 _originalLocalPosition;
        private Vector3 _originalLocalScale;
        private bool _dragging;
        private bool _pointerPressed;
        private Vector2 _pointerDownScreenPosition;
        private bool _reportedInput;
        private bool _reportedReject;
        private Plane _dragPlane;

        private const float DragThresholdPixels = 12f;

        public bool IsDragging => _dragging;
        public int PlayerSlot => _playerSlot;
        public int SlotIndex => _slotIndex;

        public void UpdateSlotIndex(int slotIndex)
        {
            _slotIndex = slotIndex;
        }

        public void Initialize(FusionKidnapBoardView view, BattleWaveStateAuthority authority, int playerSlot, int slotIndex)
        {
            _view = view;
            _authority = authority;
            _playerSlot = playerSlot;
            _slotIndex = slotIndex;
        }

        private void OnMouseDown()
        {
            BeginPointerPress(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginPointerPress(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            TryBeginDrag(eventData.position);
            ContinueDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_dragging) EndDrag();
            _pointerPressed = false;
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            if (!_pointerPressed && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit)
                    && (hit.collider == GetComponent<Collider>() || hit.collider.transform.IsChildOf(transform)))
                    BeginPointerPress(pointerPosition);
            }

            if (_pointerPressed && !_dragging && Mouse.current.leftButton.isPressed)
                TryBeginDrag(pointerPosition);
            if (_dragging && Mouse.current.leftButton.isPressed)
                ContinueDrag(pointerPosition);
            if (_dragging && Mouse.current.leftButton.wasReleasedThisFrame)
                EndDrag();
            if (!_dragging && Mouse.current.leftButton.wasReleasedThisFrame)
                _pointerPressed = false;
        }

        private void BeginPointerPress(Vector2 screenPosition)
        {
            if (_dragging) return;
            _pointerPressed = true;
            _pointerDownScreenPosition = screenPosition;
        }

        private void TryBeginDrag(Vector2 screenPosition)
        {
            if (_dragging || !_pointerPressed) return;
            if ((screenPosition - _pointerDownScreenPosition).sqrMagnitude < DragThresholdPixels * DragThresholdPixels)
                return;
            BeginDrag();
        }

        private void BeginDrag()
        {
            if (_dragging || !CanDrag())
                return;

            _dragging = true;
            _originalParent = transform.parent;
            _originalLocalPosition = transform.localPosition;
            _originalLocalScale = transform.localScale;
            _dragPlane = new Plane(Vector3.up, transform.position);
            transform.SetParent(_view.GetGrid(_playerSlot), true);
            if (!_reportedInput)
            {
                _reportedInput = true;
                Debug.Log($"[FusionKidnapUnitDrag] drag started: playerSlot={_playerSlot}, slot={_slotIndex}.");
            }
        }

        private void OnMouseDrag()
        {
            if (Mouse.current == null)
                return;

            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            TryBeginDrag(pointerPosition);
            ContinueDrag(pointerPosition);
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            if (!_dragging || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (_dragPlane.Raycast(ray, out float distance))
                transform.position = ray.GetPoint(distance);
        }

        private void OnMouseUp()
        {
            EndDrag();
        }

        private void EndDrag()
        {
            if (!_dragging) return;
            _dragging = false;
            _pointerPressed = false;
            if (_view.TryFindNearestSlot(_playerSlot, transform.position, _slotIndex, out int targetSlot))
            {
                _view.SnapUnitToSlot(gameObject, _playerSlot, _slotIndex);
                if (_authority.IsBoardInjector(_playerSlot, _slotIndex) && _authority.IsBoardOccupied(_playerSlot, targetSlot))
                    _authority.RequestUseInjector(_slotIndex, targetSlot);
                else if (_authority.IsBoardOccupied(_playerSlot, targetSlot))
                    _authority.RequestMergeOrSwap(_slotIndex, targetSlot);
                else
                    _authority.RequestMove(_slotIndex, targetSlot);
                return;
            }

            RestoreOriginalVisual();
        }

        private bool CanDrag()
        {
            if (_view == null || _authority == null || _authority.Runner == null)
            {
                ReportReject("authority/view/runner is not ready");
                return false;
            }
            int localSlot = _authority.GetNetworkedPlayerSlot(_authority.Runner.LocalPlayer);
            bool occupied = _authority.IsBoardOccupied(_playerSlot, _slotIndex);
            bool choiceLocked = _authority.IsBoardSlotLockedForMythicChoice(_playerSlot, _slotIndex);
            if (localSlot != _playerSlot || !occupied || choiceLocked)
            {
                ReportReject($"localSlot={localSlot}, unitSlot={_playerSlot}, slot={_slotIndex}, occupied={occupied}, choiceLocked={choiceLocked}");
                return false;
            }
            return true;
        }

        private void ReportReject(string reason)
        {
            if (_reportedReject)
                return;
            _reportedReject = true;
            Debug.Log($"[FusionKidnapUnitDrag] drag rejected: {reason}.");
        }

        private void RestoreOriginalVisual()
        {
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, false);
                transform.localPosition = _originalLocalPosition;
                transform.localScale = _originalLocalScale;
            }
        }
    }
}
