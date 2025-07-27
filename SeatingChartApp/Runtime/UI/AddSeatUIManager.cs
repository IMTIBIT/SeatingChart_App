using SeatingChartApp.Runtime.Systems;
using SeatingChartApp.Runtime.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        PopulateDropdown();
        if (addButton != null) addButton.onClick.AddListener(OnAddSeat);
        if (cancelButton != null) cancelButton.onClick.AddListener(HidePanel);
    }

    private void PopulateDropdown()
    {
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
        if (index < 0 || index >= seatPrefabs.Count) return;
        capacityInput.text = seatPrefabs[index].defaultCapacity.ToString();
    }

    public void ShowPanel()
    {
        _editingSeat = null;
        seatTypeDropdown.interactable = true;
        seatLabelInput.text = string.Empty;
        errorText.text = string.Empty;
        addSeatPanel.SetActive(true);
    }

    private void HidePanel()
    {
        addSeatPanel.SetActive(false);
    }

    private void OnAddSeat()
    {
        string label = seatLabelInput.text.Trim();
        if (string.IsNullOrEmpty(label))
        {
            errorText.text = "Seat name/ID is required.";
            return;
        }

        int capacity;
        if (!int.TryParse(capacityInput.text.Trim(), out capacity) || capacity <= 0)
        {
            errorText.text = "Capacity must be positive.";
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
        return seatPrefabs[seatPrefabs.Count - 1].prefab;
    }

    private void InstantiateSeat(GameObject prefab, string label, int capacity)
    {
        var parent = AreaManager.Instance.GetCurrentAreaContainer() ?? transform;
        var newSeatObj = Instantiate(prefab, parent);
        newSeatObj.name = label;

        var seatController = newSeatObj.GetComponent<SeatController>();
        seatController.SeatID = TryParseSeatId(label);
        seatController.Capacity = capacity;
        seatController.UpdateVisualState();

        LayoutManager.Instance.RegisterSeat(seatController);
        LayoutManager.Instance.MarkLayoutDirty();
    }

    public void AddChair()
    {
        int chairCapacity = 1;
        string label = $"Chair_{Random.Range(1000, 9999)}";
        GameObject chairPrefab = GetPrefabForExactCapacity(chairCapacity);

        if (chairPrefab != null)
        {
            InstantiateSeat(chairPrefab, label, chairCapacity);
        }
        else
        {
            Debug.LogWarning("No chair prefab with capacity 1 found.");
        }
    }

    public void AddTable()
    {
        int tableCapacity = 4;
        string label = $"Table_{Random.Range(1000, 9999)}";
        GameObject tablePrefab = GetPrefabForExactCapacity(tableCapacity);

        if (tablePrefab != null)
        {
            InstantiateSeat(tablePrefab, label, tableCapacity);
        }
        else
        {
            Debug.LogWarning("No table prefab with capacity 4 found.");
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
        if (int.TryParse(label, out int id)) return id;
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
