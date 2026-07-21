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
        private bool _reportedInput;
        private bool _reportedReject;
        private Plane _dragPlane;

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
            BeginDrag();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            BeginDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            ContinueDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EndDrag();
        }

        private void Update()
        {
            if (!_dragging && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit)
                    && (hit.collider == GetComponent<Collider>() || hit.collider.transform.IsChildOf(transform)))
                    BeginDrag();
            }

            if (_dragging && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                EndDrag();
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
            if (!_dragging || Mouse.current == null)
                return;

            ContinueDrag(Mouse.current.position.ReadValue());
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
            if (_view.TryFindNearestSlot(_playerSlot, transform.position, _slotIndex, out int targetSlot)
                && !_authority.IsBoardOccupied(_playerSlot, targetSlot))
            {
                _view.SnapUnitToSlot(gameObject, _playerSlot, _slotIndex);
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
            if (localSlot != _playerSlot || !occupied)
            {
                ReportReject($"localSlot={localSlot}, unitSlot={_playerSlot}, slot={_slotIndex}, occupied={occupied}");
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
