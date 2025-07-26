using SeatingChartApp.Runtime.Systems;
using SeatingChartApp.Runtime.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Provides a UI for admins to add new seats to the current area.  Seats are
    /// selected from a library of prefabs and assigned an ID/label and capacity.
    /// </summary>
    public class AddSeatUIManager : MonoBehaviour
    {
        [Header("Add Seat Panel")]
        [SerializeField] private GameObject addSeatPanel;
        [SerializeField] private TMP_Dropdown seatTypeDropdown;
        [SerializeField] private TMP_InputField seatLabelInput;
        [SerializeField] private TMP_InputField capacityInput;
        [SerializeField] private Button addButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text errorText;

        [Header("Seat Prefabs")]
        [Tooltip("List of seat prefabs that can be added.  Assign in the inspector.")]
        public List<GameObject> seatPrefabs = new List<GameObject>();

        // When editing an existing seat, this will point to the seat being edited.
        private SeatController _editingSeat;

        private void Awake()
        {
            // Populate dropdown with prefab names
            if (seatTypeDropdown != null)
            {
                seatTypeDropdown.options.Clear();
                foreach (var prefab in seatPrefabs)
                {
                    seatTypeDropdown.options.Add(new TMP_Dropdown.OptionData(prefab != null ? prefab.name : "Seat"));
                }
                seatTypeDropdown.RefreshShownValue();
            }
            // Button listeners
            if (addButton != null) addButton.onClick.AddListener(OnAddSeat);
            if (cancelButton != null) cancelButton.onClick.AddListener(HidePanel);
        }

        /// <summary>
        /// Opens the add seat panel and clears inputs.
        /// </summary>
        public void ShowPanel()
        {
            // Show panel for creating a new seat
            _editingSeat = null;
            if (seatTypeDropdown != null && seatPrefabs.Count > 0)
            {
                seatTypeDropdown.value = 0;
            }
            if (seatLabelInput != null) seatLabelInput.text = string.Empty;
            if (capacityInput != null) capacityInput.text = string.Empty;
            if (errorText != null) errorText.text = string.Empty;
            if (addSeatPanel != null) addSeatPanel.SetActive(true);
        }

        /// <summary>
        /// Shows the panel for editing an existing seat.  Prepopulates the fields
        /// with the seat's current ID and capacity and stores the editing seat
        /// reference.  The seat type dropdown is not changed, because changing
        /// prefab type after creation is not supported.
        /// </summary>
        /// <param name="seat">Seat to edit.</param>
        public void ShowPanelForEditing(SeatController seat)
        {
            _editingSeat = seat;
            if (_editingSeat != null)
            {
                // Prepopulate fields with existing values
                if (seatLabelInput != null) seatLabelInput.text = _editingSeat.SeatID.ToString();
                if (capacityInput != null) capacityInput.text = _editingSeat.Capacity.ToString();
                if (errorText != null) errorText.text = string.Empty;
            }
            // Hide seat type dropdown when editing; we do not allow type changes
            if (seatTypeDropdown != null) seatTypeDropdown.gameObject.SetActive(_editingSeat == null);
            if (addSeatPanel != null) addSeatPanel.SetActive(true);
        }

        /// <summary>
        /// Hides the add seat panel.
        /// </summary>
        public void HidePanel()
        {
            if (addSeatPanel != null) addSeatPanel.SetActive(false);
        }

        /// <summary>
        /// Handler for the Add button.  Validates input, instantiates the
        /// selected seat prefab, assigns its ID/label/capacity, and
        /// registers it with the layout manager.
        /// </summary>
        private void OnAddSeat()
        {
            // If we are editing an existing seat, update its properties rather than creating new
            if (_editingSeat != null)
            {
                // Validate inputs
                string newLabel = seatLabelInput != null ? seatLabelInput.text.Trim() : string.Empty;
                if (string.IsNullOrEmpty(newLabel))
                {
                    if (errorText != null) errorText.text = "Seat name/ID is required.";
                    return;
                }
                int newId;
                if (!int.TryParse(newLabel, out newId))
                {
                    if (errorText != null) errorText.text = "Seat ID must be an integer.";
                    return;
                }
                int newCap = _editingSeat.Capacity;
                if (capacityInput != null && !string.IsNullOrWhiteSpace(capacityInput.text))
                {
                    if (!int.TryParse(capacityInput.text.Trim(), out newCap) || newCap <= 0)
                    {
                        if (errorText != null) errorText.text = "Capacity must be a positive integer.";
                        return;
                    }
                }
                // Ensure new ID is unique within current area (if changed)
                if (LayoutManager.Instance != null)
                {
                    foreach (var seat in LayoutManager.Instance.Seats)
                    {
                        if (seat != null && seat != _editingSeat && seat.SeatID == newId)
                        {
                            if (errorText != null) errorText.text = "Seat ID already exists.";
                            return;
                        }
                    }
                }
                // Apply changes
                _editingSeat.SeatID = newId;
                _editingSeat.Capacity = newCap;
                _editingSeat.name = newLabel;
                // Persist changes
                if (LayoutManager.Instance != null)
                {
                    LayoutManager.Instance.MarkLayoutDirty();
                }
                // Clear editing state and hide panel
                _editingSeat = null;
                // Re-enable dropdown for next creation
                if (seatTypeDropdown != null) seatTypeDropdown.gameObject.SetActive(true);
                HidePanel();
                return;
            }
            if (seatPrefabs == null || seatPrefabs.Count == 0)
            {
                if (errorText != null) errorText.text = "No seat prefabs configured.";
                return;
            }
            int index = seatTypeDropdown != null ? seatTypeDropdown.value : 0;
            if (index < 0 || index >= seatPrefabs.Count || seatPrefabs[index] == null)
            {
                if (errorText != null) errorText.text = "Invalid seat selection.";
                return;
            }
            string label = seatLabelInput != null ? seatLabelInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(label))
            {
                if (errorText != null) errorText.text = "Seat name/ID is required.";
                return;
            }
            int capacity = 1;
            if (capacityInput != null && !string.IsNullOrWhiteSpace(capacityInput.text))
            {
                if (!int.TryParse(capacityInput.text.Trim(), out capacity) || capacity <= 0)
                {
                    if (errorText != null) errorText.text = "Capacity must be a positive integer.";
                    return;
                }
            }
            // Ensure the seat ID (label) is unique within the current area
            if (LayoutManager.Instance != null)
            {
                foreach (var seat in LayoutManager.Instance.Seats)
                {
                    if (seat != null && seat.SeatID.ToString().Equals(label, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (errorText != null) errorText.text = "Seat ID already exists.";
                        return;
                    }
                }
            }
            // Instantiate the selected prefab
            GameObject prefab = seatPrefabs[index];
            Transform parent = null;
            // Parent the new seat to the current area container if available
            if (AreaManager.Instance != null)
            {
                parent = AreaManager.Instance.GetCurrentAreaContainer();
            }
            if (parent == null)
            {
                // Fallback to this manager's transform if no area is active
                parent = transform;
            }
            GameObject newSeatObj = Instantiate(prefab, parent);
            newSeatObj.name = label;
            SeatController seatController = newSeatObj.GetComponent<SeatController>();
            if (seatController != null)
            {
                seatController.SeatID = TryParseSeatId(label);
                seatController.Capacity = capacity;
                seatController.UpdateVisualState();
            }
            // Register with the LayoutManager
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.RegisterSeat(seatController);
                LayoutManager.Instance.MarkLayoutDirty();
            }
            HidePanel();
        }

        /// <summary>
        /// Helper to parse a seat ID from the label.  Falls back to a hash code
        /// if parsing fails.  Seat IDs should ideally be integers.
        /// </summary>
        private int TryParseSeatId(string label)
        {
            if (int.TryParse(label, out int id))
                return id;
            return label.GetHashCode();
        }
    }
}