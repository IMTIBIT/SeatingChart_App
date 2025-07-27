using System;
using UnityEngine;
using SeatingChartApp.Runtime.Systems;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Manages the state of the "Layout Edit Mode". This mode allows admins
    /// to add, remove, and rotate seats directly on the canvas.
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

            // Ensure toolbar is hidden initially
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
        /// Toggles the layout edit mode on or off.
        /// </summary>
        public void ToggleEditMode()
        {
            // Only admins can enter edit mode
            if (_userRoleManager == null || _userRoleManager.CurrentRole != UserRoleManager.Role.Admin)
            {
                DebugManager.LogWarning(LogCategory.UI, "Only Admins can enter Layout Edit Mode.");
                return;
            }

            IsEditModeActive = !IsEditModeActive;
            DebugManager.Log(LogCategory.UI, $"Layout Edit Mode {(IsEditModeActive ? "activated" : "deactivated")}.");

            if (layoutEditToolbar != null)
            {
                layoutEditToolbar.SetActive(IsEditModeActive);
            }

            // Notify other systems (like SeatController) that the mode has changed
            OnEditModeChanged?.Invoke(IsEditModeActive);
        }

        /// <summary>
        /// Ensures edit mode is turned off if the user's role changes from Admin.
        /// </summary>
        private void HandleRoleChanged(UserRoleManager.Role newRole)
        {
            if (newRole != UserRoleManager.Role.Admin && IsEditModeActive)
            {
                ToggleEditMode(); // This will deactivate the mode
            }
        }
    }
}
