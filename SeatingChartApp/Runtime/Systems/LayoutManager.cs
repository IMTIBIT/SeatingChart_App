using System.Collections.Generic;
using System.IO;
using SeatingChartApp.Runtime.Data;
using UnityEngine;
using System;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Central manager for persisting and restoring seat layouts. Now supports
    /// a separate "default" layout file that can be explicitly saved and restored by admins.
    /// </summary>
    public class LayoutManager : MonoBehaviour
    {
        public List<SeatController> Seats = new List<SeatController>();
        private bool _layoutDirty;
        public string CurrentAreaName = "Default";

        // The file for the current, operational layout
        private string WorkingSaveFilePath => Path.Combine(Application.persistentDataPath, $"seatlayout_{CurrentAreaName}_working.json");

        // 🆕 NEW: The file for the admin-defined master/default layout
        private string DefaultSaveFilePath => Path.Combine(Application.persistentDataPath, $"seatlayout_{CurrentAreaName}_default.json");

        private void Awake()
        {
            ServiceProvider.Register(this);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<LayoutManager>();
        }

        private void Start()
        {
            LoadLayout();
        }

        private void OnApplicationQuit()
        {
            SaveLayout();
        }

        public void SwitchArea(string areaName, List<SeatController> areaSeats)
        {
            SaveLayout();
            CurrentAreaName = areaName;
            Seats.Clear();
            Seats.AddRange(areaSeats);
            LoadLayout();
        }

        public void MarkLayoutDirty()
        {
            _layoutDirty = true;
        }

        private void Update()
        {
            if (_layoutDirty)
            {
                SaveLayout();
                _layoutDirty = false;
            }
        }

        /// <summary>
        /// Saves the current seat layout to the working file.
        /// </summary>
        public void SaveLayout()
        {
            var layout = CreateLayoutDataFromScene();
            try
            {
                string json = JsonUtility.ToJson(layout, true);
                File.WriteAllText(WorkingSaveFilePath, json);
                // We don't log this every time to avoid spamming the console
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving, $"Failed to save working layout for '{CurrentAreaName}': {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 NEW: Saves the current seat layout as the new default/master layout.
        /// This is an explicit admin action.
        /// </summary>
        public void SaveAsDefaultLayout()
        {
            var layout = CreateLayoutDataFromScene();
            try
            {
                string json = JsonUtility.ToJson(layout, true);
                File.WriteAllText(DefaultSaveFilePath, json);
                DebugManager.Log(LogCategory.Saving, $"SUCCESS: Current arrangement for '{CurrentAreaName}' has been saved as the new default layout.");
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving, $"Failed to save default layout for '{CurrentAreaName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the working seat layout from disk.
        /// </summary>
        public void LoadLayout()
        {
            if (!File.Exists(WorkingSaveFilePath))
            {
                DebugManager.Log(LogCategory.Saving, $"No working layout found for '{CurrentAreaName}'. Attempting to load default layout.");
                ResetLayoutToDefault(); // If no working copy, start with the default
                return;
            }

            try
            {
                string json = File.ReadAllText(WorkingSaveFilePath);
                var layout = JsonUtility.FromJson<SeatLayoutData>(json);
                ApplyLayoutDataToScene(layout);
                DebugManager.Log(LogCategory.Saving, $"Working layout for area '{CurrentAreaName}' loaded successfully.");
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving, $"Failed to load working layout for '{CurrentAreaName}': {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 UPDATED: Resets the layout. It now loads from the default file if it exists,
        /// otherwise it clears all seats.
        /// </summary>
        public void ResetLayoutToDefault()
        {
            // First, delete the current working copy
            try
            {
                if (File.Exists(WorkingSaveFilePath)) File.Delete(WorkingSaveFilePath);
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving, $"Could not delete working layout file during reset: {ex.Message}");
            }

            // Now, load the default layout if it exists
            if (File.Exists(DefaultSaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(DefaultSaveFilePath);
                    var layout = JsonUtility.FromJson<SeatLayoutData>(json);
                    ApplyLayoutDataToScene(layout);
                    DebugManager.Log(LogCategory.Saving, $"SUCCESS: Layout for '{CurrentAreaName}' has been reset to the saved default.");
                }
                catch (Exception ex)
                {
                    DebugManager.LogError(LogCategory.Saving, $"Error loading default layout during reset: {ex.Message}. Performing hard reset.");
                    HardResetSeats();
                }
            }
            else
            {
                // If no default file exists, perform a hard reset
                DebugManager.LogWarning(LogCategory.Saving, "No default layout saved. Performing hard reset to empty state.");
                HardResetSeats();
            }

            // Mark dirty to save this reset state as the new working copy
            MarkLayoutDirty();
        }

        private void HardResetSeats()
        {
            foreach (var seat in Seats)
            {
                if (seat == null) continue;
                seat.ClearSeat();
                var rect = seat.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                    rect.localEulerAngles = Vector3.zero;
                }
                seat.UpdateVisualState();
            }
        }

        private SeatLayoutData CreateLayoutDataFromScene()
        {
            var layout = new SeatLayoutData();
            foreach (var seat in Seats)
            {
                if (seat == null) continue;
                var rect = seat.transform as RectTransform;
                var data = new SeatData
                {
                    seatID = seat.SeatID,
                    anchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero,
                    rotation = rect != null ? rect.localEulerAngles.z : 0f,
                    state = seat.State,
                    guest = seat.CurrentGuest,
                    capacity = seat.Capacity
                };
                layout.seats.Add(data);
            }
            return layout;
        }

        private void ApplyLayoutDataToScene(SeatLayoutData layout)
        {
            if (layout == null || layout.seats == null) return;

            foreach (var data in layout.seats)
            {
                var seat = Seats.Find(s => s != null && s.SeatID == data.seatID);
                if (seat == null) continue;

                var rect = seat.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = data.anchoredPosition;
                    rect.localEulerAngles = new Vector3(0, 0, data.rotation);
                }

                // When loading a layout, clear any existing guests
                seat.State = SeatState.Available;
                seat.CurrentGuest = null;

                seat.Capacity = data.capacity > 0 ? data.capacity : seat.Capacity;
                seat.UpdateVisualState();
            }
        }

        public void RegisterSeat(SeatController seat)
        {
            if (seat != null && !Seats.Contains(seat))
            {
                Seats.Add(seat);
                DebugManager.Log(LogCategory.Spawning, $"Seat {seat.SeatID} registered.");
            }
        }

        public void UnregisterSeat(SeatController seat)
        {
            if (seat != null && Seats.Remove(seat))
            {
                DebugManager.Log(LogCategory.Spawning, $"Seat {seat.SeatID} unregistered.");
            }
        }
    }
}
