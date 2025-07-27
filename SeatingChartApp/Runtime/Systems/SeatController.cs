using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

        // --- Service-located Managers ---
        private UserRoleManager _userRoleManager;
        private SeatingUIManager _seatingUIManager;
        private LayoutManager _layoutManager;
        private AnalyticsManager _analyticsManager;
        private LayoutEditManager _layoutEditManager;
        private SelectionManager _selectionManager;

        // --- Visuals ---
        private Outline _selectionOutline;
        private Outline _editModeOutline;

        // --- Unity Methods ---
        private void Awake()
        {
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

            if (_layoutManager != null) _layoutManager.RegisterSeat(this);
        }

        private void OnDestroy()
        {
            if (_layoutManager != null) _layoutManager.UnregisterSeat(this);
            LayoutEditManager.OnEditModeChanged -= HandleEditModeChanged;
            SelectionManager.OnSelectionChanged -= HandleSelectionChanged;
        }

        // --- Public Methods ---
        public void OnPointerClick(PointerEventData eventData)
        {
            bool isEditMode = _layoutEditManager != null && _layoutEditManager.IsEditModeActive;

            if (isEditMode && eventData.button == PointerEventData.InputButton.Right)
            {
                RotateSeat(45f);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (isEditMode)
                {
                    _seatingUIManager?.OpenSeatAssignmentPanel(this);
                }
                else
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        _selectionManager?.ToggleSelection(this);
                    }
                    else
                    {
                        _selectionManager?.SelectOnly(this);
                        _seatingUIManager?.OpenSeatAssignmentPanel(this);
                    }
                }
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
            if (CurrentGuest != null) _analyticsManager?.RecordGuestCleared(CurrentGuest);
            CurrentGuest = null;
            State = SeatState.Available;
            if (TimerText != null) TimerText.text = string.Empty;
            UpdateVisualState();
            _layoutManager?.MarkLayoutDirty();
        }

        /// <summary>
        /// 🆕 FIXED: This method was missing and has been restored.
        /// Toggles the out of service flag for the seat.
        /// </summary>
        public void ToggleOutOfService()
        {
            State = (State == SeatState.OutOfService) ? SeatState.Available : SeatState.OutOfService;
            if (State == SeatState.OutOfService)
            {
                CurrentGuest = null;
            }
            if (TimerText != null)
            {
                TimerText.text = string.Empty;
            }
            UpdateVisualState();
            _layoutManager?.MarkLayoutDirty();
        }

        public void RotateSeat(float angle)
        {
            if (transform is RectTransform rect)
            {
                rect.Rotate(0, 0, angle);
                _layoutManager?.MarkLayoutDirty();
            }
        }

        public void UpdateVisualState()
        {
            if (SeatImage == null) return;
            Color color = Color.white;
            switch (State)
            {
                case SeatState.Available: color = Color.green; break;
                case SeatState.Reserved: color = new Color(1f, 0.64f, 0f); break;
                case SeatState.Occupied: color = Color.red; break;
                case SeatState.Cleaning: color = Color.yellow; break;
                case SeatState.OutOfService: color = Color.gray; break;
            }
            SeatImage.color = color;
        }

        // --- Event Handlers ---
        private void HandleEditModeChanged(bool isEditMode)
        {
            if (_editModeOutline != null) _editModeOutline.enabled = isEditMode;
        }

        private void HandleSelectionChanged(List<SeatController> selectedSeats)
        {
            if (_selectionOutline != null)
            {
                _selectionOutline.enabled = selectedSeats.Contains(this);
            }
        }

        #region Dragging
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin || _layoutEditManager == null || !_layoutEditManager.IsEditModeActive) return;
            _dragging = true;
            RectTransform rect = transform as RectTransform;
            RectTransform parentRect = rect.parent as RectTransform ?? rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            _dragOffset = rect.anchoredPosition - localPoint;
            _originalScale = transform.localScale;
            transform.localScale = _originalScale * 1.1f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            RectTransform rect = transform as RectTransform;
            RectTransform parentRect = rect.parent as RectTransform ?? rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            rect.anchoredPosition = localPoint + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            transform.localScale = _originalScale;
            RectTransform rect = transform as RectTransform;
            if (snapToGrid && rect != null)
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
