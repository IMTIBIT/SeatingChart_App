using SeatingChartApp.Runtime.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeatingChartApp.Runtime.UI
{
    public class AdminToolsManager : MonoBehaviour
    {
        [Header("Admin Buttons")]
        [SerializeField] private Button resetLayoutButton;
        [SerializeField] private Button roleSwitchButton;
        [SerializeField] private Button layoutEditButton;
        [SerializeField] private Button saveDefaultLayoutButton;
        [SerializeField] private Button exportDataButton;
        [SerializeField] private Button endOfDayButton; // 🆕 NEW

        [Header("UI Panels")]
        [SerializeField] private GameObject endOfDayConfirmationPanel; // 🆕 NEW

        [Header("Search & Filter Controls")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private TMP_Dropdown filterDropdown;

        [Header("Analytics Display")]
        [SerializeField] private GameObject analyticsPanel;
        [SerializeField] private TMP_Text totalGuestsText;
        [SerializeField] private TMP_Text avgStayText;

        private UserRoleManager _userRoleManager;
        private LayoutManager _layoutManager;
        private LayoutEditManager _layoutEditManager;
        private AnalyticsManager _analyticsManager;
        private SessionManager _sessionManager; // 🆕 NEW

        private void Awake()
        {
            ServiceProvider.Register(this);

            if (resetLayoutButton != null) resetLayoutButton.onClick.AddListener(OnResetLayout);
            if (roleSwitchButton != null) roleSwitchButton.onClick.AddListener(OnRoleSwitch);
            if (layoutEditButton != null) layoutEditButton.onClick.AddListener(OnLayoutEdit);
            if (saveDefaultLayoutButton != null) saveDefaultLayoutButton.onClick.AddListener(OnSaveDefaultLayout);
            if (exportDataButton != null) exportDataButton.onClick.AddListener(OnExportData);
            if (endOfDayButton != null) endOfDayButton.onClick.AddListener(OnEndOfDayClicked); // 🆕 NEW
            if (searchButton != null) searchButton.onClick.AddListener(OnSearch);
            if (filterDropdown != null) filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }

        private void Start()
        {
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();
            _layoutManager = ServiceProvider.Get<LayoutManager>();
            _layoutEditManager = ServiceProvider.Get<LayoutEditManager>();
            _analyticsManager = ServiceProvider.Get<AnalyticsManager>();
            _sessionManager = ServiceProvider.Get<SessionManager>(); // 🆕 NEW

            if (_userRoleManager != null)
            {
                _userRoleManager.OnRoleChanged += HandleRoleChanged;
                HandleRoleChanged(_userRoleManager.CurrentRole);
            }

            AnalyticsManager.OnAnalyticsUpdated += UpdateAnalyticsDisplay;
            UpdateAnalyticsDisplay();
        }

        private void OnDestroy()
        {
            if (_userRoleManager != null) _userRoleManager.OnRoleChanged -= HandleRoleChanged;
            AnalyticsManager.OnAnalyticsUpdated -= UpdateAnalyticsDisplay;
            ServiceProvider.Unregister<AdminToolsManager>();
        }

        private void OnEndOfDayClicked()
        {
            if (endOfDayConfirmationPanel != null)
            {
                endOfDayConfirmationPanel.SetActive(true);
            }
        }

        public void ConfirmEndOfDay()
        {
            if (endOfDayConfirmationPanel != null) endOfDayConfirmationPanel.SetActive(false);
            _sessionManager?.RunEndOfDayProcess();
        }

        public void CancelEndOfDay()
        {
            if (endOfDayConfirmationPanel != null) endOfDayConfirmationPanel.SetActive(false);
        }

        private void OnResetLayout()
        {
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin) return;
            _layoutManager?.ResetLayoutToDefault();
        }

        private void OnRoleSwitch()
        {
            _userRoleManager?.ToggleRole();
        }

        private void OnLayoutEdit()
        {
            _layoutEditManager?.ToggleEditMode();
        }

        private void OnSaveDefaultLayout()
        {
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin) return;
            if (_layoutEditManager != null && _layoutEditManager.IsEditModeActive)
            {
                DebugManager.LogWarning(LogCategory.UI, "Please exit Layout Edit Mode before saving the default layout.");
                return;
            }
            _layoutManager?.SaveAsDefaultLayout();
        }

        private void OnExportData()
        {
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin) return;
            _analyticsManager?.ExportSessionToCSV();
        }

        private void UpdateAnalyticsDisplay()
        {
            if (_analyticsManager == null) return;
            if (totalGuestsText != null) totalGuestsText.text = $"Total Guests Today: {_analyticsManager.TotalGuestsToday}";
            if (avgStayText != null) avgStayText.text = $"Avg. Stay: {_analyticsManager.AverageStayDurationMinutes:F1} min";
        }

        private void HandleRoleChanged(UserRoleManager.Role role)
        {
            bool isAdmin = role == UserRoleManager.Role.Admin;
            if (resetLayoutButton != null) resetLayoutButton.gameObject.SetActive(isAdmin);
            if (roleSwitchButton != null) roleSwitchButton.gameObject.SetActive(true);
            if (layoutEditButton != null) layoutEditButton.gameObject.SetActive(isAdmin);
            if (saveDefaultLayoutButton != null) saveDefaultLayoutButton.gameObject.SetActive(isAdmin);
            if (exportDataButton != null) exportDataButton.gameObject.SetActive(isAdmin);
            if (endOfDayButton != null) endOfDayButton.gameObject.SetActive(isAdmin); // 🆕 NEW
            if (searchInput != null) searchInput.gameObject.SetActive(isAdmin);
            if (searchButton != null) searchButton.gameObject.SetActive(isAdmin);
            if (filterDropdown != null) filterDropdown.gameObject.SetActive(isAdmin);
            if (analyticsPanel != null) analyticsPanel.SetActive(isAdmin);
        }

        private void OnSearch()
        {
            if (_layoutManager == null) return;
            string query = searchInput != null ? searchInput.text.Trim().ToLowerInvariant() : string.Empty;
            foreach (var seat in _layoutManager.Seats)
            {
                if (seat == null) continue;
                seat.UpdateVisualState();
                if (string.IsNullOrEmpty(query)) continue;
                bool match = false;
                if (seat.SeatID.ToString().ToLowerInvariant().Contains(query)) match = true;
                if (seat.CurrentGuest != null)
                {
                    if (seat.CurrentGuest.FirstName.ToLowerInvariant().Contains(query)) match = true;
                    if (seat.CurrentGuest.LastName.ToLowerInvariant().Contains(query)) match = true;
                    if (seat.CurrentGuest.RoomNumber.ToLowerInvariant().Contains(query)) match = true;
                }
                if (match && seat.SeatImage != null) seat.SeatImage.color = Color.cyan;
            }
        }

        private void OnFilterChanged(int index)
        {
            if (_layoutManager == null || filterDropdown == null) return;
            string selected = filterDropdown.options[index].text;
            foreach (var seat in _layoutManager.Seats)
            {
                if (seat == null) continue;
                bool visible = true;
                if (!string.IsNullOrEmpty(selected) && !selected.Equals("All", System.StringComparison.OrdinalIgnoreCase))
                {
                    visible = seat.State.ToString().Equals(selected, System.StringComparison.OrdinalIgnoreCase);
                }
                seat.gameObject.SetActive(visible);
            }
        }
    }
}
