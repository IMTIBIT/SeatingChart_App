using System.Collections.Generic;
using System.IO;
using SeatingChartApp.Runtime.Data;
using UnityEngine;
using System;
using System.Linq;

namespace SeatingChartApp.Runtime.Systems
{
    public class LayoutManager : MonoBehaviour
    {
        public List<SeatController> Seats = new List<SeatController>();
        private bool _layoutDirty;
        public string CurrentAreaName = "Default";

        private string WorkingSaveFilePath => Path.Combine(Application.persistentDataPath, $"seatlayout_{CurrentAreaName}_working.json");
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

        public void SaveLayout()
        {
            var layout = CreateLayoutDataFromScene();
            try
            {
                string json = JsonUtility.ToJson(layout, true);
                File.WriteAllText(WorkingSaveFilePath, json);
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving, $"Failed to save working layout for '{CurrentAreaName}': {ex.Message}");
            }
        }

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

        public void LoadLayout()
        {
            if (!File.Exists(WorkingSaveFilePath))
            {
                ResetLayoutToDefault();
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

        public void ResetLayoutToDefault()
        {
            if (File.Exists(WorkingSaveFilePath)) try { File.Delete(WorkingSaveFilePath); } catch { }

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
                DebugManager.LogWarning(LogCategory.Saving, "No default layout saved. Performing hard reset to empty state.");
                HardResetSeats();
            }
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
                    capacity = seat.Capacity,
                    parentAreaName = seat.transform.parent.name // 🆕 NEW: Save the parent container's name
                };
                layout.seats.Add(data);
            }
            return layout;
        }

        private void ApplyLayoutDataToScene(SeatLayoutData layout)
        {
            if (layout == null || layout.seats == null) return;

            var areaManager = ServiceProvider.Get<AreaManager>();

            foreach (var data in layout.seats)
            {
                // Find the seat in the currently managed list
                var seat = Seats.Find(s => s != null && s.SeatID == data.seatID);
                if (seat == null) continue;

                var rect = seat.transform as RectTransform;

                // 🆕 NEW: Ensure the seat is in the correct parent container
                if (areaManager != null && !string.IsNullOrEmpty(data.parentAreaName))
                {
                    Transform correctParent = areaManager.areaContainers.FirstOrDefault(t => t.name == data.parentAreaName);
                    if (correctParent != null && seat.transform.parent != correctParent)
                    {
                        seat.transform.SetParent(correctParent, true);
                    }
                }

                if (rect != null)
                {
                    rect.anchoredPosition = data.anchoredPosition;
                    rect.localEulerAngles = new Vector3(0, 0, data.rotation);
                }

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
            }
        }

        public void UnregisterSeat(SeatController seat)
        {
            if (seat != null) Seats.Remove(seat);
        }
    }
}
