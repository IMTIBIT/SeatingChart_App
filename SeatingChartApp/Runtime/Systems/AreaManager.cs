using System.Collections.Generic;
using SeatingChartApp.Runtime.UI;
using TMPro;
using UnityEngine;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Manages multiple seating areas (e.g. Pool, Waterpark).  Each area is
    /// represented by a container transform holding that area's seats.  When
    /// the selected area changes, all other areas are deactivated, the
    /// LayoutManager is updated with the new seat list and the layout is loaded
    /// from an area‑specific file.  The area dropdown is automatically
    /// populated from the list of area names.
    /// </summary>
    public class AreaManager : MonoBehaviour
    {
        [Tooltip("Dropdown used to select the active seating area.")]
        public TMP_Dropdown areaDropdown;

        [Tooltip("List of names representing each area in the same order as areaContainers.")]
        public List<string> areaNames = new List<string>();

        [Tooltip("Parent transforms for each seating area.  Each should contain the seat instances for that area.")]
        public List<Transform> areaContainers = new List<Transform>();

        private LayoutManager _layoutManager;

        private void Start()
        {
            _layoutManager = LayoutManager.Instance;
            PopulateDropdown();
            // Activate first area by default if available
            if (areaNames.Count > 0)
            {
                SwitchArea(0);
            }
            if (areaDropdown != null)
            {
                areaDropdown.onValueChanged.AddListener(SwitchArea);
            }
        }

        /// <summary>
        /// Populates the dropdown options with the provided area names.
        /// </summary>
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

        /// <summary>
        /// Called when the dropdown value changes.  Deactivates all area
        /// containers except the selected one, gathers its seats and passes
        /// them to the LayoutManager for management.
        /// </summary>
        /// <param name="index">Index of the selected area.</param>
        private void SwitchArea(int index)
        {
            if (_layoutManager == null)
                return;
            if (index < 0 || index >= areaContainers.Count)
                return;
            // Save current area layout via layout manager
            // Deactivate all area containers
            for (int i = 0; i < areaContainers.Count; i++)
            {
                if (areaContainers[i] != null)
                {
                    areaContainers[i].gameObject.SetActive(i == index);
                }
            }
            // Gather seats from the selected container
            List<SeatController> seats = new List<SeatController>();
            if (areaContainers[index] != null)
            {
                seats.AddRange(areaContainers[index].GetComponentsInChildren<SeatController>(true));
            }
            string areaName = areaNames != null && index < areaNames.Count ? areaNames[index] : $"Area{index}";
            _layoutManager.SwitchArea(areaName, seats);
        }
    }
}