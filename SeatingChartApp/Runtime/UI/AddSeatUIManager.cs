using SeatingChartApp.Runtime.Systems;
using SeatingChartApp.Runtime.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
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
        public List<SeatPrefabData> seatPrefabs = new List<SeatPrefabData>();

        private SeatController _editingSeat;

        // Manager references obtained via ServiceProvider
        private LayoutManager _layoutManager;
        private AreaManager _areaManager;

        private void Awake()
        {
            ServiceProvider.Register(this);

            PopulateDropdown();
            if (addButton != null) addButton.onClick.AddListener(OnAddSeat);
            if (cancelButton != null) cancelButton.onClick.AddListener(HidePanel);
        }

        private void Start()
        {
            // Resolve dependencies using ServiceProvider
            _layoutManager = ServiceProvider.Get<LayoutManager>();
            _areaManager = ServiceProvider.Get<AreaManager>();
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<AddSeatUIManager>();
        }

        private void PopulateDropdown()
        {
            if (seatTypeDropdown == null) return;
            seatTypeDropdown.options.Clear();
            foreach (var prefabData in seatPrefabs)
            {
                seatTypeDropdown.options.Add(new TMP_Dropdown.OptionData(prefabData.prefabName));
            }
            seatTypeDropdown.RefreshShownValue();
            seatTypeDropdown.onValueChanged.AddListener(OnSeatTypeChanged);
            OnSeatTypeChanged(0);
        }

        private void OnSeatTypeChanged(int index)
        {
            if (capacityInput == null || index < 0 || index >= seatPrefabs.Count) return;
            capacityInput.text = seatPrefabs[index].defaultCapacity.ToString();
        }

        public void ShowPanel()
        {
            _editingSeat = null;
            if (seatTypeDropdown != null) seatTypeDropdown.interactable = true;
            if (seatLabelInput != null) seatLabelInput.text = string.Empty;
            if (errorText != null) errorText.text = string.Empty;
            if (addSeatPanel != null) addSeatPanel.SetActive(true);
        }

        private void HidePanel()
        {
            if (addSeatPanel != null) addSeatPanel.SetActive(false);
        }

        private void OnAddSeat()
        {
            string label = seatLabelInput.text.Trim();
            if (string.IsNullOrEmpty(label))
            {
                if (errorText != null) errorText.text = "Seat name/ID is required.";
                return;
            }

            if (!int.TryParse(capacityInput.text.Trim(), out int capacity) || capacity <= 0)
            {
                if (errorText != null) errorText.text = "Capacity must be a positive number.";
                return;
            }

            var selectedPrefab = seatPrefabs[seatTypeDropdown.value].prefab;
            InstantiateSeat(selectedPrefab, label, capacity);
            HidePanel();
        }

        public void AutoAddSeat(string seatLabel, int occupancy)
        {
            GameObject selectedPrefab = GetPrefabForOccupancy(occupancy);
            InstantiateSeat(selectedPrefab, seatLabel, occupancy);
        }

        private GameObject GetPrefabForOccupancy(int occupancy)
        {
            foreach (var seatPrefab in seatPrefabs)
            {
                if (seatPrefab.defaultCapacity >= occupancy)
                    return seatPrefab.prefab;
            }
            // Fallback to the last prefab if none are large enough
            return seatPrefabs.Count > 0 ? seatPrefabs[seatPrefabs.Count - 1].prefab : null;
        }

        private void InstantiateSeat(GameObject prefab, string label, int capacity)
        {
            if (prefab == null)
            {
                DebugManager.LogError(LogCategory.Spawning, "Cannot instantiate seat. Prefab is null.");
                return;
            }
            if (_areaManager == null || _layoutManager == null)
            {
                DebugManager.LogError(LogCategory.Spawning, "AreaManager or LayoutManager not found. Cannot instantiate seat.");
                return;
            }

            var parent = _areaManager.GetCurrentAreaContainer() ?? transform;
            var newSeatObj = Instantiate(prefab, parent);
            newSeatObj.name = label;

            var seatController = newSeatObj.GetComponent<SeatController>();
            if (seatController != null)
            {
                seatController.SeatID = TryParseSeatId(label);
                seatController.Capacity = capacity;
                // The SeatController's Start method will handle registration and visual updates
            }

            _layoutManager.MarkLayoutDirty();
        }

        public void AddChair()
        {
            const int chairCapacity = 1;
            string label = $"Chair_{Random.Range(1000, 9999)}";
            GameObject chairPrefab = GetPrefabForExactCapacity(chairCapacity);

            if (chairPrefab != null)
            {
                InstantiateSeat(chairPrefab, label, chairCapacity);
            }
            else
            {
                DebugManager.LogWarning(LogCategory.Spawning, "No chair prefab with capacity 1 found.");
            }
        }

        public void AddTable()
        {
            const int tableCapacity = 4;
            string label = $"Table_{Random.Range(1000, 9999)}";
            GameObject tablePrefab = GetPrefabForExactCapacity(tableCapacity);

            if (tablePrefab != null)
            {
                InstantiateSeat(tablePrefab, label, tableCapacity);
            }
            else
            {
                DebugManager.LogWarning(LogCategory.Spawning, "No table prefab with capacity 4 found.");
            }
        }

        private GameObject GetPrefabForExactCapacity(int capacity)
        {
            foreach (var seatPrefab in seatPrefabs)
            {
                if (seatPrefab.defaultCapacity == capacity)
                    return seatPrefab.prefab;
            }
            return null;
        }

        private int TryParseSeatId(string label)
        {
            // A more robust way to generate a unique ID from a string label
            return label.GetHashCode();
        }
    }

    [System.Serializable]
    public class SeatPrefabData
    {
        public string prefabName;
        public GameObject prefab;
        public int defaultCapacity;
    }
}
