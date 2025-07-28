// SessionManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SeatingChartApp.Runtime.Systems
{
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

        public void RunEndOfDayProcess()
        {
            DebugManager.Log(LogCategory.General, "--- Starting End of Day Process ---");
            if (_areaManager == null || _layoutManager == null || _analyticsManager == null)
            {
                DebugManager.LogError(LogCategory.General, "A required manager is missing. Aborting End of Day process.");
                return;
            }

            ClearAllGuests();
            _analyticsManager.ArchiveAndResetSession();
            DebugManager.Log(LogCategory.General, "--- End of Day Process Complete ---");
        }

        private void ClearAllGuests()
        {
            if (_areaManager.areaContainers.Count == 0) return;

            DebugManager.Log(LogCategory.General, "Clearing all remaining guests from all areas...");

            var allSeats = Object.FindObjectsByType<SeatController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None
            );

            foreach (var seat in allSeats)
            {
                if (seat.CurrentGuest != null)
                    seat.ClearSeat();
            }

            _layoutManager.SaveLayout();
        }
    }
}