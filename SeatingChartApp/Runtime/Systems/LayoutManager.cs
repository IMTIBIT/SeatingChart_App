using System.Collections.Generic;
using System.IO;
using SeatingChartApp.Runtime.Data;
using UnityEngine;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Central manager responsible for persisting and restoring seat layout
    /// information.  Maintains a list of all SeatController instances in the
    /// scene, saves their state to disk on change or on application quit, and
    /// restores their positions and occupancy on load.
    /// </summary>
    public class LayoutManager : MonoBehaviour
    {
        public static LayoutManager Instance { get; private set; }

        /// <summary>
        /// All seats currently registered in the layout.  Seats register
        /// themselves in Awake so the manager can operate on them.
        /// </summary>
        public List<SeatController> Seats = new List<SeatController>();

        private bool _layoutDirty;

        /// <summary>
        /// Name of the current seating area (e.g. "Pool" or "Waterpark").
        /// Used to prefix the save file so multiple areas can be persisted separately.
        /// </summary>
        [Tooltip("Logical name of the current seating area.  Save files will be named using this value.")]
        public string CurrentAreaName = "Default";

        private string SaveFilePath
        {
            get
            {
                string fileName = $"seatlayout_{CurrentAreaName}.json";
                return Path.Combine(Application.persistentDataPath, fileName);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Gather seats from the scene.  If your seats are dynamically
            // instantiated this could be deferred or made more robust with
            // registration callbacks.  Use the non‑allocating API in newer
            // Unity versions to avoid the deprecated FindObjectsOfType.
#if UNITY_2023_1_OR_NEWER
            SeatController[] existingSeats = FindObjectsByType<SeatController>(FindObjectsSortMode.None);
            Seats.AddRange(existingSeats);
#else
            Seats.AddRange(FindObjectsOfType<SeatController>());
#endif
            // Load any previously saved layout
            LoadLayout();
        }

        private void OnApplicationQuit()
        {
            // Save on exit to ensure layout isn't lost on abrupt closure
            SaveLayout();
        }

        /// <summary>
        /// Switches the current area name and updates the internal seat list.
        /// This method should be called by the AreaManager when
        /// a new seating area is selected.  It saves the current layout,
        /// updates the Seats list from the provided seats and then loads
        /// the new area's layout from disk.
        /// </summary>
        public void SwitchArea(string areaName, List<SeatController> areaSeats)
        {
            // Save the current area's layout before switching
            SaveLayout();
            CurrentAreaName = areaName;
            // Replace the seat list
            Seats.Clear();
            Seats.AddRange(areaSeats);
            // Load the new layout (if any)
            LoadLayout();
        }

        /// <summary>
        /// Flags the layout as dirty which causes it to be saved on the next
        /// update cycle.  This allows for multiple drags or edits without
        /// repeatedly writing to disk every frame.
        /// </summary>
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
        /// Serializes the current seat layout to JSON and writes it to
        /// persistent data path.  Each seat's ID, anchored position, state and
        /// guest (if any) are captured.
        /// </summary>
        public void SaveLayout()
        {
            var layout = new SeatLayoutData();
            layout.seats = new List<SeatData>();
            foreach (var seat in Seats)
            {
                if (seat == null)
                    continue;
                var rect = seat.transform as RectTransform;
                var data = new SeatData
                {
                    seatID = seat.SeatID,
                    anchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero,
                    state = seat.State,
                    guest = seat.CurrentGuest != null ? new GuestData(seat.CurrentGuest.FirstName, seat.CurrentGuest.LastName, seat.CurrentGuest.RoomNumber, seat.CurrentGuest.PartySize, seat.CurrentGuest.GuestID, seat.CurrentGuest.Notes) : null,
                    capacity = seat.Capacity
                };
                layout.seats.Add(data);
            }
            string json = JsonUtility.ToJson(layout, true);
            try
            {
                File.WriteAllText(SaveFilePath, json);
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to save seating layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the seat layout from disk if present, restoring seat positions
        /// and state.  If no file exists nothing is changed.  Should be
        /// called after seats have been collected.
        /// </summary>
        public void LoadLayout()
        {
            if (!File.Exists(SaveFilePath))
                return;
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                var layout = JsonUtility.FromJson<SeatLayoutData>(json);
                if (layout == null || layout.seats == null)
                    return;
                foreach (var data in layout.seats)
                {
                    var seat = Seats.Find(s => s.SeatID == data.seatID);
                    if (seat == null)
                        continue;
                    var rect = seat.transform as RectTransform;
                    if (rect != null)
                    {
                        rect.anchoredPosition = data.anchoredPosition;
                    }
                    seat.State = data.state;
                    seat.Capacity = data.capacity > 0 ? data.capacity : seat.Capacity;
                    if (data.guest != null)
                    {
                        seat.CurrentGuest = new GuestData(data.guest.FirstName, data.guest.LastName, data.guest.RoomNumber, data.guest.PartySize, data.guest.GuestID, data.guest.Notes);
                    }
                    else
                    {
                        seat.CurrentGuest = null;
                    }
                    // If the seat is occupied we treat the seat as just seated; the timer will start at zero on load
                    if (seat.State == SeatState.Occupied && seat.CurrentGuest != null)
                    {
                        seat.OccupiedStartTime = Time.time;
                    }
                    seat.UpdateVisualState();
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to load seating layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes the current layout file and resets all seats back to their
        /// default state.  Positions are cleared to zero and guests removed.
        /// This should only be called by admins via AdminToolsManager.
        /// </summary>
        public void ResetLayout()
        {
            // Delete the save file if it exists
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to reset layout: {ex.Message}");
            }
            // Reset seat positions and clear any guests/state
            foreach (var seat in Seats)
            {
                if (seat == null)
                    continue;
                seat.ClearSeat();
                var rect = seat.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                }
                seat.UpdateVisualState();
            }
            MarkLayoutDirty();
        }
    }
}