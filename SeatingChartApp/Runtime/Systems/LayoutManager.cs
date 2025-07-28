using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SeatingChartApp.Runtime.Data;

namespace SeatingChartApp.Runtime.Systems
{
    public class LayoutManager : MonoBehaviour
    {
        public List<SeatController> Seats = new List<SeatController>();
        private bool _layoutDirty;
        public string CurrentAreaName = "Default";

        private string WorkingSaveFilePath => Path.Combine(
            Application.persistentDataPath,
            $"seatlayout_{CurrentAreaName}_working.json"
        );
        private string DefaultSaveFilePath => Path.Combine(
            Application.persistentDataPath,
            $"seatlayout_{CurrentAreaName}_default.json"
        );

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
                File.WriteAllText(WorkingSaveFilePath, JsonUtility.ToJson(layout, true));
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving,
                    $"Failed to save working layout for '{CurrentAreaName}': {ex.Message}");
            }
        }

        public void SaveAsDefaultLayout()
        {
            var layout = CreateLayoutDataFromScene();
            try
            {
                File.WriteAllText(DefaultSaveFilePath, JsonUtility.ToJson(layout, true));
                DebugManager.Log(LogCategory.Saving,
                    $"SUCCESS: Current arrangement for '{CurrentAreaName}' has been saved as the new default layout.");
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving,
                    $"Failed to save default layout for '{CurrentAreaName}': {ex.Message}");
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
                var json = File.ReadAllText(WorkingSaveFilePath);
                var layout = JsonUtility.FromJson<SeatLayoutData>(json);
                ApplyLayoutDataToScene(layout);
                DebugManager.Log(LogCategory.Saving,
                    $"Working layout for area '{CurrentAreaName}' loaded successfully.");
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Saving,
                    $"Failed to load working layout for '{CurrentAreaName}': {ex.Message}");
            }
        }

        public void ResetLayoutToDefault()
        {
            if (File.Exists(WorkingSaveFilePath))
                File.Delete(WorkingSaveFilePath);

            if (File.Exists(DefaultSaveFilePath))
            {
                try
                {
                    var json = File.ReadAllText(DefaultSaveFilePath);
                    var layout = JsonUtility.FromJson<SeatLayoutData>(json);
                    ApplyLayoutDataToScene(layout);
                    DebugManager.Log(LogCategory.Saving,
                        $"SUCCESS: Layout for '{CurrentAreaName}' has been reset to the saved default.");
                }
                catch (Exception ex)
                {
                    DebugManager.LogError(LogCategory.Saving,
                        $"Error loading default layout during reset: {ex.Message}. Performing hard reset.");
                    HardResetSeats();
                }
            }
            else
            {
                DebugManager.LogWarning(LogCategory.Saving,
                    "No default layout saved. Performing hard reset to empty state.");
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
                if (seat.transform is RectTransform rect)
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
                    parentAreaName = seat.transform.parent.name
                };
                layout.seats.Add(data);
            }
            return layout;
        }

        private void ApplyLayoutDataToScene(SeatLayoutData layout)
        {
            if (layout?.seats == null) return;

            var areaMgr = ServiceProvider.Get<AreaManager>();

            foreach (var data in layout.seats)
            {
                var seat = Seats.Find(s => s?.SeatID == data.seatID);
                if (seat == null) continue;

                if (areaMgr != null && !string.IsNullOrEmpty(data.parentAreaName))
                {
                    var parent = areaMgr.areaContainers
                        .FirstOrDefault(t => t.name == data.parentAreaName);
                    if (parent != null && seat.transform.parent != parent)
                        seat.transform.SetParent(parent, true);
                }

                if (seat.transform is RectTransform rect)
                {
                    rect.anchoredPosition = data.anchoredPosition;
                    rect.localEulerAngles = new Vector3(0, 0, data.rotation);
                }

                seat.State = data.state;
                seat.CurrentGuest = data.guest;
                seat.Capacity = data.capacity > 0 ? data.capacity : seat.Capacity;

                if (seat.State == SeatState.Occupied)
                    seat.OccupiedStartTime = Time.time;

                seat.UpdateVisualState();
            }
        }

        public void RegisterSeat(SeatController seat)
        {
            if (seat != null && !Seats.Contains(seat))
                Seats.Add(seat);
        }

        public void UnregisterSeat(SeatController seat)
        {
            if (seat != null)
                Seats.Remove(seat);
        }
    }
}
