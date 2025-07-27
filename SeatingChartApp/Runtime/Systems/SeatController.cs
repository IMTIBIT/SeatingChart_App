using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private Color _originalColor;
        public bool snapToGrid = true;
        public float gridSize = 50f;

        // --- Service-located Managers ---
        private UserRoleManager _userRoleManager;
        private SeatingUIManager _seatingUIManager;
        private LayoutManager _layoutManager;
        private AnalyticsManager _analyticsManager;
        private LayoutEditManager _layoutEditManager;

        // --- Unity Methods ---
        private void Awake()
        {
            LayoutEditManager.OnEditModeChanged += HandleEditModeChanged;
        }

        private void Start()
        {
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();
            _seatingUIManager = ServiceProvider.Get<SeatingUIManager>();
            _layoutManager = ServiceProvider.Get<LayoutManager>();
            _analyticsManager = ServiceProvider.Get<AnalyticsManager>();
            _layoutEditManager = ServiceProvider.Get<LayoutEditManager>();

            if (SeatImage == null) SeatImage = GetComponent<Image>();

            if (_layoutManager != null) _layoutManager.RegisterSeat(this);
            else DebugManager.LogError(LogCategory.General, "LayoutManager not found. Seat cannot register itself.");
        }

        private void OnDestroy()
        {
            if (_layoutManager != null) _layoutManager.UnregisterSeat(this);
            LayoutEditManager.OnEditModeChanged -= HandleEditModeChanged;
        }

        private void Update()
        {
            if (State == SeatState.Occupied && TimerText != null)
            {
                float elapsed = Time.time - OccupiedStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);
                TimerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        // --- Public Methods ---
        public void OnPointerClick(PointerEventData eventData)
        {
            // 🆕 NEW: Check for right-click rotation in edit mode
            if (_layoutEditManager != null && _layoutEditManager.IsEditModeActive && eventData.button == PointerEventData.InputButton.Right)
            {
                RotateSeat(45f); // Rotate by 45 degrees on right-click
                return; // Prevent panel from opening
            }

            // If in edit mode (and it was a left-click), open the panel
            if (_layoutEditManager != null && _layoutEditManager.IsEditModeActive)
            {
                _seatingUIManager?.OpenSeatAssignmentPanel(this);
                return;
            }

            // Standard operational mode (left-click):
            if (State == SeatState.OutOfService && (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin))
                return;

            _seatingUIManager?.OpenSeatAssignmentPanel(this);
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

        public void ToggleOutOfService()
        {
            State = (State == SeatState.OutOfService) ? SeatState.Available : SeatState.OutOfService;
            if (State == SeatState.OutOfService) CurrentGuest = null;
            if (TimerText != null) TimerText.text = string.Empty;
            UpdateVisualState();

            _layoutManager?.MarkLayoutDirty();
        }

        /// <summary>
        /// Rotates the seat by a specified angle.
        /// </summary>
        public void RotateSeat(float angle)
        {
            if (transform is RectTransform rect)
            {
                rect.Rotate(0, 0, angle);
                _layoutManager?.MarkLayoutDirty();
                DebugManager.Log(LogCategory.UI, $"Seat {SeatID} rotated by {angle} degrees. New rotation: {rect.localEulerAngles.z}");
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
            var outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.enabled = isEditMode;
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(5, -5);
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
