using SeatingChartApp.Runtime.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Provides a set of admin‑only tools accessible via the UI.  Buttons
    /// exposed here trigger functions such as resetting the layout to its
    /// default state or switching roles quickly for testing.  Attach this
    /// component to a GameObject in your scene and assign the button
    /// references in the inspector.
    /// </summary>
    public class AdminToolsManager : MonoBehaviour
    {
        [Header("Admin Buttons")]
        [SerializeField] private Button resetLayoutButton;
        [SerializeField] private Button roleSwitchButton;

        private void Awake()
        {
            if (resetLayoutButton != null)
            {
                resetLayoutButton.onClick.AddListener(OnResetLayout);
            }
            if (roleSwitchButton != null)
            {
                roleSwitchButton.onClick.AddListener(OnRoleSwitch);
            }
        }

        /// <summary>
        /// Invoked when the reset layout button is pressed.  Delegates to
        /// the LayoutManager to delete the saved layout and restore all
        /// seats to their default positions.  Only functions for admins.
        /// </summary>
        private void OnResetLayout()
        {
            if (UserRoleManager.Instance == null || UserRoleManager.Instance.CurrentRole != UserRoleManager.Role.Admin)
                return;
            if (LayoutManager.Instance != null)
            {
                LayoutManager.Instance.ResetLayout();
            }
        }

        /// <summary>
        /// Toggles between Admin and Attendant roles.  Useful for rapid
        /// testing when building the UI.  Note that in production builds
        /// this functionality should be removed or secured.
        /// </summary>
        private void OnRoleSwitch()
        {
            if (UserRoleManager.Instance == null)
                return;
            var manager = UserRoleManager.Instance;
            var newRole = manager.CurrentRole == UserRoleManager.Role.Admin ? UserRoleManager.Role.Attendant : UserRoleManager.Role.Admin;
            manager.SetRole(newRole);
        }
    }
}