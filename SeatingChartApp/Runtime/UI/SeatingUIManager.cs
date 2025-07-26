using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.Systems;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Handles all seat assignment user interface.  Presents a panel for
    /// entering guest details, validates input, and applies assignments
    /// through the SeatController.  Also exposes buttons for clearing seats
    /// and toggling out‑of‑service status.  Uses a simple singleton for
    /// access from SeatController.
    /// </summary>
    public class SeatingUIManager : MonoBehaviour
    {
        public static SeatingUIManager Instance { get; private set; }

        [Header("Assignment Panel References")]
        [SerializeField] private GameObject assignmentPanel;
        [SerializeField] private TMP_Text seatHeaderText;
        [SerializeField] private TMP_InputField firstNameInput;
        [SerializeField] private TMP_InputField lastNameInput;
        [SerializeField] private TMP_InputField roomNumberInput;
        [SerializeField] private TMP_InputField partySizeInput;
        [SerializeField] private TMP_InputField guestIdInput;
        [SerializeField] private TMP_InputField notesInput;
        [SerializeField] private Button assignButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button outOfServiceButton;
        [SerializeField] private TMP_Text feedbackText;

        // Currently selected seat for editing
        private SeatController _currentSeat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Hook up button listeners
            if (assignButton != null) assignButton.onClick.AddListener(OnAssignGuest);
            if (cancelButton != null) cancelButton.onClick.AddListener(ClosePanel);
            if (clearButton != null) clearButton.onClick.AddListener(OnClearSeat);
            if (outOfServiceButton != null) outOfServiceButton.onClick.AddListener(OnToggleOutOfService);

            // Input field validation
            if (firstNameInput != null) firstNameInput.onValueChanged.AddListener(_ => ValidateInputs());
            if (lastNameInput != null) lastNameInput.onValueChanged.AddListener(_ => ValidateInputs());
            if (roomNumberInput != null) roomNumberInput.onValueChanged.AddListener(_ => ValidateInputs());
            if (partySizeInput != null) partySizeInput.onValueChanged.AddListener(_ => ValidateInputs());
        }

        /// <summary>
        /// Opens the assignment panel for the specified seat.  Populates
        /// fields based on the seat's current state and occupant.  Only
        /// attendants and admins may call this method; additional role
        /// filtering is handled in SeatController.
        /// </summary>
        public void OpenSeatAssignmentPanel(SeatController seat)
        {
            if (seat == null)
                return;
            _currentSeat = seat;
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(true);
            }
            // Update header text
            if (seatHeaderText != null)
            {
                seatHeaderText.text = $"Seat {seat.SeatID}";
            }
            // Populate fields
            if (seat.CurrentGuest != null)
            {
                // Display existing guest details
                if (firstNameInput != null) firstNameInput.text = seat.CurrentGuest.FirstName;
                if (lastNameInput != null) lastNameInput.text = seat.CurrentGuest.LastName;
                if (roomNumberInput != null) roomNumberInput.text = seat.CurrentGuest.RoomNumber;
                if (partySizeInput != null) partySizeInput.text = seat.CurrentGuest.PartySize.ToString();
                if (guestIdInput != null) guestIdInput.text = seat.CurrentGuest.GuestID;
                if (notesInput != null) notesInput.text = seat.CurrentGuest.Notes;
            }
            else
            {
                // Clear fields for new entry
                if (firstNameInput != null) firstNameInput.text = string.Empty;
                if (lastNameInput != null) lastNameInput.text = string.Empty;
                if (roomNumberInput != null) roomNumberInput.text = string.Empty;
                if (partySizeInput != null) partySizeInput.text = string.Empty;
                if (guestIdInput != null) guestIdInput.text = string.Empty;
                if (notesInput != null) notesInput.text = string.Empty;
            }
            // Update clear button text
            if (clearButton != null)
            {
                clearButton.gameObject.SetActive(seat.CurrentGuest != null);
            }
            // Update out of service button text
            if (outOfServiceButton != null)
            {
                outOfServiceButton.gameObject.SetActive(UserRoleManager.Instance != null && UserRoleManager.Instance.CurrentRole == UserRoleManager.Role.Admin);
                string label = seat.State == SeatState.OutOfService ? "Restore Seat" : "Out of Service";
                TMP_Text btnText = outOfServiceButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = label;
            }
            // Hide feedback
            if (feedbackText != null) feedbackText.text = string.Empty;
            ValidateInputs();
        }

        /// <summary>
        /// Validates the input fields and toggles the assign button
        /// accordingly.  Ensures required fields are not empty and party
        /// size is a positive integer within capacity.
        /// </summary>
        private bool ValidateInputs()
        {
            bool isValid = true;
            if (_currentSeat == null)
            {
                isValid = false;
            }
            // Basic string checks
            if (string.IsNullOrWhiteSpace(firstNameInput?.text) ||
                string.IsNullOrWhiteSpace(lastNameInput?.text) ||
                string.IsNullOrWhiteSpace(roomNumberInput?.text) ||
                string.IsNullOrWhiteSpace(partySizeInput?.text))
            {
                isValid = false;
            }
            // Party size numeric check
            int size = 0;
            if (!int.TryParse(partySizeInput?.text, out size) || size <= 0)
            {
                isValid = false;
            }
            // Capacity check
            if (_currentSeat != null && size > _currentSeat.Capacity)
            {
                isValid = false;
                if (feedbackText != null)
                {
                    feedbackText.text = $"Party exceeds capacity ({_currentSeat.Capacity}).";
                }
            }
            else
            {
                if (feedbackText != null)
                {
                    feedbackText.text = string.Empty;
                }
            }
            // Disable assign when seat is out of service
            if (_currentSeat != null && _currentSeat.State == SeatState.OutOfService)
            {
                isValid = false;
            }
            if (assignButton != null)
            {
                assignButton.interactable = isValid;
            }
            return isValid;
        }

        /// <summary>
        /// Called when the assign button is pressed.  Validates input one
        /// more time and if valid creates a new GuestData record and
        /// assigns it to the seat.  Persists the layout change and closes
        /// the panel.
        /// </summary>
        private void OnAssignGuest()
        {
            if (_currentSeat == null)
                return;
            if (!ValidateInputs())
            {
                // Prevent assignment if invalid
                return;
            }
            int size = int.Parse(partySizeInput.text);
            var guest = new GuestData(
                firstNameInput.text.Trim(),
                lastNameInput.text.Trim(),
                roomNumberInput.text.Trim(),
                size,
                guestIdInput != null ? guestIdInput.text.Trim() : string.Empty,
                notesInput != null ? notesInput.text.Trim() : string.Empty);
            _currentSeat.AssignGuest(guest);
            // Mark layout as dirty so it saves immediately
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
            ClosePanel();
        }

        /// <summary>
        /// Clears the current seat's guest assignment.  Only shown when
        /// editing an occupied seat.  Persists the layout change.
        /// </summary>
        private void OnClearSeat()
        {
            if (_currentSeat == null)
                return;
            _currentSeat.ClearSeat();
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
            ClosePanel();
        }

        /// <summary>
        /// Toggles the current seat's out‑of‑service state.  Only available
        /// for admins.  Persists the layout change and closes the panel.
        /// </summary>
        private void OnToggleOutOfService()
        {
            if (_currentSeat == null)
                return;
            _currentSeat.ToggleOutOfService();
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.MarkLayoutDirty();
            }
            ClosePanel();
        }

        /// <summary>
        /// Hides the assignment panel and clears the selected seat reference.
        /// Input fields are not cleared here; they will be overwritten the
        /// next time the panel opens.
        /// </summary>
        public void ClosePanel()
        {
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(false);
            }
            _currentSeat = null;
        }
    }
}