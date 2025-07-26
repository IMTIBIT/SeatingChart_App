using System;
using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.UI; // Added for accessing SeatingUIManager
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Controls a single seat within the seating chart.  This component is
    /// responsible for tracking the seat's state, managing its occupant
    /// record, displaying a timer while occupied, handling click and drag
    /// interactions, and updating its visual appearance based on its state.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SeatController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("The unique identifier for this seat.  Used for saving and loading layouts.")]
        public int SeatID;

        [Tooltip("Maximum number of guests that this seat can accommodate.  Chairs should be set to 1, tables to 4, etc.")]
        public int Capacity = 1;

        [Tooltip("Current state of the seat (available, occupied, etc.)")]
        public SeatState State = SeatState.Available;

        [Tooltip("Reference to any guest currently occupying this seat.")]
        public GuestData CurrentGuest;

        [Tooltip("The TextMeshPro component used to display the elapsed time while occupied.")]
        public TMPro.TMP_Text TimerText;

        [Tooltip("Image used to tint the seat based on its state.")]
        public Image SeatImage;

        /// <summary>
        /// When a seat becomes occupied this timestamp is recorded so the
        /// elapsed time can be calculated each frame.
        /// </summary>
        public float OccupiedStartTime;

        // Dragging variables
        private bool _dragging;
        private Vector3 _dragOffset;
        private Vector3 _originalScale;
        private Color _originalColor;

        // Grid snapping configuration
        [Tooltip("Enable snapping seat positions to a grid on drag end.")]
        public bool snapToGrid = true;
        [Tooltip("Grid size in pixels for snapping.")]
        public float gridSize = 20f;

        // Stores the starting position before a drag begins to detect collisions or revert
        private Vector2 _startPosition;

        private void Awake()
        {
            // Ensure we always have an image reference for visual feedback
            if (SeatImage == null)
            {
                SeatImage = GetComponent<Image>();
            }
        }

        private void Update()
        {
            // Update the timer display if the seat is occupied
            if (State == SeatState.Occupied && TimerText != null)
            {
                float elapsed = Time.time - OccupiedStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);
                TimerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        /// <summary>
        /// Handles tap events on this seat.  If the seat is not out of
        /// service then it will request the seating UI manager to open the
        /// assignment panel for this seat.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Prevent attendants from interacting with out‑of‑service seats,
            // but allow admins to click them so they can restore the seat
            if (State == SeatState.OutOfService && (UserRoleManager.Instance == null || UserRoleManager.Instance.CurrentRole != UserRoleManager.Role.Admin))
                return;
            SeatingUIManager.Instance?.OpenSeatAssignmentPanel(this);
        }

        /// <summary>
        /// Assigns a guest to this seat.  The state is updated to occupied,
        /// the occupant is stored and the timer resets.  The visual state
        /// colours are updated accordingly.
        /// </summary>
        public void AssignGuest(GuestData guest)
        {
            if (guest == null)
                return;
            // In a more complex system you might support multiple guests per seat
            // for table seating, but for simplicity we only store a single guest
            // record here.  Capacity is checked by the UI manager before calling
            // this method.
            CurrentGuest = guest;
            OccupiedStartTime = Time.time;
            State = SeatState.Occupied;
            UpdateVisualState();
            // Persist layout when a guest is assigned
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
        }

        /// <summary>
        /// Determines whether a guest can be assigned to this seat based on
        /// the seat's capacity.  This method simply checks the guest's
        /// PartySize against the capacity and returns true if it fits.  Use
        /// this in the UI layer to prevent oversubscribing a chair or table.
        /// </summary>
        public bool CanAssignGuest(GuestData guest)
        {
            if (guest == null)
                return false;
            return guest.PartySize <= Capacity;
        }

        /// <summary>
        /// Clears the seat, removing any guest and resetting the timer.  The
        /// state becomes available and the visual colours are updated.
        /// </summary>
        public void ClearSeat()
        {
            CurrentGuest = null;
            State = SeatState.Available;
            if (TimerText != null)
            {
                TimerText.text = string.Empty;
            }
            UpdateVisualState();
            // Persist layout when a seat is cleared
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
        }

        /// <summary>
        /// Toggles the out of service flag.  When a seat is marked as
        /// out-of-service it cannot be interacted with and is displayed in
        /// grey.  Toggling again will return it to the available state.
        /// </summary>
        public void ToggleOutOfService()
        {
            if (State == SeatState.OutOfService)
            {
                State = SeatState.Available;
            }
            else
            {
                // Clearing any occupant when taking the seat out of service
                CurrentGuest = null;
                State = SeatState.OutOfService;
            }
            if (TimerText != null)
            {
                TimerText.text = string.Empty;
            }
            UpdateVisualState();
            // Persist layout when toggling out of service
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
        }

        /// <summary>
        /// Sets the seat's colour based on its current state.  Colours are
        /// deliberately simple and high contrast for outdoor visibility.  If
        /// you wish to theme the app differently you can modify this method
        /// or drive it via a configuration file or ScriptableObject.
        /// </summary>
        public void UpdateVisualState()
        {
            if (SeatImage == null)
                return;

            Color color = Color.white;
            switch (State)
            {
                case SeatState.Available:
                    color = Color.green;
                    break;
                case SeatState.Reserved:
                    color = new Color(1f, 0.64f, 0f); // orange
                    break;
                case SeatState.Occupied:
                    color = Color.red;
                    break;
                case SeatState.Cleaning:
                    color = Color.yellow;
                    break;
                case SeatState.OutOfService:
                    color = Color.gray;
                    break;
            }
            SeatImage.color = color;
        }

        #region Dragging
        /// <summary>
        /// Begins a drag operation.  Only allowed when the current role is
        /// Admin.  Records an offset so the seat follows the finger as
        /// expected and provides visual feedback during the drag.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (UserRoleManager.Instance == null || UserRoleManager.Instance.CurrentRole != UserRoleManager.Role.Admin)
                return;
            _dragging = true;
            RectTransform rect = transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPoint);
            _dragOffset = rect.anchoredPosition - (Vector3)localPoint;
            // Record the starting anchored position for collision detection
            _startPosition = rect.anchoredPosition;
            // Store original visuals and apply feedback
            _originalScale = transform.localScale;
            _originalColor = SeatImage != null ? SeatImage.color : Color.white;
            // Slightly enlarge and lighten the seat while dragging
            transform.localScale = _originalScale * 1.1f;
            if (SeatImage != null)
            {
                Color c = _originalColor;
                c.a = Mathf.Min(1f, c.a + 0.2f);
                SeatImage.color = c;
            }
        }

        /// <summary>
        /// Continues dragging.  Only active for admins.  Moves the seat
        /// relative to its parent canvas based on the pointer's position.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || UserRoleManager.Instance == null || UserRoleManager.Instance.CurrentRole != UserRoleManager.Role.Admin)
                return;
            RectTransform rect = transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
            rect.anchoredPosition = localPoint + (Vector2)_dragOffset;
        }

        /// <summary>
        /// Ends the drag operation and informs the layout manager that the
        /// layout has changed.  This triggers a save at the next update and
        /// snaps the seat to a grid if enabled.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;
            _dragging = false;
            // Restore original visuals
            transform.localScale = _originalScale;
            if (SeatImage != null)
            {
                SeatImage.color = _originalColor;
            }
            RectTransform rect = transform as RectTransform;
            if (snapToGrid && rect != null)
            {
                // Snap anchoredPosition to nearest grid size
                Vector2 pos = rect.anchoredPosition;
                pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
                rect.anchoredPosition = pos;
                // Simple collision avoidance: if another seat already occupies this snapped position
                if (LayoutManager.Instance != null)
                {
                    foreach (var other in LayoutManager.Instance.Seats)
                    {
                        if (other == null || other == this)
                            continue;
                        RectTransform oRect = other.transform as RectTransform;
                        if (oRect != null)
                        {
                            Vector2 otherPos = oRect.anchoredPosition;
                            // Compare within half a grid cell tolerance
                            if (Mathf.Abs(otherPos.x - pos.x) < gridSize * 0.5f && Mathf.Abs(otherPos.y - pos.y) < gridSize * 0.5f)
                            {
                                // Offset horizontally to avoid overlap
                                pos.x += gridSize;
                                break;
                            }
                        }
                    }
                    rect.anchoredPosition = pos;
                }
            }
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
        }
        #endregion
    }
}