using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace SeatingChartApp.Runtime.Systems
{
    [RequireComponent(typeof(Image))]
    public class SeatController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // --- Fields ---
        public int SeatID;
        public int Capacity = 1;
        public SeatState State = SeatState.Available;
        public GuestData CurrentGuest;
        public TMPro.TMP_Text TimerText;
        public Image SeatImage;
        public float OccupiedStartTime;

        private bool _dragging;
        private Vector2 _dragOffset;
        private Vector3 _originalScale;
        public bool snapToGrid = true;
        public float gridSize = 50f;

        // --- Service‑located Managers ---
        private UserRoleManager _userRoleManager;
        private SeatingUIManager _seatingUIManager;
        private LayoutManager _layoutManager;
        private AnalyticsManager _analyticsManager;
        private LayoutEditManager _layoutEditManager;
        private SelectionManager _selectionManager;

        // --- Visuals ---
        private Outline _selectionOutline;
        private Outline _editModeOutline;

        private void Awake()
        {
            ServiceProvider.Register(this);
            LayoutEditManager.OnEditModeChanged += HandleEditModeChanged;
            SelectionManager.OnSelectionChanged += HandleSelectionChanged;
        }

        private void Start()
        {
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();
            _seatingUIManager = ServiceProvider.Get<SeatingUIManager>();
            _layoutManager = ServiceProvider.Get<LayoutManager>();
            _analyticsManager = ServiceProvider.Get<AnalyticsManager>();
            _layoutEditManager = ServiceProvider.Get<LayoutEditManager>();
            _selectionManager = ServiceProvider.Get<SelectionManager>();

            if (SeatImage == null) SeatImage = GetComponent<Image>();

            var outlines = GetComponents<Outline>();
            _editModeOutline = outlines.Length > 0 ? outlines[0] : gameObject.AddComponent<Outline>();
            _selectionOutline = outlines.Length > 1 ? outlines[1] : gameObject.AddComponent<Outline>();

            _editModeOutline.effectColor = Color.yellow;
            _editModeOutline.effectDistance = new Vector2(4, -4);
            _editModeOutline.enabled = false;

            _selectionOutline.effectColor = Color.cyan;
            _selectionOutline.effectDistance = new Vector2(-4, 4);
            _selectionOutline.enabled = false;

            _layoutManager?.RegisterSeat(this);
        }

        private void OnDestroy()
        {
            _layoutManager ??= ServiceProvider.Get<LayoutManager>();
            _layoutManager?.UnregisterSeat(this);

            LayoutEditManager.OnEditModeChanged -= HandleEditModeChanged;
            SelectionManager.OnSelectionChanged -= HandleSelectionChanged;

            ServiceProvider.Unregister<SeatController>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            bool isEdit = _layoutEditManager != null && _layoutEditManager.IsEditModeActive;

            // Right‑click rotates—even mid‑drag—and cancels any scaling
            if (isEdit && eventData.button == PointerEventData.InputButton.Right)
            {
                CancelDrag();
                RotateSeat(45f);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (isEdit)
                {
                    _seatingUIManager?.OpenSeatAssignmentPanel(this);
                }
                else
                {
                    bool shift = (Keyboard.current?.leftShiftKey.isPressed ?? false)
                              || (Keyboard.current?.rightShiftKey.isPressed ?? false);

                    if (shift)
                        _selectionManager?.ToggleSelection(this);
                    else
                    {
                        _selectionManager?.SelectOnly(this);
                        _seatingUIManager?.OpenSeatAssignmentPanel(this);
                    }
                }
            }
        }

        private void CancelDrag()
        {
            if (!_dragging) return;
            _dragging = false;
            transform.localScale = _originalScale;
        }

        public void RotateSeat(float angle)
        {
            CancelDrag();  // ensure clean state
            if (transform is RectTransform rect)
            {
                rect.Rotate(0, 0, angle);
                _layoutManager?.MarkLayoutDirty();
            }
        }

        public void AssignGuest(GuestData guest)
        {
            if (guest == null) return;
            CurrentGuest = guest;
            OccupiedStartTime = Time.time;
            State = SeatState.Occupied;
            UpdateVisualState();
            _analyticsManager?.RecordGuestSeated(guest);
            _layoutManager?.MarkLayoutDirty();
        }

        public void ClearSeat()
        {
            if (CurrentGuest != null)
                _analyticsManager?.RecordGuestCleared(CurrentGuest);

            CurrentGuest = null;
            State = SeatState.Available;

            if (TimerText != null)
                TimerText.text = string.Empty;

            UpdateVisualState();
            _layoutManager?.MarkLayoutDirty();
        }

        public void ToggleOutOfService()
        {
            State = (State == SeatState.OutOfService)
                ? SeatState.Available
                : SeatState.OutOfService;

            if (State == SeatState.OutOfService)
                CurrentGuest = null;

            if (TimerText != null)
                TimerText.text = string.Empty;

            UpdateVisualState();
            _layoutManager?.MarkLayoutDirty();
        }

        public void UpdateVisualState()
        {
            if (SeatImage == null) return;
            Color color = State switch
            {
                SeatState.Available => Color.green,
                SeatState.Reserved => new Color(1f, 0.64f, 0f),
                SeatState.Occupied => Color.red,
                SeatState.Cleaning => Color.yellow,
                SeatState.OutOfService => Color.gray,
                _ => Color.white
            };
            SeatImage.color = color;
        }

        private void HandleEditModeChanged(bool isEdit) => _editModeOutline.enabled = isEdit;
        private void HandleSelectionChanged(List<SeatController> sel) => _selectionOutline.enabled = sel.Contains(this);

        #region Dragging
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragging
                || _userRoleManager?.CurrentRole != UserRoleManager.Role.Admin
                || !_layoutEditManager.IsEditModeActive)
                return;

            _dragging = true;
            var rect = transform as RectTransform;
            var parentRect = rect?.parent as RectTransform ?? rect;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local
            );

            _dragOffset = rect.anchoredPosition - local;
            _originalScale = transform.localScale;
            transform.localScale = _originalScale * 1.1f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            var rect = transform as RectTransform;
            var parentRect = rect?.parent as RectTransform ?? rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local
            );
            rect.anchoredPosition = local + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            transform.localScale = _originalScale;

            var rect = transform as RectTransform;
            if (snapToGrid && rect != null && gridSize > 0f)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
                rect.anchoredPosition = pos;
            }

            _layoutManager?.MarkLayoutDirty();
        }
        #endregion
    }
}
