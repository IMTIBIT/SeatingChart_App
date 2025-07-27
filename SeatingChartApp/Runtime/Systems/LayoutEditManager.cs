using System;
using UnityEngine;
using SeatingChartApp.Runtime.Systems;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Manages the state of the "Layout Edit Mode". This mode is now strictly
    /// controlled and can only be activated by an Admin.
    /// </summary>
    public class LayoutEditManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject layoutEditToolbar;

        public bool IsEditModeActive { get; private set; }
        public static event Action<bool> OnEditModeChanged;

        private UserRoleManager _userRoleManager;

        private void Awake()
        {
            ServiceProvider.Register(this);
        }

        private void Start()
        {
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();
            if (_userRoleManager != null)
            {
                _userRoleManager.OnRoleChanged += HandleRoleChanged;
            }

            if (layoutEditToolbar != null)
            {
                layoutEditToolbar.SetActive(false);
            }
            IsEditModeActive = false;
        }

        private void OnDestroy()
        {
            if (_userRoleManager != null)
            {
                _userRoleManager.OnRoleChanged -= HandleRoleChanged;
            }
            ServiceProvider.Unregister<LayoutEditManager>();
        }

        /// <summary>
        /// Toggles the layout edit mode, but only if the current user is an Admin.
        /// </summary>
        public void ToggleEditMode()
        {
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin)
            {
                DebugManager.LogWarning(LogCategory.UI, "Attempted to enter Layout Edit Mode without Admin privileges.");
                // Ensure the mode is off if a non-admin tries to activate it.
                if (IsEditModeActive)
                {
                    IsEditModeActive = false;
                    UpdateEditModeState();
                }
                return;
            }

            IsEditModeActive = !IsEditModeActive;
            UpdateEditModeState();
        }

        private void UpdateEditModeState()
        {
            DebugManager.Log(LogCategory.UI, $"Layout Edit Mode {(IsEditModeActive ? "activated" : "deactivated")}.");
            if (layoutEditToolbar != null)
            {
                layoutEditToolbar.SetActive(IsEditModeActive);
            }
            OnEditModeChanged?.Invoke(IsEditModeActive);
        }

        private void HandleRoleChanged(UserRoleManager.Role newRole)
        {
            if (newRole != UserRoleManager.Role.Admin && IsEditModeActive)
            {
                IsEditModeActive = false;
                UpdateEditModeState();
            }
        }
    }
}
