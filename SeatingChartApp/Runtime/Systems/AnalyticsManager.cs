using UnityEngine;
using SeatingChartApp.Runtime.Data;
using SeatingChartApp.Runtime.Systems;
using System.IO;
using System;
using System.Linq;
using System.Text;

namespace SeatingChartApp.Runtime.Systems
{
    public class AnalyticsManager : MonoBehaviour
    {
        private SessionData _currentSession;

        public int TotalGuestsToday { get; private set; }
        public double AverageStayDurationMinutes { get; private set; }

        public static event Action OnAnalyticsUpdated;

        private string SavePath => Path.Combine(Application.persistentDataPath, $"session_{_currentSession.SessionDate}.json");

        void Awake()
        {
            ServiceProvider.Register(this);
            _currentSession = new SessionData();
            LoadSession();
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<AnalyticsManager>();
        }

        public void RecordGuestSeated(GuestData guest)
        {
            guest.TimeSeated = DateTime.Now;
            _currentSession.GuestInteractions.Add(guest);

            CalculateMetrics();
            SaveSession();
            DebugManager.Log(LogCategory.Analytics, $"Guest '{guest.FirstName} {guest.LastName}' seated at {guest.TimeSeated}.");
        }

        public void RecordGuestCleared(GuestData guest)
        {
            var guestRecord = _currentSession.GuestInteractions.FindLast(g => g.GuestID == guest.GuestID && g.TimeCleared == default);
            if (guestRecord != null)
            {
                guestRecord.TimeCleared = DateTime.Now;
                CalculateMetrics();
                SaveSession();
                TimeSpan duration = guestRecord.TimeCleared - guestRecord.TimeSeated;
                DebugManager.Log(LogCategory.Analytics, $"Guest '{guest.FirstName} {guest.LastName}' cleared at {guestRecord.TimeCleared}. Duration: {duration.TotalMinutes:F2} minutes.");
            }
        }

        /// <summary>
        /// 🆕 NEW: Finalizes the current session, archives it, and starts a new one.
        /// </summary>
        public void ArchiveAndResetSession()
        {
            DebugManager.Log(LogCategory.Analytics, "Archiving and resetting session...");
            // Final save before archiving
            SaveSession();

            // Archive the file by renaming it (optional, but good practice)
            string archivePath = Path.Combine(Application.persistentDataPath, $"session_{_currentSession.SessionDate}_{DateTime.Now:HHmmss}.json.archive");
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Move(SavePath, archivePath);
                    DebugManager.Log(LogCategory.Analytics, $"Session archived to {archivePath}");
                }
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Analytics, $"Could not archive session file: {ex.Message}");
            }

            // Start a new session
            _currentSession = new SessionData();
            CalculateMetrics(); // This will reset counters to 0 and invoke the update event
            SaveSession(); // Save the new, empty session file
        }

        private void CalculateMetrics()
        {
            TotalGuestsToday = _currentSession.GuestInteractions.Count;
            var completedStays = _currentSession.GuestInteractions.Where(g => g.TimeCleared != default).ToList();
            AverageStayDurationMinutes = completedStays.Any() ? completedStays.Average(g => (g.TimeCleared - g.TimeSeated).TotalMinutes) : 0;
            OnAnalyticsUpdated?.Invoke();
        }

        public void ExportSessionToCSV()
        {
            var areaManager = ServiceProvider.Get<AreaManager>();
            string areaName = areaManager != null ? areaManager.CurrentAreaName : "UnknownArea";

            var sb = new StringBuilder();
            sb.AppendLine("FirstName,LastName,RoomNumber,PartySize,GuestID,TimeSeated,TimeCleared,DurationMinutes,Area");

            foreach (var guest in _currentSession.GuestInteractions)
            {
                string timeCleared = guest.TimeCleared == default ? "N/A" : guest.TimeCleared.ToString("yyyy-MM-dd HH:mm:ss");
                double duration = guest.TimeCleared == default ? 0 : (guest.TimeCleared - guest.TimeSeated).TotalMinutes;
                sb.AppendLine($"{guest.FirstName},{guest.LastName},{guest.RoomNumber},{guest.PartySize},{guest.GuestID},{guest.TimeSeated:yyyy-MM-dd HH:mm:ss},{timeCleared},{duration:F2},{areaName}");
            }

            string csvPath = Path.Combine(Application.persistentDataPath, $"analytics_{DateTime.Now:yyyy-MM-dd}.csv");
            try
            {
                File.WriteAllText(csvPath, sb.ToString());
                DebugManager.Log(LogCategory.Analytics, $"SUCCESS: Analytics exported to {csvPath}");
#if UNITY_EDITOR || UNITY_STANDALONE
                Application.OpenURL("file:///" + Application.persistentDataPath);
#endif
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Analytics, $"Failed to export CSV: {ex.Message}");
            }
        }

        private void SaveSession()
        {
            try
            {
                var json = JsonUtility.ToJson(_currentSession, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                DebugManager.LogError(LogCategory.Analytics, $"Failed to save session data: {ex.Message}");
            }
        }

        private void LoadSession()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    var json = File.ReadAllText(SavePath);
                    _currentSession = JsonUtility.FromJson<SessionData>(json);
                    DebugManager.Log(LogCategory.Analytics, $"Session data for {_currentSession.SessionDate} loaded.");
                }
                catch (Exception ex)
                {
                    DebugManager.LogError(LogCategory.Analytics, $"Failed to load session data: {ex.Message}. Starting new session.");
                    _currentSession = new SessionData();
                }
            }
            else
            {
                DebugManager.Log(LogCategory.Analytics, "No session data found for today. Starting a new session.");
            }
            CalculateMetrics();
        }
    }
}
