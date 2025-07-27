using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
    public class SeatingUIManager : MonoBehaviour
    {
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

        [Header("Layout Edit Mode Buttons")]
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button rotateButton;

        [Header("Overlay")]
        [SerializeField] private GameObject overlay;

        private SeatController _currentSeat;

        private UserRoleManager _userRoleManager;
        private LayoutManager _layoutManager;
        private LayoutEditManager _layoutEditManager;

        private void Awake()
        {
            ServiceProvider.Register(this);
            DontDestroyOnLoad(gameObject);

            if (assignButton != null) assignButton.onClick.AddListener(OnAssignGuest);
            if (cancelButton != null) cancelButton.onClick.AddListener(ClosePanel);
            if (clearButton != null) clearButton.onClick.AddListener(OnClearSeat);
            if (outOfServiceButton != null) outOfServiceButton.onClick.AddListener(OnToggleOutOfService);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteSeat);
            if (rotateButton != null) rotateButton.onClick.AddListener(OnRotateSeat);

            if (firstNameInput != null) firstNameInput.onValueChanged.AddListener(_ => ValidateInputs());
            if (lastNameInput != null) lastNameInput.onValueChanged.AddListener(_ => ValidateInputs());
            if (roomNumberInput != null) roomNumberInput.onValueChanged.AddListener(_ => ValidateInputs());
            if (partySizeInput != null) partySizeInput.onValueChanged.AddListener(_ => ValidateInputs());
        }

        private void Start()
        {
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();
            _layoutManager = ServiceProvider.Get<LayoutManager>();
            _layoutEditManager = ServiceProvider.Get<LayoutEditManager>();
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<SeatingUIManager>();
        }

        public void OpenSeatAssignmentPanel(SeatController seat)
        {
            if (seat == null) return;

            ClosePanel();
            _currentSeat = seat;

            if (assignmentPanel != null) assignmentPanel.SetActive(true);
            if (overlay != null) overlay.SetActive(true);
            if (seatHeaderText != null) seatHeaderText.text = $"Seat {seat.SeatID}";

            PopulateFieldsFromSeat(seat);
            UpdatePanelButtons(seat);
            ValidateInputs();
        }

        private void PopulateFieldsFromSeat(SeatController seat)
        {
            bool isEditMode = _layoutEditManager != null && _layoutEditManager.IsEditModeActive;
            if (isEditMode) return;

            if (seat.CurrentGuest != null)
            {
                if (firstNameInput != null) firstNameInput.text = seat.CurrentGuest.FirstName;
                if (lastNameInput != null) lastNameInput.text = seat.CurrentGuest.LastName;
                if (roomNumberInput != null) roomNumberInput.text = seat.CurrentGuest.RoomNumber;
                if (partySizeInput != null) partySizeInput.text = seat.CurrentGuest.PartySize.ToString();
                if (guestIdInput != null) guestIdInput.text = seat.CurrentGuest.GuestID;
                if (notesInput != null) notesInput.text = seat.CurrentGuest.Notes;
            }
            else
            {
                if (firstNameInput != null) firstNameInput.text = string.Empty;
                if (lastNameInput != null) lastNameInput.text = string.Empty;
                if (roomNumberInput != null) roomNumberInput.text = string.Empty;
                if (partySizeInput != null) partySizeInput.text = string.Empty;
                if (guestIdInput != null) guestIdInput.text = string.Empty;
                if (notesInput != null) notesInput.text = string.Empty;
            }
        }

        private void UpdatePanelButtons(SeatController seat)
        {
            bool isAdmin = _userRoleManager != null && _userRoleManager.CurrentRole == UserRoleManager.Role.Admin;
            bool isEditMode = _layoutEditManager != null && _layoutEditManager.IsEditModeActive;

            SetGuestFieldsActive(!isEditMode);

            if (deleteButton != null) deleteButton.gameObject.SetActive(isAdmin && isEditMode);
            if (rotateButton != null) rotateButton.gameObject.SetActive(isAdmin && isEditMode);
            if (clearButton != null) clearButton.gameObject.SetActive(seat.CurrentGuest != null && !isEditMode);
            if (outOfServiceButton != null) outOfServiceButton.gameObject.SetActive(isAdmin && !isEditMode);

            if (outOfServiceButton != null && outOfServiceButton.gameObject.activeSelf)
            {
                string label = seat.State == SeatState.OutOfService ? "Restore Seat" : "Out of Service";
                TMP_Text btnText = outOfServiceButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = label;
            }

            if (feedbackText != null) feedbackText.text = string.Empty;
        }

        private void SetGuestFieldsActive(bool isActive)
        {
            if (firstNameInput != null) firstNameInput.transform.parent.gameObject.SetActive(isActive);
            if (lastNameInput != null) lastNameInput.transform.parent.gameObject.SetActive(isActive);
            if (roomNumberInput != null) roomNumberInput.transform.parent.gameObject.SetActive(isActive);
            if (partySizeInput != null) partySizeInput.transform.parent.gameObject.SetActive(isActive);
            if (guestIdInput != null) guestIdInput.transform.parent.gameObject.SetActive(isActive);
            if (notesInput != null) notesInput.transform.parent.gameObject.SetActive(isActive);
            if (assignButton != null) assignButton.gameObject.SetActive(isActive);
        }

        private bool ValidateInputs()
        {
            bool isEditMode = _layoutEditManager != null && _layoutEditManager.IsEditModeActive;
            if (isEditMode)
            {
                if (assignButton != null) assignButton.interactable = false;
                return false;
            }

            bool isValid = true;
            if (_currentSeat == null) isValid = false;

            if (string.IsNullOrWhiteSpace(firstNameInput?.text) ||
                string.IsNullOrWhiteSpace(lastNameInput?.text) ||
                string.IsNullOrWhiteSpace(roomNumberInput?.text) ||
                string.IsNullOrWhiteSpace(partySizeInput?.text))
            {
                isValid = false;
            }

            if (!int.TryParse(partySizeInput?.text, out int size) || size <= 0)
            {
                isValid = false;
            }

            if (_currentSeat != null && size > _currentSeat.Capacity)
            {
                isValid = false;
                if (feedbackText != null) feedbackText.text = $"Party exceeds capacity ({_currentSeat.Capacity}).";
            }
            else if (isValid)
            {
                if (feedbackText != null) feedbackText.text = string.Empty;
            }

            if (_currentSeat != null && _currentSeat.State == SeatState.OutOfService) isValid = false;
            if (assignButton != null) assignButton.interactable = isValid;

            return isValid;
        }

        // --- Button Handlers ---
        private void OnAssignGuest()
        {
            if (_currentSeat == null || !ValidateInputs()) return;

            int.TryParse(partySizeInput.text, out int size);
            var guest = new GuestData(
                firstNameInput.text.Trim(),
                lastNameInput.text.Trim(),
                roomNumberInput.text.Trim(),
                size,
                guestIdInput != null ? guestIdInput.text.Trim() : string.Empty,
                notesInput != null ? notesInput.text.Trim() : string.Empty);

            _currentSeat.AssignGuest(guest);
            _layoutManager?.MarkLayoutDirty();
            ClosePanel();
        }

        private void OnClearSeat()
        {
            if (_currentSeat == null) return;
            _currentSeat.ClearSeat();
            _layoutManager?.MarkLayoutDirty();
            ClosePanel();
        }

        private void OnToggleOutOfService()
        {
            if (_currentSeat == null) return;
            _currentSeat.ToggleOutOfService();
            _layoutManager?.MarkLayoutDirty();
            ClosePanel();
        }

        private void OnDeleteSeat()
        {
            if (_currentSeat == null) return;
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin) return;

            _layoutManager?.UnregisterSeat(_currentSeat);
            Destroy(_currentSeat.gameObject);
            _layoutManager?.MarkLayoutDirty();
            ClosePanel();
        }

        private void OnRotateSeat()
        {
            if (_currentSeat == null) return;
            // The UI button will rotate by a standard 90 degrees
            _currentSeat.RotateSeat(90f);
        }

        public void ClosePanel()
        {
            if (assignmentPanel != null) assignmentPanel.SetActive(false);
            if (overlay != null) overlay.SetActive(false);
            _currentSeat = null;
        }
    }
}
