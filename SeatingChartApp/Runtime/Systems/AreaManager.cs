using System.Collections.Generic;
using SeatingChartApp.Runtime.UI;
using SeatingChartApp.Runtime.Systems;
using TMPro;
using UnityEngine;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Manages multiple seating areas (e.g. Pool, Waterpark). It has been
    /// updated to use the ServiceProvider and expose the current area name.
    /// </summary>
    public class AreaManager : MonoBehaviour
    {
        [Tooltip("Dropdown used to select the active seating area.")]
        public TMP_Dropdown areaDropdown;

        [Tooltip("List of names representing each area in the same order as areaContainers.")]
        public List<string> areaNames = new List<string>();

        [Tooltip("Parent transforms for each seating area. Each should contain the seat instances for that area.")]
        public List<Transform> areaContainers = new List<Transform>();

        // 🆕 NEW: Public property to get the name of the current area.
        public string CurrentAreaName { get; private set; }

        private int _currentAreaIndex = -1;
        private LayoutManager _layoutManager;

        private void Awake()
        {
            ServiceProvider.Register(this);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _layoutManager = ServiceProvider.Get<LayoutManager>();

            PopulateDropdown();

            if (areaNames.Count > 0)
            {
                SwitchArea(0);
            }
            if (areaDropdown != null)
            {
                areaDropdown.onValueChanged.AddListener(SwitchArea);
            }
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<AreaManager>();
        }

        private void PopulateDropdown()
        {
            if (areaDropdown == null) return;
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (string name in areaNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }
            areaDropdown.options = options;
        }

        private void SwitchArea(int index)
        {
            if (_layoutManager == null)
            {
                DebugManager.LogError(LogCategory.General, "LayoutManager not found. Cannot switch area.");
                return;
            }
            if (index < 0 || index >= areaContainers.Count) return;

            _currentAreaIndex = index;
            CurrentAreaName = areaNames != null && index < areaNames.Count ? areaNames[index] : $"Area{index}";

            for (int i = 0; i < areaContainers.Count; i++)
            {
                if (areaContainers[i] != null)
                {
                    areaContainers[i].gameObject.SetActive(i == index);
                }
            }

            List<SeatController> seats = new List<SeatController>();
            if (areaContainers[index] != null)
            {
                seats.AddRange(areaContainers[index].GetComponentsInChildren<SeatController>(true));
            }

            _layoutManager.SwitchArea(CurrentAreaName, seats);
        }

        public Transform GetCurrentAreaContainer()
        {
            if (_currentAreaIndex < 0 || _currentAreaIndex >= areaContainers.Count)
            {
                return null;
            }
            return areaContainers[_currentAreaIndex];
        }
    }
}
