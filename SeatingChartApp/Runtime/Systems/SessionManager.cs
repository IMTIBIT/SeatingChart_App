using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Manages the operational day, including the critical "End of Day" process.
    /// This process clears all guests, archives analytics, and prepares the app for the next day.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        private AreaManager _areaManager;
        private LayoutManager _layoutManager;
        private AnalyticsManager _analyticsManager;

        private void Awake()
        {
            ServiceProvider.Register(this);
        }

        private void Start()
        {
            _areaManager = ServiceProvider.Get<AreaManager>();
            _layoutManager = ServiceProvider.Get<LayoutManager>();
            _analyticsManager = ServiceProvider.Get<AnalyticsManager>();
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<SessionManager>();
        }

        /// <summary>
        /// Initiates the full End of Day process.
        /// </summary>
        public void RunEndOfDayProcess()
        {
            DebugManager.Log(LogCategory.General, "--- Starting End of Day Process ---");
            if (_areaManager == null || _layoutManager == null || _analyticsManager == null)
            {
                DebugManager.LogError(LogCategory.General, "A required manager is missing. Aborting End of Day process.");
                return;
            }

            // Clear all guests from every area to finalize their analytics
            ClearAllGuests();

            // Archive the finalized analytics and reset for the new day
            _analyticsManager.ArchiveAndResetSession();

            DebugManager.Log(LogCategory.General, "--- End of Day Process Complete ---");
        }

        /// <summary>
        /// Iterates through all defined areas and clears any seated guests.
        /// </summary>
        private void ClearAllGuests()
        {
            if (_areaManager.areaContainers.Count == 0) return;

            DebugManager.Log(LogCategory.General, "Clearing all remaining guests from all areas...");

            // We don't need to switch areas visually, just operate on the data.
            // A better approach would be to get all SeatControllers regardless of active area.
            // For now, we'll find all of them in the scene.
            var allSeats = FindObjectsOfType<SeatController>();

            foreach (var seat in allSeats)
            {
                if (seat.CurrentGuest != null)
                {
                    seat.ClearSeat();
                }
            }

            // After clearing, ensure the final state is saved.
            _layoutManager.SaveLayout();
        }
    }
}
